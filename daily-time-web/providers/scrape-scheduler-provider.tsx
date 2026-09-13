"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
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
import { jobOfferKeys, jobPortalKeys } from "@/lib/query/keys";
import { useAppNotifications } from "@/providers/app-notifications-provider";
import type { JobPortal } from "@/types/api";

const STORAGE_AUTO_KEY = "dailytime.scrape.autoEnabled";
const STORAGE_INTERVAL_KEY = "dailytime.scrape.intervalMinutes";
const STORAGE_AUTO_PORTALS_KEY = "dailytime.scrape.autoPortalIds";
const STORAGE_NEXT_KEY = "dailytime.scrape.nextAutoAt";

/** Default prudente (antes era 10). */
export const DEFAULT_INTERVAL_MINUTES = 60;
export const MIN_INTERVAL_MINUTES = 10;
export const MAX_INTERVAL_MINUTES = 360;

/** Origen de una corrida secuencial: manual = todos los activos, auto = programada, selected = solo los marcados. */
export type ScrapeRunSource = "manual" | "auto" | "selected";

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
  auto: {
    emptyTitle: "Sin portales en la automática",
    emptyBody: "Marca al menos un portal para la ejecución automática.",
    startTitle: "Captura automática iniciada",
    endTitle: "Captura automática terminada",
  },
  selected: {
    emptyTitle: "Sin portales marcados",
    emptyBody: "Marca al menos un portal con el check Automática.",
    startTitle: "Captura de portales marcados",
    endTitle: "Captura de marcados terminada",
  },
};

type ScrapeSchedulerContextValue = {
  autoEnabled: boolean;
  setAutoEnabled: (value: boolean) => void;
  intervalMinutes: number;
  setIntervalMinutes: (minutes: number) => void;
  autoPortalIds: number[];
  setAutoPortalIds: (ids: number[]) => void;
  toggleAutoPortal: (id: number) => void;
  isRunningAll: boolean;
  currentPortalId: number | null;
  lastAutoAt: string | null;
  nextAutoAt: string | null;
  runAllSequential: (opts?: { source?: ScrapeRunSource }) => Promise<void>;
  runOne: (portalId: number, portalName?: string) => Promise<WorkerScrapeResult | null>;
  stop: () => Promise<void>;
};

const ScrapeSchedulerContext = createContext<ScrapeSchedulerContextValue | null>(null);

function clampIntervalMinutes(value: number): number {
  if (!Number.isFinite(value)) return DEFAULT_INTERVAL_MINUTES;
  return Math.min(
    MAX_INTERVAL_MINUTES,
    Math.max(MIN_INTERVAL_MINUTES, Math.round(value)),
  );
}

function readAutoEnabled(): boolean {
  if (typeof window === "undefined") return false;
  try {
    return window.localStorage.getItem(STORAGE_AUTO_KEY) === "1";
  } catch {
    return false;
  }
}

function readIntervalMinutes(): number {
  if (typeof window === "undefined") return DEFAULT_INTERVAL_MINUTES;
  try {
    const raw = window.localStorage.getItem(STORAGE_INTERVAL_KEY);
    if (!raw) return DEFAULT_INTERVAL_MINUTES;
    return clampIntervalMinutes(Number(raw));
  } catch {
    return DEFAULT_INTERVAL_MINUTES;
  }
}

function readAutoPortalIds(): number[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(STORAGE_AUTO_PORTALS_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((id): id is number => Number.isInteger(id) && id > 0);
  } catch {
    return [];
  }
}

export function ScrapeSchedulerProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { notify } = useAppNotifications();
  const [autoEnabled, setAutoEnabledState] = useState(false);
  const [intervalMinutes, setIntervalMinutesState] = useState(DEFAULT_INTERVAL_MINUTES);
  const [autoPortalIds, setAutoPortalIdsState] = useState<number[]>([]);
  const [ready, setReady] = useState(false);
  const [isRunningAll, setIsRunningAll] = useState(false);
  const [currentPortalId, setCurrentPortalId] = useState<number | null>(null);
  const [lastAutoAt, setLastAutoAt] = useState<string | null>(null);
  const [nextAutoAt, setNextAutoAt] = useState<string | null>(null);
  const runningRef = useRef(false);
  const stopRequestedRef = useRef(false);
  const abortRef = useRef<AbortController | null>(null);
  const nextDueMsRef = useRef<number | null>(null);
  const intervalMinutesRef = useRef(DEFAULT_INTERVAL_MINUTES);
  const runAllSequentialRef = useRef<(opts?: { source?: ScrapeRunSource }) => Promise<void>>(
    async () => {},
  );

  const persistNextDue = useCallback((dueMs: number | null) => {
    nextDueMsRef.current = dueMs;
    setNextAutoAt(dueMs != null ? new Date(dueMs).toISOString() : null);
    try {
      if (dueMs == null) window.localStorage.removeItem(STORAGE_NEXT_KEY);
      else window.localStorage.setItem(STORAGE_NEXT_KEY, String(dueMs));
    } catch {
      /* ignore */
    }
  }, []);

  const scheduleFromNow = useCallback(() => {
    const ms = clampIntervalMinutes(intervalMinutesRef.current) * 60 * 1000;
    persistNextDue(Date.now() + ms);
  }, [persistNextDue]);

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

  useEffect(() => {
    const enabled = readAutoEnabled();
    const minutes = readIntervalMinutes();
    setAutoEnabledState(enabled);
    setIntervalMinutesState(minutes);
    intervalMinutesRef.current = minutes;
    setAutoPortalIdsState(readAutoPortalIds());
    if (enabled) {
      try {
        const stored = Number(window.localStorage.getItem(STORAGE_NEXT_KEY));
        if (Number.isFinite(stored) && stored > Date.now()) {
          persistNextDue(stored);
        } else {
          persistNextDue(Date.now() + minutes * 60 * 1000);
        }
      } catch {
        persistNextDue(Date.now() + minutes * 60 * 1000);
      }
    }
    setReady(true);
  }, [persistNextDue]);

  const setAutoEnabled = useCallback(
    (value: boolean) => {
      setAutoEnabledState(value);
      try {
        window.localStorage.setItem(STORAGE_AUTO_KEY, value ? "1" : "0");
      } catch {
        /* ignore */
      }
      if (value) scheduleFromNow();
      else persistNextDue(null);
    },
    [persistNextDue, scheduleFromNow],
  );

  const setIntervalMinutes = useCallback(
    (minutes: number) => {
      const next = clampIntervalMinutes(minutes);
      if (next === intervalMinutesRef.current) return;
      intervalMinutesRef.current = next;
      setIntervalMinutesState(next);
      try {
        window.localStorage.setItem(STORAGE_INTERVAL_KEY, String(next));
      } catch {
        /* ignore */
      }
      if (autoEnabled) scheduleFromNow();
    },
    [autoEnabled, scheduleFromNow],
  );

  const persistAutoPortalIds = useCallback((ids: number[]) => {
    const next = [...new Set(ids)].sort((a, b) => a - b);
    setAutoPortalIdsState(next);
    try {
      window.localStorage.setItem(STORAGE_AUTO_PORTALS_KEY, JSON.stringify(next));
    } catch {
      /* ignore */
    }
  }, []);

  const setAutoPortalIds = useCallback(
    (ids: number[]) => persistAutoPortalIds(ids),
    [persistAutoPortalIds],
  );

  const toggleAutoPortal = useCallback(
    (id: number) => {
      setAutoPortalIdsState((current) => {
        const next = current.includes(id)
          ? current.filter((item) => item !== id)
          : [...current, id];
        const unique = [...new Set(next)].sort((a, b) => a - b);
        try {
          window.localStorage.setItem(STORAGE_AUTO_PORTALS_KEY, JSON.stringify(unique));
        } catch {
          /* ignore */
        }
        return unique;
      });
    },
    [],
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
    }
  }, [notify]);

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
          source === "manual"
            ? active
            : active.filter((p) => autoPortalIds.includes(p.id));
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

        if (source === "auto") {
          setLastAutoAt(new Date().toISOString());
        }
      } finally {
        runningRef.current = false;
        setIsRunningAll(false);
        setCurrentPortalId(null);
        if (source === "auto") scheduleFromNow();
      }
    },
    [notify, runOne, autoPortalIds, scheduleFromNow],
  );

  runAllSequentialRef.current = runAllSequential;
  intervalMinutesRef.current = intervalMinutes;

  useEffect(() => {
    if (!ready || !autoEnabled) return;

    const id = window.setInterval(() => {
      const due = nextDueMsRef.current;
      if (due == null || Date.now() < due) return;
      if (runningRef.current) return;
      void runAllSequentialRef.current({ source: "auto" });
    }, 1000);

    return () => window.clearInterval(id);
  }, [ready, autoEnabled]);

  const value = useMemo<ScrapeSchedulerContextValue>(
    () => ({
      autoEnabled,
      setAutoEnabled,
      intervalMinutes,
      setIntervalMinutes,
      autoPortalIds,
      setAutoPortalIds,
      toggleAutoPortal,
      isRunningAll,
      currentPortalId,
      lastAutoAt,
      nextAutoAt,
      runAllSequential,
      runOne,
      stop,
    }),
    [
      autoEnabled,
      setAutoEnabled,
      intervalMinutes,
      setIntervalMinutes,
      autoPortalIds,
      setAutoPortalIds,
      toggleAutoPortal,
      isRunningAll,
      currentPortalId,
      lastAutoAt,
      nextAutoAt,
      runAllSequential,
      runOne,
      stop,
    ],
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
