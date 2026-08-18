"use client";

import { useMemo, useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { WorkItemStatus, WorkItemType } from "@/types/api";
import { useStatuses } from "@/hooks/queries/use-statuses";
import { useStatusMutations } from "@/hooks/mutations/use-status-mutations";
import { StatusForm } from "@/components/features/statuses/StatusForm";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";

const TYPE_LABEL: Record<WorkItemType, string> = {
  task: "Tarea",
  note: "Nota",
};

export function StatusesView() {
  const [filter, setFilter] = useState<WorkItemType | "all">("all");
  const [showCreate, setShowCreate] = useState(false);
  const [editing, setEditing] = useState<WorkItemStatus | null>(null);
  const query = useStatuses(filter === "all" ? undefined : filter);
  const { remove } = useStatusMutations();
  const confirm = useConfirm();

  const groups = useMemo(() => {
    const items = query.data ?? [];
    return {
      task: items.filter((s) => s.itemType === "task"),
      note: items.filter((s) => s.itemType === "note"),
    };
  }, [query.data]);

  const handleDelete = async (status: WorkItemStatus) => {
    const ok = await confirm({
      title: "Eliminar estado",
      description: `¿Estás seguro de eliminar el estado “${status.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(status.id, {
      onSuccess: () => toast.success("Estado eliminado"),
      onError: (e) => toast.error(e.message),
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <p className="text-sm text-[var(--muted)]">
          Administra los estados de tareas y notas. Usa ItemType para separarlos.
        </p>
        <CreatePanel
          open={showCreate}
          onOpen={() => setShowCreate(true)}
          onClose={() => setShowCreate(false)}
          label="Nuevo estado"
        >
          <StatusForm onClose={() => setShowCreate(false)} />
        </CreatePanel>
      </div>

      <div className="flex flex-wrap gap-2">
        {(
          [
            ["all", "Todos"],
            ["task", "Tareas"],
            ["note", "Notas"],
          ] as const
        ).map(([value, label]) => (
          <button
            key={value}
            type="button"
            onClick={() => setFilter(value)}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm transition-colors",
              filter === value
                ? "bg-[var(--accent)] text-white"
                : "border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)] hover:text-[var(--ink)]"
            )}
          >
            {label}
          </button>
        ))}
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando estados…</p>}
      {query.error && (
        <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>
      )}

      {!query.isLoading && !query.error && (
        <div className="flex flex-col gap-6">
          {(filter === "all" || filter === "task") && (
            <StatusGroup
              title="Estados de tareas"
              items={groups.task}
              onEdit={setEditing}
              onDelete={handleDelete}
              deleting={remove.isPending}
            />
          )}
          {(filter === "all" || filter === "note") && (
            <StatusGroup
              title="Estados de notas"
              items={groups.note}
              onEdit={setEditing}
              onDelete={handleDelete}
              deleting={remove.isPending}
            />
          )}
        </div>
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => {
          if (!open) setEditing(null);
        }}
        title="Editar estado"
        description={
          editing
            ? `Tipo: ${TYPE_LABEL[(editing.itemType as WorkItemType) ?? "task"] ?? editing.itemType}`
            : undefined
        }
        size="xs"
      >
        {editing ? (
          <StatusForm status={editing} onClose={() => setEditing(null)} />
        ) : null}
      </FormModal>
    </div>
  );
}

function StatusGroup({
  title,
  items,
  onEdit,
  onDelete,
  deleting,
}: {
  title: string;
  items: WorkItemStatus[];
  onEdit: (status: WorkItemStatus) => void;
  onDelete: (status: WorkItemStatus) => void;
  deleting: boolean;
}) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-[family-name:var(--font-fraunces)] text-lg font-semibold text-[var(--ink)]">
        {title}
      </h2>
      {!items.length ? (
        <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
          No hay estados en este grupo.
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {items.map((status) => (
            <li
              key={status.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]"
            >
              <div className="flex min-w-0 items-start gap-3">
                <span
                  className="mt-1 size-3 shrink-0 rounded-full ring-2 ring-white shadow-sm"
                  style={{ backgroundColor: status.color }}
                  aria-label={`Color ${status.color}`}
                />
                <div className="min-w-0">
                  <p className="font-medium text-[var(--ink)]">{status.name}</p>
                  <p className="text-sm text-[var(--muted)]">{status.description}</p>
                  <p className="text-xs text-[var(--muted)]">
                    {status.color}
                    {status.isFinal ? " · final" : ""}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => onEdit(status)}
                  className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]"
                >
                  <Pencil className="size-3.5" />
                  Editar
                </button>
                <button
                  type="button"
                  onClick={() => onDelete(status)}
                  disabled={deleting}
                  className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)] disabled:opacity-50"
                >
                  <Trash2 className="size-3.5" />
                  Eliminar
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
