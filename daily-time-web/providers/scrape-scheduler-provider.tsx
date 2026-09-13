"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { getJobPortal, getJobPortals } from "@/lib/api/job-portals";
import {
  scrapePortalNow,
  stopScrapes,
  WorkerRequestAbortedError,
  type WorkerScrapeResult,
} from "@/lib/api/worker";
import { jobOfferKeys, jobPortalKeys, scrapeScheduleKeys } from "@/lib/query/keys";
import { useAppNotifications } from "@/providers/app-notifications-provider";
import type { JobPortal } from "@/types/api";

/**
 * Capturas manuales lanzadas desde la web. La ejecución automática ya no vive aquí:
 * la programa el worker con el horario global guardado en la API.
 */
export type ScrapeRunSource = "manual" | "selected";

const RUN_COPY: Record<
  ScrapeRunSource,
  { emptyTitle: string; emptyBody: string; startTitle: string; endTitle: string }
> = {
  manual: {
    emptyTitle: "Sin portales activos",
    emptyBody: "Activa al menos un portal para capturar.",
    startTitle: "Captura de todos los portales",
    endTitle: "Captura secuencial terminada",
  },
  selected: {
    emptyTitle: "Sin portales marcados",
    emptyBody: "Marca al menos un portal con el check Automática.",
    startTitle: "Captura de portales marcados",
    endTitle: "Captura de marcados terminada",
  },
};

type ScrapeSchedulerContextValue = {
  isRunningAll: boolean;
  currentPortalId: number | null;
  runAllSequential: (opts?: { source?: ScrapeRunSource }) => Promise<void>;
  runOne: (portalId: number, portalName?: string) => Promise<WorkerScrapeResult | null>;
  stop: () => Promise<void>;
};

const ScrapeSchedulerContext = createContext<ScrapeSchedulerContextValue | null>(null);

export function ScrapeSchedulerProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { notify } = useAppNotifications();
  const [isRunningAll, setIsRunningAll] = useState(false);
  const [currentPortalId, setCurrentPortalId] = useState<number | null>(null);
  const runningRef = useRef(false);
  const stopRequestedRef = useRef(false);
  const abortRef = useRef<AbortController | null>(null);

  const patchPortal = useCallback(
    (portalId: number, patch: Partial<JobPortal>) => {
      queryClient.setQueriesData<JobPortal[]>(
        { queryKey: jobPortalKeys.all },
        (old) => {
          if (!old) return old;
          return old.map((item) =>
            item.id === portalId ? { ...item, ...patch } : item,
          );
        },
      );
    },
    [queryClient],
  );

  const refreshPortal = useCallback(
    async (portalId: number) => {
      try {
        const updated = await getJobPortal(portalId);
        patchPortal(portalId, updated);
      } catch {
        /* keep optimistic patch */
      }
    },
    [patchPortal],
  );

  const runOne = useCallback(
    async (portalId: number, portalName?: string) => {
      if (abortRef.current) {
        notify({
          title: "Captura en curso",
          body: "Espera a que termine este portal o pulsa Detener.",
          tone: "info",
        });
        return null;
      }

      const abort = new AbortController();
      abortRef.current = abort;
      setCurrentPortalId(portalId);
      patchPortal(portalId, {
        lastRunStatus: "running",
        lastRunAt: new Date().toISOString(),
      });

      try {
        const result = await scrapePortalNow(portalId, abort.signal);
        await refreshPortal(portalId);
        if (
          result.status !== "cancelled" &&
          result.status !== "error" &&
          result.status !== "blocked"
        ) {
          await queryClient.invalidateQueries({ queryKey: jobOfferKeys.all });
        }
        return result;
      } catch (error) {
        await refreshPortal(portalId);
        if (error instanceof WorkerRequestAbortedError) {
          return {
            portalId,
            portalName: portalName ?? `portal #${portalId}`,
            status: "cancelled",
            message: "Captura detenida.",
            offers: [],
          } satisfies WorkerScrapeResult;
        }
        notify({
          title: `Error capturando ${portalName ?? `portal #${portalId}`}`,
          body:
            error instanceof Error
              ? error.message
              : "¿Está corriendo el worker en :5500?",
          tone: "error",
        });
        return null;
      } finally {
        if (abortRef.current === abort) {
          abortRef.current = null;
        }
        setCurrentPortalId(null);
      }
    },
    [notify, patchPortal, queryClient, refreshPortal],
  );

  const stop = useCallback(async () => {
    stopRequestedRef.current = true;
    abortRef.current?.abort();
    try {
      const result = await stopScrapes();
      notify({
        title: "Detener captura",
        body: result.message,
        tone: "info",
      });
    } catch (error) {
      notify({
        title: "No se pudo pedir la detención al worker",
        body: error instanceof Error ? error.message : "Revisa que el worker esté en :5500.",
        tone: "error",
      });
    } finally {
      // Puede haber detenido una captura programada: refresca su estado.
      void queryClient.invalidateQueries({ queryKey: scrapeScheduleKeys.all });
      void queryClient.invalidateQueries({ queryKey: jobPortalKeys.all });
    }
  }, [notify, queryClient]);

  const runAllSequential = useCallback(
    async (opts?: { source?: ScrapeRunSource }) => {
      if (runningRef.current) {
        notify({
          title: "Captura en curso",
          body: "Espera a que termine la secuencia actual.",
          tone: "info",
        });
        return;
      }

      runningRef.current = true;
      stopRequestedRef.current = false;
      setIsRunningAll(true);
      const source = opts?.source ?? "manual";
      const copy = RUN_COPY[source];

      try {
        const active = (await getJobPortals(true)).filter((p) => p.isActive);
        const portals =
          source === "manual" ? active : active.filter((p) => p.autoScrapeEnabled);
        if (!portals.length) {
          notify({
            title: copy.emptyTitle,
            body: copy.emptyBody,
            tone: "info",
          });
          return;
        }

        notify({
          title: copy.startTitle,
          body: `Se ejecutarán ${portals.length} portal(es) uno detrás de otro.`,
          tone: "info",
        });

        let ok = 0;
        let failed = 0;
        let inserted = 0;
        let updated = 0;
        let stopped = false;

        for (const portal of portals) {
          if (stopRequestedRef.current) {
            stopped = true;
            break;
          }
          const result = await runOne(portal.id, portal.name);
          if (stopRequestedRef.current || result?.status === "cancelled") {
            stopped = true;
            notify({
              title: "Captura detenida",
              body: result?.message ?? "Se detuvo la secuencia.",
              tone: "info",
            });
            break;
          }
          if (!result) {
            failed++;
            continue;
          }
          if (result.status === "error" || result.status === "blocked") {
            failed++;
            notify({
              title: `${portal.name}: ${result.status}`,
              body: result.message,
              tone: "error",
              toast: false,
            });
          } else {
            ok++;
            inserted += result.savedInserted ?? 0;
            updated += result.savedUpdated ?? 0;
            notify({
              title: `${portal.name}: ${result.status}`,
              body: result.message,
              tone: "success",
              toast: false,
            });
          }
        }

        if (!stopped) {
          notify({
            title: copy.endTitle,
            body: `OK: ${ok} · Errores: ${failed} · Nuevas: ${inserted} · Actualizadas: ${updated}`,
            tone: failed > 0 && ok === 0 ? "error" : "success",
          });
        }
      } catch (error) {
        notify({
          title: "No se pudo iniciar la captura",
          body: error instanceof Error ? error.message : "Revisa que la API esté activa.",
          tone: "error",
        });
      } finally {
        runningRef.current = false;
        setIsRunningAll(false);
        setCurrentPortalId(null);
      }
    },
    [notify, runOne],
  );

  const value = useMemo<ScrapeSchedulerContextValue>(
    () => ({
      isRunningAll,
      currentPortalId,
      runAllSequential,
      runOne,
      stop,
    }),
    [isRunningAll, currentPortalId, runAllSequential, runOne, stop],
  );

  return (
    <ScrapeSchedulerContext.Provider value={value}>
      {children}
    </ScrapeSchedulerContext.Provider>
  );
}

export function useScrapeScheduler() {
  const ctx = useContext(ScrapeSchedulerContext);
  if (!ctx) {
    throw new Error("useScrapeScheduler debe usarse dentro de ScrapeSchedulerProvider.");
  }
  return ctx;
}
