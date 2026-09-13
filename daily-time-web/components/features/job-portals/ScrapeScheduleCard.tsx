"use client";

import { useState } from "react";
import { Plus, Square, Timer, TriangleAlert, X } from "lucide-react";
import { toast } from "sonner";
import type { JobPortal, ScrapeSchedule, ScrapeSlotStatus } from "@/types/api";
import { useScrapeScheduleMutations } from "@/hooks/mutations/use-scrape-schedule-mutations";
import {
  SCHEDULE_TIME_PATTERN,
  scrapeScheduleSchema,
  type ScrapeScheduleInput,
} from "@/schemas/scrape-schedule.schema";
import { cn } from "@/lib/utils/cn";

const DAYS: { iso: number; short: string; name: string }[] = [
  { iso: 1, short: "L", name: "Lunes" },
  { iso: 2, short: "M", name: "Martes" },
  { iso: 3, short: "X", name: "Miércoles" },
  { iso: 4, short: "J", name: "Jueves" },
  { iso: 5, short: "V", name: "Viernes" },
  { iso: 6, short: "S", name: "Sábado" },
  { iso: 7, short: "D", name: "Domingo" },
];

const SLOT_STATUS_LABELS: Record<ScrapeSlotStatus, string> = {
  running: "en curso",
  completed: "completada",
  failed: "con errores",
  cancelled: "detenida",
  skipped: "omitida",
};

function formatSlot(iso: string) {
  const date = new Date(iso);
  const time = date.toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" });
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(today.getDate() + 1);
  if (date.toDateString() === today.toDateString()) return `hoy ${time}`;
  if (date.toDateString() === tomorrow.toDateString()) return `mañana ${time}`;
  const day = date.toLocaleDateString("es-CO", { weekday: "short", day: "numeric", month: "short" });
  return `${day} ${time}`;
}

/** Hueco más largo (en horas) entre dos capturas seguidas a lo largo de la semana. */
function largestGapHours(times: string[], days: number[]) {
  if (!times.length) return null;
  const activeDays = days.length ? days : DAYS.map((d) => d.iso);
  const minutesOfDay = times.map((t) => {
    const [h, m] = t.split(":").map(Number);
    return h * 60 + m;
  });
  const slots = activeDays
    .flatMap((day) => minutesOfDay.map((m) => (day - 1) * 1440 + m))
    .sort((a, b) => a - b);
  const week = 7 * 1440;
  let max = 0;
  slots.forEach((slot, i) => {
    const next = i + 1 < slots.length ? slots[i + 1] : slots[0] + week;
    max = Math.max(max, next - slot);
  });
  return max / 60;
}

/** Ventana de antigüedad que busca el portal (maxAgeHours / maxAgeDays de su ScrapeConfig). */
function portalMaxAgeHours(portal: JobPortal) {
  if (!portal.scrapeConfig) return null;
  try {
    const config = JSON.parse(portal.scrapeConfig) as { maxAgeHours?: unknown; maxAgeDays?: unknown };
    if (typeof config.maxAgeHours === "number") return config.maxAgeHours;
    if (typeof config.maxAgeDays === "number") return config.maxAgeDays * 24;
  } catch {
    /* JSON inválido: sin aviso */
  }
  return null;
}

function toInput(schedule: ScrapeSchedule | undefined): ScrapeScheduleInput {
  return {
    enabled: schedule?.enabled ?? false,
    times: schedule?.times ?? [],
    days: schedule?.days ?? [],
  };
}

export function ScrapeScheduleCard({
  schedule,
  portals,
  onStop,
}: {
  schedule: ScrapeSchedule | undefined;
  portals: JobPortal[];
  onStop: () => void;
}) {
  const { update } = useScrapeScheduleMutations();
  const [draft, setDraft] = useState<ScrapeScheduleInput | null>(null);
  const [newTime, setNewTime] = useState("07:00");
  const current = draft ?? toInput(schedule);
  const dirty = draft != null;
  const running = schedule?.lastSlotStatus === "running";
  const markedPortals = portals.filter((p) => p.isActive && p.autoScrapeEnabled);

  const gap = largestGapHours(current.times, current.days);
  const shortWindowPortals =
    gap == null
      ? []
      : markedPortals.filter((p) => {
          const maxAge = portalMaxAgeHours(p);
          return maxAge != null && maxAge < gap;
        });

  const save = (next: ScrapeScheduleInput) => {
    const parsed = scrapeScheduleSchema.safeParse(next);
    if (!parsed.success) {
      toast.error(parsed.error.issues[0]?.message ?? "Horario inválido");
      return;
    }
    update.mutate(parsed.data, {
      onSuccess: ({ schedule: saved, workerReloaded }) => {
        setDraft(null);
        toast.success(saved.enabled ? "Horario guardado" : "Ejecución automática desactivada", {
          description: !workerReloaded
            ? "El worker no respondió: tomará el horario cuando arranque."
            : saved.nextSlotAt
              ? `Próxima captura: ${formatSlot(saved.nextSlotAt)}`
              : undefined,
        });
      },
      onError: (error) => toast.error(error.message),
    });
  };

  const addTime = () => {
    if (!SCHEDULE_TIME_PATTERN.test(newTime)) {
      toast.error("Usa el formato HH:mm");
      return;
    }
    if (current.times.includes(newTime)) return;
    setDraft({ ...current, times: [...current.times, newTime].sort() });
  };

  const removeTime = (time: string) =>
    setDraft({ ...current, times: current.times.filter((t) => t !== time) });

  const toggleDay = (iso: number) => {
    const selected = current.days.length ? current.days : DAYS.map((d) => d.iso);
    const next = selected.includes(iso)
      ? selected.filter((d) => d !== iso)
      : [...selected, iso].sort((a, b) => a - b);
    if (!next.length) {
      toast.error("Deja al menos un día");
      return;
    }
    setDraft({ ...current, days: next.length === DAYS.length ? [] : next });
  };

  return (
    <div className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[var(--shadow-card)]">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="inline-flex items-center gap-1.5 text-sm font-medium text-[var(--ink)]">
            <Timer className="size-4" />
            Ejecución automática
          </p>
          <p className="mt-1 text-xs text-[var(--muted)]">
            A cada hora, el worker captura uno tras otro los portales marcados como
            Automática ({markedPortals.length}), aunque la web esté cerrada. Si el
            equipo está apagado a esa hora, esa captura se omite.
          </p>
        </div>
        <label
          className={cn(
            "inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--border)] px-3 py-2 text-sm",
            current.enabled
              ? "border-[var(--accent)] bg-[var(--accent-soft)] text-[var(--ink)]"
              : "text-[var(--muted)]",
          )}
        >
          <span>{current.enabled ? "Activa" : "Inactiva"}</span>
          <input
            type="checkbox"
            className="size-4 accent-[var(--accent)]"
            checked={current.enabled}
            disabled={update.isPending || !schedule}
            onChange={(e) => save({ ...current, enabled: e.target.checked })}
          />
        </label>
      </div>

      <div className="flex flex-col gap-2">
        <span className="text-xs font-medium text-[var(--muted)]">Horas</span>
        <div className="flex flex-wrap items-center gap-2">
          {current.times.map((time) => (
            <span
              key={time}
              className="inline-flex items-center gap-1 rounded-full border border-[var(--border)] bg-[var(--surface-muted)] py-1 pl-3 pr-1 text-sm text-[var(--ink)]"
            >
              {time}
              <button
                type="button"
                onClick={() => removeTime(time)}
                aria-label={`Quitar ${time}`}
                className="rounded-full p-0.5 text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
              >
                <X className="size-3.5" />
              </button>
            </span>
          ))}
          {!current.times.length ? (
            <span className="text-xs text-[var(--muted)]">Sin horas definidas.</span>
          ) : null}
          <span className="inline-flex items-center gap-1">
            <input
              type="time"
              value={newTime}
              onChange={(e) => setNewTime(e.target.value)}
              className="rounded-md border border-[var(--border)] bg-white px-2 py-1 text-sm outline-none focus:border-[var(--accent)]"
            />
            <button
              type="button"
              onClick={addTime}
              className="inline-flex items-center gap-1 rounded-md border border-[var(--border)] px-2 py-1 text-sm hover:bg-[var(--surface-muted)]"
            >
              <Plus className="size-3.5" />
              Agregar
            </button>
          </span>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <span className="text-xs font-medium text-[var(--muted)]">Días</span>
        <div className="flex flex-wrap gap-1">
          {DAYS.map((day) => {
            const active = !current.days.length || current.days.includes(day.iso);
            return (
              <button
                key={day.iso}
                type="button"
                title={day.name}
                aria-pressed={active}
                onClick={() => toggleDay(day.iso)}
                className={cn(
                  "size-8 rounded-md border text-sm",
                  active
                    ? "border-[var(--accent)] bg-[var(--accent-soft)] text-[var(--ink)]"
                    : "border-[var(--border)] text-[var(--muted)] hover:bg-[var(--surface-muted)]",
                )}
              >
                {day.short}
              </button>
            );
          })}
        </div>
      </div>

      {shortWindowPortals.length && gap != null ? (
        <p className="flex items-start gap-1.5 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
          <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
          <span>
            Pasan hasta {Math.round(gap)} h entre capturas y{" "}
            {shortWindowPortals.map((p) => `${p.name} (${portalMaxAgeHours(p)} h)`).join(", ")}{" "}
            solo busca ofertas más recientes: podrías perder las publicadas en ese hueco.
            Agrega horas o amplía su maxAgeHours.
          </span>
        </p>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-[var(--border)] pt-3">
        <p className="text-xs text-[var(--muted)]">
          {running && schedule?.lastSlotAt
            ? `Captura de las ${formatSlot(schedule.lastSlotAt)} en curso. `
            : schedule?.enabled && schedule.nextSlotAt
              ? `Próxima: ${formatSlot(schedule.nextSlotAt)}. `
              : ""}
          {!running && schedule?.lastSlotAt && schedule.lastSlotStatus
            ? `Última: ${formatSlot(schedule.lastSlotAt)} · ${SLOT_STATUS_LABELS[schedule.lastSlotStatus]}${schedule.lastSlotMessage ? ` · ${schedule.lastSlotMessage}` : ""}`
            : ""}
        </p>
        <div className="flex gap-2">
          {running ? (
            <button
              type="button"
              onClick={onStop}
              className="inline-flex items-center gap-1.5 rounded-md border border-[var(--danger)] px-3 py-1.5 text-sm font-medium text-[var(--danger)] hover:bg-red-50"
            >
              <Square className="size-4" />
              Detener
            </button>
          ) : null}
          {dirty ? (
            <>
              <button
                type="button"
                onClick={() => setDraft(null)}
                className="rounded-md border border-[var(--border)] px-3 py-1.5 text-sm"
              >
                Descartar
              </button>
              <button
                type="button"
                onClick={() => save(current)}
                disabled={update.isPending}
                className="rounded-md bg-[var(--accent)] px-3 py-1.5 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
              >
                {update.isPending ? "Guardando…" : "Guardar horario"}
              </button>
            </>
          ) : null}
        </div>
      </div>
    </div>
  );
}
