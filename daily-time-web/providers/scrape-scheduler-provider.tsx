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
import { getJobPortals } from "@/lib/api/job-portals";
import { scrapePortalNow, type WorkerScrapeResult } from "@/lib/api/worker";
import { jobOfferKeys, jobPortalKeys } from "@/lib/query/keys";
import { useAppNotifications } from "@/providers/app-notifications-provider";

const STORAGE_AUTO_KEY = "dailytime.scrape.autoEnabled";
const STORAGE_INTERVAL_KEY = "dailytime.scrape.intervalMinutes";

/** Default prudente (antes era 10). */
export const DEFAULT_INTERVAL_MINUTES = 60;
export const MIN_INTERVAL_MINUTES = 10;
export const MAX_INTERVAL_MINUTES = 360;

type ScrapeSchedulerContextValue = {
  autoEnabled: boolean;
  setAutoEnabled: (value: boolean) => void;
  intervalMinutes: number;
  setIntervalMinutes: (minutes: number) => void;
  isRunningAll: boolean;
  currentPortalId: number | null;
  lastAutoAt: string | null;
  nextAutoAt: string | null;
  runAllSequential: (opts?: { source?: "manual" | "auto" }) => Promise<void>;
  runOne: (portalId: number, portalName?: string) => Promise<WorkerScrapeResult | null>;
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

export function ScrapeSchedulerProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { notify } = useAppNotifications();
  const [autoEnabled, setAutoEnabledState] = useState(false);
  const [intervalMinutes, setIntervalMinutesState] = useState(DEFAULT_INTERVAL_MINUTES);
  const [ready, setReady] = useState(false);
  const [isRunningAll, setIsRunningAll] = useState(false);
  const [currentPortalId, setCurrentPortalId] = useState<number | null>(null);
  const [lastAutoAt, setLastAutoAt] = useState<string | null>(null);
  const [nextAutoAt, setNextAutoAt] = useState<string | null>(null);
  const runningRef = useRef(false);

  useEffect(() => {
    setAutoEnabledState(readAutoEnabled());
    setIntervalMinutesState(readIntervalMinutes());
    setReady(true);
  }, []);

  const setAutoEnabled = useCallback(
    (value: boolean) => {
      setAutoEnabledState(value);
      try {
        window.localStorage.setItem(STORAGE_AUTO_KEY, value ? "1" : "0");
      } catch {
        /* ignore */
      }
      if (value) {
        const ms = clampIntervalMinutes(intervalMinutes) * 60 * 1000;
        setNextAutoAt(new Date(Date.now() + ms).toISOString());
      } else {
        setNextAutoAt(null);
      }
    },
    [intervalMinutes],
  );

  const setIntervalMinutes = useCallback(
    (minutes: number) => {
      const next = clampIntervalMinutes(minutes);
      setIntervalMinutesState(next);
      try {
        window.localStorage.setItem(STORAGE_INTERVAL_KEY, String(next));
      } catch {
        /* ignore */
      }
      if (autoEnabled) {
        setNextAutoAt(new Date(Date.now() + next * 60 * 1000).toISOString());
      }
    },
    [autoEnabled],
  );

  const invalidate = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: jobPortalKeys.all }),
      queryClient.invalidateQueries({ queryKey: jobOfferKeys.all }),
    ]);
  }, [queryClient]);

  const runOne = useCallback(
    async (portalId: number, portalName?: string) => {
      setCurrentPortalId(portalId);
      try {
        const result = await scrapePortalNow(portalId);
        await invalidate();
        return result;
      } catch (error) {
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
        setCurrentPortalId(null);
      }
    },
    [invalidate, notify],
  );

  const runAllSequential = useCallback(
    async (opts?: { source?: "manual" | "auto" }) => {
      if (runningRef.current) {
        notify({
          title: "Captura en curso",
          body: "Espera a que termine la secuencia actual.",
          tone: "info",
        });
        return;
      }

      runningRef.current = true;
      setIsRunningAll(true);
      const source = opts?.source ?? "manual";

      try {
        const portals = (await getJobPortals(true)).filter((p) => p.isActive);
        if (!portals.length) {
          notify({
            title: "Sin portales activos",
            body: "Activa al menos un portal para capturar.",
            tone: "info",
          });
          return;
        }

        notify({
          title:
            source === "auto"
              ? "Captura automática iniciada"
              : "Captura de todos los portales",
          body: `Se ejecutarán ${portals.length} portal(es) uno detrás de otro.`,
          tone: "info",
        });

        let ok = 0;
        let failed = 0;
        let inserted = 0;
        let updated = 0;

        for (const portal of portals) {
          const result = await runOne(portal.id, portal.name);
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

        notify({
          title:
            source === "auto"
              ? "Captura automática terminada"
              : "Captura secuencial terminada",
          body: `OK: ${ok} · Errores: ${failed} · Nuevas: ${inserted} · Actualizadas: ${updated}`,
          tone: failed > 0 && ok === 0 ? "error" : "success",
        });

        if (source === "auto") {
          setLastAutoAt(new Date().toISOString());
        }
      } finally {
        runningRef.current = false;
        setIsRunningAll(false);
        setCurrentPortalId(null);
      }
    },
    [notify, runOne],
  );

  useEffect(() => {
    if (!ready || !autoEnabled) {
      setNextAutoAt(null);
      return;
    }

    const intervalMs = clampIntervalMinutes(intervalMinutes) * 60 * 1000;

    const tick = () => {
      setNextAutoAt(new Date(Date.now() + intervalMs).toISOString());
      void runAllSequential({ source: "auto" });
    };

    setNextAutoAt(new Date(Date.now() + intervalMs).toISOString());
    const id = window.setInterval(tick, intervalMs);
    return () => window.clearInterval(id);
  }, [ready, autoEnabled, intervalMinutes, runAllSequential]);

  const value = useMemo<ScrapeSchedulerContextValue>(
    () => ({
      autoEnabled,
      setAutoEnabled,
      intervalMinutes,
      setIntervalMinutes,
      isRunningAll,
      currentPortalId,
      lastAutoAt,
      nextAutoAt,
      runAllSequential,
      runOne,
    }),
    [
      autoEnabled,
      setAutoEnabled,
      intervalMinutes,
      setIntervalMinutes,
      isRunningAll,
      currentPortalId,
      lastAutoAt,
      nextAutoAt,
      runAllSequential,
      runOne,
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
