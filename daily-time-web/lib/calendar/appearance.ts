/**
 * Reglas de color del calendario, en un solo sitio para que las vistas de día, semana, mes y
 * año digan lo mismo:
 * - lo que viene de Google se pinta con el color que tiene allí,
 * - lo tuyo usa la paleta de DailyTime (verde agua las tareas, ámbar las notas),
 * - la franja izquierda es siempre el color del estado.
 */

import type { CSSProperties } from "react";

type ItemAppearance = {
  syncSource: string | null;
  googleColor: string | null;
  status: { color: string } | null;
};

export type CalendarItemKind = "task" | "note";

/**
 * Cierto solo si el elemento nació en Google. Una tarea tuya que además está espejada allí
 * sigue siendo tuya: eso lo indica el icono, no el color.
 */
export function isFromGoogle(item: { syncSource: string | null }): boolean {
  return item.syncSource === "google";
}

/** Clases de relleno y texto para lo que usa la paleta propia de DailyTime. */
function localClasses(kind: CalendarItemKind): string {
  return kind === "task"
    ? "border-[var(--accent)]/30 bg-[var(--accent-soft)] text-[var(--accent-strong)]"
    : "border-amber-300/70 bg-amber-100 text-[var(--note)]";
}

/**
 * Luminancia percibida del color, para decidir si el texto encima va oscuro o claro. La
 * paleta de Google va del pastel al rojo fuerte, así que un color de texto fijo no sirve.
 */
function isLight(hex: string): boolean {
  const limpio = hex.replace("#", "");
  if (limpio.length !== 6) return true;
  const r = parseInt(limpio.slice(0, 2), 16);
  const g = parseInt(limpio.slice(2, 4), 16);
  const b = parseInt(limpio.slice(4, 6), 16);
  if ([r, g, b].some(Number.isNaN)) return true;
  return (r * 299 + g * 587 + b * 114) / 1000 > 145;
}

/**
 * Clases y estilos en línea de un elemento. El color de Google no puede ir en una clase de
 * Tailwind porque es un valor que llega en tiempo de ejecución.
 */
export function itemAppearance(
  item: ItemAppearance,
  kind: CalendarItemKind,
): { className: string; style: CSSProperties } {
  const style: CSSProperties = {};

  if (isFromGoogle(item) && item.googleColor) {
    style.backgroundColor = item.googleColor;
    style.color = isLight(item.googleColor) ? "#1d1d1d" : "#ffffff";
    // El borde toma el propio relleno: cualquier otro color en el filo se lee como si
    // asomara otro bloque por detrás.
    style.borderColor = item.googleColor;
    return { className: "border", style };
  }

  // De Google pero sin color todavía (aún no lo ha traído una sincronización): gris neutro.
  const className = isFromGoogle(item)
    ? "border-slate-300/80 bg-slate-100 text-slate-700"
    : localClasses(kind);

  return { className, style };
}

/**
 * Color del indicador de estado. Va dentro del bloque, nunca en el filo: en el borde parece
 * un segundo elemento asomando por detrás del color de Google.
 */
export function statusMarkColor(item: { status: { color: string } | null }): string | null {
  return item.status?.color ?? null;
}

/** Anillo de foco al pasar el ratón, en la familia que corresponda. */
export function hoverRingClasses(
  item: { syncSource: string | null },
  kind: CalendarItemKind,
): string {
  if (isFromGoogle(item)) return "hover:ring-2 hover:ring-slate-400/50";
  return kind === "task"
    ? "hover:ring-2 hover:ring-[var(--accent)]/30"
    : "hover:ring-2 hover:ring-amber-400/40";
}
