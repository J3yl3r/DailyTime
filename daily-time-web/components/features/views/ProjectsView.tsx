"use client";

import { useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { Project } from "@/types/api";
import { useProjects } from "@/hooks/queries/use-projects";
import { useProjectMutations } from "@/hooks/mutations/use-project-mutations";
import { ProjectForm } from "@/components/features/projects/ProjectForm";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";

export function ProjectsView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Project | null>(null);
  const query = useProjects();
  const { remove } = useProjectMutations();
  const confirm = useConfirm();

  const deleteItem = async (item: Project) => {
    const ok = await confirm({
      title: "Eliminar proyecto",
      description: `¿Estás seguro de eliminar “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(item.id, {
      onSuccess: () => toast.success("Proyecto eliminado"),
      onError: (error) => toast.error(error.message),
    });
  };

  const items = query.data ?? [];

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Proyectos que puedes asociar a tus tareas y notas.
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nuevo proyecto"
        >
          <ProjectForm onClose={() => setCreating(false)} />
        </CreatePanel>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>}
      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Aún no has agregado proyectos.
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
                  {item.description && (
                    <p className="text-sm text-[var(--muted)]">{item.description}</p>
                  )}
                  {!item.isActive && (
                    <p className="text-xs text-[var(--muted)]">Inactivo</p>
                  )}
                </div>
                <div className="flex gap-1">
                  <button
                    type="button"
                    onClick={() => setEditing(item)}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                  >
                    <Pencil className="size-3.5" /> Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => deleteItem(item)}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
                  >
                    <Trash2 className="size-3.5" /> Eliminar
                  </button>
                </div>
              </article>
            ))}
          </div>
        )
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => !open && setEditing(null)}
        title="Editar proyecto"
        size="xs"
      >
        {editing && <ProjectForm project={editing} onClose={() => setEditing(null)} />}
      </FormModal>
    </div>
  );
}
