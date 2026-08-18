"use client";

import type { CSSProperties, HTMLAttributes } from "react";
import { Clock, Trash2, GripVertical, ListPlus, Pencil } from "lucide-react";
import type { Note } from "@/types/api";
import { useNoteMutations } from "@/hooks/mutations/use-note-mutations";
import { formatDurationMinutes } from "@/lib/utils/time";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";
import { toast } from "sonner";

type Props = {
  note: Note;
  workDate: string;
  onAddTime?: (noteId: number, label: string) => void;
  onAddChild?: () => void;
  onEdit?: () => void;
  depth?: number;
  dragHandleProps?: HTMLAttributes<HTMLButtonElement> & Record<string, unknown>;
  isDragging?: boolean;
  className?: string;
  style?: CSSProperties;
};

function MetaRow({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  if (!value?.trim()) return null;
  return (
    <p className="min-w-0 truncate text-[11px] leading-snug text-[var(--muted)]">
      <span className="font-medium text-[var(--ink)]/70">{label}: </span>
      {value}
    </p>
  );
}

export function NoteCard({
  note,
  workDate,
  onAddTime,
  onAddChild,
  onEdit,
  depth = 1,
  dragHandleProps,
  isDragging,
  className,
}: Props) {
  const { remove } = useNoteMutations(workDate);
  const confirm = useConfirm();
  const label = note.title?.trim() || note.content.slice(0, 40) || "Nota";
  const schedule =
    note.startTime && note.endTime
      ? `${note.startTime.slice(0, 5)}–${note.endTime.slice(0, 5)}`
      : null;

  const handleDelete = async () => {
    const ok = await confirm({
      title: "Eliminar nota",
      description: `¿Estás seguro de eliminar “${label}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(note.id, {
      onSuccess: () => toast.success("Nota eliminada"),
      onError: (e) => toast.error(e.message),
    });
  };

  return (
    <article
      className={cn(
        "group relative flex flex-col gap-2 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 shadow-[var(--shadow-card)] transition-shadow",
        isDragging && "opacity-90 shadow-lg ring-2 ring-[var(--note)]",
        className
      )}
    >
      <div className="flex items-start gap-1.5">
        {dragHandleProps && (
          <button
            type="button"
            className="mt-0.5 cursor-grab touch-none rounded p-0.5 text-[var(--muted)] hover:bg-[var(--surface-muted)] active:cursor-grabbing"
            aria-label="Arrastrar"
            {...dragHandleProps}
          >
            <GripVertical className="size-3.5" />
          </button>
        )}
        <div className="min-w-0 flex-1">
          {note.title?.trim() ? (
            <p
              className="truncate text-sm font-medium leading-snug text-[var(--ink)]"
              title={note.title}
            >
              {note.title}
            </p>
          ) : null}
          <p
            className={cn(
              "line-clamp-2 text-xs leading-relaxed text-[var(--muted)]",
              note.title?.trim() && "mt-0.5"
            )}
            title={note.content}
          >
            {note.content}
          </p>

          <div className="mt-1.5 flex flex-wrap items-center gap-1">
            <span
              className="rounded border px-1 py-0.5 text-[10px] font-semibold uppercase tracking-wide"
              style={
                note.status
                  ? {
                      borderColor: note.status.color,
                      color: note.status.color,
                      backgroundColor: `${note.status.color}18`,
                    }
                  : undefined
              }
            >
              {note.status?.name ?? "Sin estado"}
            </span>
            <span className="rounded bg-sky-100 px-1 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-sky-700">
              {note.category?.name ?? "Sin categoría"}
            </span>
            {depth > 1 && (
              <span className="text-[10px] font-medium text-[var(--muted)]">
                Niv. {depth}
              </span>
            )}
          </div>

          <div className="mt-1.5 grid grid-cols-2 gap-x-2 gap-y-0.5">
            <MetaRow label="Empresa" value={note.company?.name} />
            <MetaRow label="Proyecto" value={note.project?.name} />
            <MetaRow label="Persona" value={note.person?.name} />
            <MetaRow label="Fecha" value={note.workDate} />
            <MetaRow label="Horario" value={schedule} />
            <MetaRow
              label="Tiempo"
              value={
                note.durationMinutes > 0
                  ? formatDurationMinutes(note.durationMinutes)
                  : null
              }
            />
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-end gap-0.5 border-t border-[var(--border)]/60 pt-1.5 opacity-80 transition-opacity group-hover:opacity-100">
        {onEdit && (
          <button
            type="button"
            onClick={onEdit}
            className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]"
          >
            <Pencil className="size-3.5" />
            Editar
          </button>
        )}
        {onAddChild && (
          <button
            type="button"
            onClick={onAddChild}
            className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-[var(--muted)] hover:bg-amber-100 hover:text-[var(--note)]"
          >
            <ListPlus className="size-3.5" />
            Subnota
          </button>
        )}
        {onAddTime && (
          <button
            type="button"
            onClick={() => onAddTime(note.id, label)}
            className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]"
          >
            <Clock className="size-3.5" />
            Tiempo
          </button>
        )}
        <button
          type="button"
          onClick={handleDelete}
          disabled={remove.isPending}
          className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)] disabled:opacity-50"
        >
          <Trash2 className="size-3.5" />
          Eliminar
        </button>
      </div>
    </article>
  );
}
