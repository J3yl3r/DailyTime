"use client";

import { useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { WorkItemCategory, WorkItemType } from "@/types/api";
import { useCategories } from "@/hooks/queries/use-categories";
import { useCategoryMutations } from "@/hooks/mutations/use-category-mutations";
import { CategoryForm } from "@/components/features/categories/CategoryForm";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";

export function CategoriesView() {
  const [filter, setFilter] = useState<WorkItemType | undefined>();
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<WorkItemCategory | null>(null);
  const query = useCategories(filter);
  const { remove } = useCategoryMutations();
  const confirm = useConfirm();

  const deleteItem = async (item: WorkItemCategory) => {
    const ok = await confirm({
      title: "Eliminar categoría",
      description: `¿Estás seguro de eliminar “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(item.id, {
      onSuccess: () => toast.success("Categoría eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Categorías configurables para tareas y notas.
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nueva categoría"
        >
          <CategoryForm onClose={() => setCreating(false)} />
        </CreatePanel>
      </div>

      <div className="flex gap-2">
        {([
          [undefined, "Todas"],
          ["task", "Tareas"],
          ["note", "Notas"],
        ] as const).map(([value, label]) => (
          <button
            key={label}
            type="button"
            onClick={() => setFilter(value)}
            className={`rounded-md px-3 py-1.5 text-sm ${
              filter === value
                ? "bg-[var(--accent)] text-white"
                : "border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)]"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>}
      {!query.isLoading && !query.error && (
        <div className="flex flex-col gap-6">
          {(filter === undefined || filter === "task") && (
            <CategoryGroup
              title="Categorías de tareas"
              items={(query.data ?? []).filter((item) => item.itemType === "task")}
              onEdit={setEditing}
              onDelete={deleteItem}
            />
          )}
          {(filter === undefined || filter === "note") && (
            <CategoryGroup
              title="Categorías de notas"
              items={(query.data ?? []).filter((item) => item.itemType === "note")}
              onEdit={setEditing}
              onDelete={deleteItem}
            />
          )}
        </div>
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => !open && setEditing(null)}
        title="Editar categoría"
        size="xs"
      >
        {editing && <CategoryForm category={editing} onClose={() => setEditing(null)} />}
      </FormModal>
    </div>
  );
}

function CategoryGroup({
  title,
  items,
  onEdit,
  onDelete,
}: {
  title: string;
  items: WorkItemCategory[];
  onEdit: (item: WorkItemCategory) => void;
  onDelete: (item: WorkItemCategory) => void;
}) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-[family-name:var(--font-fraunces)] text-lg font-semibold text-[var(--ink)]">
        {title}
      </h2>
      {!items.length ? (
        <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
          No hay categorías en este grupo.
        </p>
      ) : (
        <div className="flex flex-col gap-2">
          {items.map((item) => (
            <article
              key={item.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]"
            >
              <div className="min-w-0">
                <p className="font-medium">{item.name}</p>
                <p className="text-sm text-[var(--muted)]">{item.description}</p>
                {!item.isActive && (
                  <p className="text-xs text-[var(--muted)]">Inactiva</p>
                )}
              </div>
              <div className="flex gap-1">
                <button
                  type="button"
                  onClick={() => onEdit(item)}
                  className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                >
                  <Pencil className="size-3.5" /> Editar
                </button>
                <button
                  type="button"
                  onClick={() => onDelete(item)}
                  className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
                >
                  <Trash2 className="size-3.5" /> Eliminar
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
