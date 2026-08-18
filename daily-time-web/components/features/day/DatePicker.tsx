"use client";

import { addDays, format, parseISO } from "date-fns";
import { es } from "date-fns/locale";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { toApiDate } from "@/lib/utils/date";

type Props = {
  workDate: string;
  onChange: (workDate: string) => void;
};

export function DatePicker({ workDate, onChange }: Props) {
  const go = (days: number) => {
    const d = addDays(parseISO(workDate), days);
    onChange(toApiDate(d));
  };

  return (
    <div className="flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={() => go(-1)}
        className="rounded-md border border-[var(--border)] bg-[var(--surface)] p-1.5 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
        aria-label="Día anterior"
      >
        <ChevronLeft className="size-4" />
      </button>
      <input
        type="date"
        value={workDate}
        onChange={(e) => onChange(e.target.value)}
        className="rounded-md border border-[var(--border)] bg-[var(--surface)] px-2 py-1.5 text-sm text-[var(--ink)] outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
      />
      <button
        type="button"
        onClick={() => go(1)}
        className="rounded-md border border-[var(--border)] bg-[var(--surface)] p-1.5 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
        aria-label="Día siguiente"
      >
        <ChevronRight className="size-4" />
      </button>
      <span className="hidden text-sm capitalize text-[var(--muted)] sm:inline">
        {format(parseISO(workDate), "EEEE d MMM yyyy", { locale: es })}
      </span>
    </div>
  );
}
