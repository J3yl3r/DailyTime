import { endOfMonth, endOfYear, format, isValid, parseISO, startOfMonth, startOfYear } from "date-fns";

export type DateFilterMode = "today" | "day" | "month" | "year";

export type DateFilterValue = {
  mode: DateFilterMode;
  date: string;
};

export type DateRange = {
  fromDate: string;
  toDate: string;
  anchorDate: string;
};

export function toApiDate(date: Date): string {
  return format(date, "yyyy-MM-dd");
}

export function todayApiDate(): string {
  return toApiDate(new Date());
}

export function parseApiDate(value: string): Date {
  const d = parseISO(value);
  if (!isValid(d)) throw new Error(`Fecha inválida: ${value}`);
  return d;
}

export function createDefaultDateFilter(): DateFilterValue {
  return { mode: "today", date: todayApiDate() };
}

export function getDateFilterRange(filter: DateFilterValue): DateRange {
  const anchorDate = filter.mode === "today" ? todayApiDate() : filter.date;
  const date = parseApiDate(anchorDate);

  if (filter.mode === "month") {
    return {
      fromDate: toApiDate(startOfMonth(date)),
      toDate: toApiDate(endOfMonth(date)),
      anchorDate,
    };
  }

  if (filter.mode === "year") {
    return {
      fromDate: toApiDate(startOfYear(date)),
      toDate: toApiDate(endOfYear(date)),
      anchorDate,
    };
  }

  return { fromDate: anchorDate, toDate: anchorDate, anchorDate };
}

/** Normaliza "HH:mm" a "HH:mm:ss" para System.Text.Json / TimeOnly. */
export function toApiTime(value: string | null | undefined): string | null {
  if (!value) return null;
  const trimmed = value.trim();
  if (/^\d{2}:\d{2}$/.test(trimmed)) return `${trimmed}:00`;
  if (/^\d{2}:\d{2}:\d{2}$/.test(trimmed)) return trimmed;
  return trimmed.slice(0, 8);
}

export function toDisplayTime(value: string | null | undefined): string {
  if (!value) return "";
  return value.slice(0, 5);
}