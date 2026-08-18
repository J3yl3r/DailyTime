"use client";

import type { CSSProperties, HTMLAttributes } from "react";
import { Clock, Trash2, GripVertical, ListPlus, Pencil } from "lucide-react";
import type { TaskItem } from "@/types/api";
import { useTaskItemMutations } from "@/hooks/mutations/use-task-item-mutations";
import { formatDurationMinutes } from "@/lib/utils/time";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";
import { toast } from "sonner";

type Props = {
  task: TaskItem;
  workDate: string;
  onAddTime?: (taskItemId: number, label: string) => void;
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

export function TaskCard({
  task,
  workDate,
  onAddTime,
  onAddChild,
  onEdit,
  depth = 1,
  dragHandleProps,
  isDragging,
  className,
}: Props) {
  const { remove } = useTaskItemMutations(workDate);
  const confirm = useConfirm();
  const isCompleted = task.status?.isFinal ?? task.isCompleted;
  const schedule =
    task.startTime && task.endTime
      ? `${task.startTime.slice(0, 5)}–${task.endTime.slice(0, 5)}`
      : null;

  const handleDelete = async () => {
    const ok = await confirm({
      title: "Eliminar tarea",
      description: `¿Estás seguro de eliminar “${task.title}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(task.id, {
      onSuccess: () => toast.success("Tarea eliminada"),
      onError: (e) => toast.error(e.message),
    });
  };

  return (
    <article
      className={cn(
        "group relative flex flex-col gap-2 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 shadow-[var(--shadow-card)] transition-shadow",
        isDragging && "opacity-90 shadow-lg ring-2 ring-[var(--accent)]",
        isCompleted && "opacity-75",
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
          <p
            className={cn(
              "truncate text-sm font-medium leading-snug text-[var(--ink)]",
              isCompleted && "text-[var(--muted)] line-through"
            )}
            title={task.title}
          >
            {task.title}
          </p>
          {task.content?.trim() ? (
            <p
              className="mt-0.5 line-clamp-2 text-xs leading-relaxed text-[var(--muted)]"
              title={task.content}
            >
              {task.content}
            </p>
          ) : null}

          <div className="mt-1.5 flex flex-wrap items-center gap-1">
            <span
              className="rounded border px-1 py-0.5 text-[10px] font-semibold uppercase tracking-wide"
              style={
                task.status
                  ? {
                      borderColor: task.status.color,
                      color: task.status.color,
                      backgroundColor: `${task.status.color}18`,
                    }
                  : undefined
              }
            >
              {task.status?.name ?? "Sin estado"}
            </span>
            <span className="rounded bg-sky-100 px-1 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-sky-700">
              {task.category?.name ?? "Sin categoría"}
            </span>
            {depth > 1 && (
              <span className="text-[10px] font-medium text-[var(--muted)]">
                Niv. {depth}
              </span>
            )}
          </div>

          <div className="mt-1.5 grid grid-cols-2 gap-x-2 gap-y-0.5">
            <MetaRow label="Empresa" value={task.company?.name} />
            <MetaRow label="Proyecto" value={task.project?.name} />
            <MetaRow label="Persona" value={task.person?.name} />
            <MetaRow label="Fecha" value={task.workDate} />
            <MetaRow label="Horario" value={schedule} />
            <MetaRow
              label="Tiempo"
              value={
                task.durationMinutes > 0
                  ? formatDurationMinutes(task.durationMinutes)
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
            className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs text-[var(--muted)] hover:bg-[var(--accent-soft)] hover:text-[var(--accent)]"
          >
            <ListPlus className="size-3.5" />
            Subtarea
          </button>
        )}
        {onAddTime && (
          <button
            type="button"
            onClick={() => onAddTime(task.id, task.title)}
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
