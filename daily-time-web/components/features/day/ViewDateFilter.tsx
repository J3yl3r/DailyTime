"use client";

import { CalendarDays } from "lucide-react";
import { cn } from "@/lib/utils/cn";
import {
  todayApiDate,
  type DateFilterMode,
  type DateFilterValue,
} from "@/lib/utils/date";

type Props = {
  value: DateFilterValue;
  onChange: (value: DateFilterValue) => void;
};

const MODES: { value: DateFilterMode; label: string }[] = [
  { value: "today", label: "Hoy" },
  { value: "day", label: "Día" },
  { value: "month", label: "Mes" },
  { value: "year", label: "Año" },
];

export function ViewDateFilter({ value, onChange }: Props) {
  function selectMode(mode: DateFilterMode) {
    onChange({
      mode,
      date: mode === "today" ? todayApiDate() : value.date,
    });
  }

  const inputClass =
    "h-9 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--ink)] outline-none transition focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent)]/15";

  return (
    <section className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)]/80 px-4 py-3 shadow-[var(--shadow-card)] lg:flex-row lg:items-center lg:justify-between">
      <div className="flex items-center gap-2 text-sm font-medium text-[var(--ink)]">
        <CalendarDays className="size-4 text-[var(--accent)]" />
        Periodo
      </div>
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <div className="flex flex-wrap gap-1 rounded-lg bg-[var(--surface-muted)] p-1">
          {MODES.map((mode) => (
            <button
              key={mode.value}
              type="button"
              onClick={() => selectMode(mode.value)}
              className={cn(
                "rounded-md px-3 py-1.5 text-xs font-medium text-[var(--muted)] transition",
                value.mode === mode.value &&
                  "bg-[var(--surface)] text-[var(--ink)] shadow-sm"
              )}
            >
              {mode.label}
            </button>
          ))}
        </div>

        {value.mode === "day" && (
          <input
            type="date"
            value={value.date}
            onChange={(event) =>
              event.target.value &&
              onChange({ mode: "day", date: event.target.value })
            }
            className={inputClass}
            aria-label="Día específico"
          />
        )}
        {value.mode === "month" && (
          <input
            type="month"
            value={value.date.slice(0, 7)}
            onChange={(event) =>
              event.target.value &&
              onChange({ mode: "month", date: `${event.target.value}-01` })
            }
            className={inputClass}
            aria-label="Mes"
          />
        )}
        {value.mode === "year" && (
          <input
            type="number"
            min="1900"
            max="9999"
            value={value.date.slice(0, 4)}
            onChange={(event) => {
              const year = Number(event.target.value);
              if (year >= 1900 && year <= 9999) {
                onChange({ mode: "year", date: `${year}-01-01` });
              }
            }}
            className={`${inputClass} w-28`}
            aria-label="Año"
          />
        )}
      </div>
    </section>
  );
}
