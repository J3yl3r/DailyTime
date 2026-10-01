"use client";

import { CalendarSync } from "lucide-react";
import type { Note, TaskItem } from "@/types/api";
import { cn } from "@/lib/utils/cn";
import {
  hoverRingClasses,
  isFromGoogle,
  itemAppearance,
  statusMarkColor,
} from "@/lib/calendar/appearance";

export type ScheduleItem =
  | { kind: "task"; item: TaskItem }
  | { kind: "note"; item: Note };

/**
 * Un bloque de la grilla. Codifica tres cosas a la vez sin ensuciarse:
 * - el relleno dice de dónde viene (tuyo de DailyTime, o traído de Google),
 * - la franja de la izquierda es el color del estado,
 * - la familia de color separa tareas de notas.
 */
export function ScheduleBlock({
  entry,
  startTime,
  endTime,
  top,
  height,
  left,
  width,
  columnCount,
  onClick,
}: {
  entry: ScheduleItem;
  startTime: string;
  endTime: string;
  top: number;
  height: number;
  /** Fracciones 0–1 del ancho del día. */
  left: number;
  width: number;
  columnCount: number;
  onClick: () => void;
}) {
  const { kind, item } = entry;
  const deGoogle = isFromGoogle(item);
  const enGoogle = Boolean(item.googleEventId);
  const etiqueta =
    kind === "task"
      ? (item as TaskItem).title
      : (item as Note).title?.trim() || (item as Note).content;

  const { className: colorClasses, style: colorStyle } = itemAppearance(item, kind);
  const colorEstado = statusMarkColor(item);
  const horas = `${startTime.slice(0, 5)}–${endTime.slice(0, 5)}`;
  // Por debajo de esta altura no cabe una segunda línea sin recortarla a la mitad.
  const soloTitulo = height < 34;
  const estrecho = columnCount > 2;

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "absolute z-10 flex flex-col overflow-hidden rounded-md border py-0.5 pr-1.5 pl-2.5 text-left text-[11px] shadow-sm transition-shadow hover:z-30 hover:shadow-md",
        colorClasses,
        hoverRingClasses(item, kind),
      )}
      style={{
        ...colorStyle,
        top,
        // 2px menos de alto y de ancho: así dos bloques pegados (uno acaba cuando empieza
        // el otro) se leen como dos, no como uno solo.
        height: Math.max(height - 2, 16),
        left: `calc(${left * 100}% + 2px)`,
        width: `calc(${width * 100}% - 4px)`,
      }}
      title={`${etiqueta} · ${horas}${deGoogle ? " · de Google Calendar" : ""}${
        item.status ? ` · ${item.status.name}` : ""
      }`}
    >
      {colorEstado ? (
        <span
          aria-hidden
          className="pointer-events-none absolute top-1 bottom-1 left-0.5 w-[3px] rounded-full"
          style={{ backgroundColor: colorEstado }}
        />
      ) : null}
      <span className="block truncate font-semibold">
        {enGoogle ? (
          <CalendarSync
            className="mr-1 inline size-3 shrink-0 align-[-2px]"
            aria-label="Sincronizada con Google Calendar"
          />
        ) : null}
        {etiqueta}
      </span>
      {!soloTitulo && !estrecho ? (
        <span className="block truncate text-[10px] opacity-80">{horas}</span>
      ) : null}
    </button>
  );
}
