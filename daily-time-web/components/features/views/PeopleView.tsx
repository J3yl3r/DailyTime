"use client";

import { useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { Person } from "@/types/api";
import { usePeople } from "@/hooks/queries/use-people";
import { usePersonMutations } from "@/hooks/mutations/use-person-mutations";
import { PersonForm } from "@/components/features/people/PersonForm";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";

export function PeopleView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Person | null>(null);
  const query = usePeople();
  const { remove } = usePersonMutations();
  const confirm = useConfirm();

  const deleteItem = async (item: Person) => {
    const ok = await confirm({
      title: "Eliminar persona",
      description: `¿Estás seguro de eliminar “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(item.id, {
      onSuccess: () => toast.success("Persona eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  const items = query.data ?? [];

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Personas que puedes asociar a tus tareas y notas.
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nueva persona"
        >
          <PersonForm onClose={() => setCreating(false)} />
        </CreatePanel>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>}
      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Aún no has agregado personas.
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
                    <p className="text-xs text-[var(--muted)]">Inactiva</p>
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
        title="Editar persona"
        size="xs"
      >
        {editing && <PersonForm person={editing} onClose={() => setEditing(null)} />}
      </FormModal>
    </div>
  );
}
