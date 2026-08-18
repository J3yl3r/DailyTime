"use client";

import { useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { VaultService } from "@/types/api";
import { useVaultServices } from "@/hooks/queries/use-vault-services";
import { useVaultServiceMutations } from "@/hooks/mutations/use-vault-service-mutations";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

function ServiceForm({
  initial,
  onSubmit,
  onClose,
  submitLabel,
  pending,
}: {
  initial?: Partial<VaultService>;
  onSubmit: (values: {
    name: string;
    url: string;
    notes: string;
    isActive: boolean;
  }) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [url, setUrl] = useState(initial?.url ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!name.trim()) {
          toast.error("El nombre del servicio es obligatorio");
          return;
        }
        onSubmit({
          name: name.trim(),
          url: url.trim(),
          notes: notes.trim(),
          isActive,
        });
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Nombre
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Gmail"
          className={inputClass}
          autoFocus
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        URL
        <input
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder="https://..."
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Notas
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          className={inputClass}
        />
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
        Activo
      </label>
      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={pending}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {pending ? "Guardando…" : submitLabel}
        </button>
      </div>
    </form>
  );
}

export function VaultServicesView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<VaultService | null>(null);
  const query = useVaultServices();
  const mutations = useVaultServiceMutations();
  const confirm = useConfirm();
  const items = query.data ?? [];

  const create = async (values: {
    name: string;
    url: string;
    notes: string;
    isActive: boolean;
  }) => {
    try {
      await mutations.create.mutateAsync(values);
      setCreating(false);
      toast.success("Servicio creado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al crear");
    }
  };

  const update = async (values: {
    name: string;
    url: string;
    notes: string;
    isActive: boolean;
  }) => {
    if (!editing) return;
    try {
      await mutations.update.mutateAsync({ id: editing.id, body: values });
      setEditing(null);
      toast.success("Servicio actualizado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al actualizar");
    }
  };

  const remove = async (item: VaultService) => {
    const ok = await confirm({
      title: "Eliminar servicio",
      description: `¿Estás seguro de eliminar el servicio “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Servicio eliminado"),
      onError: (error) => toast.error(error.message),
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Catálogo reutilizable de servicios (Gmail, AWS, LinkedIn…). Las
          contraseñas apuntan a un servicio.
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nuevo servicio"
        >
          <ServiceForm
            onClose={() => setCreating(false)}
            onSubmit={create}
            submitLabel="Crear"
            pending={mutations.create.isPending}
          />
        </CreatePanel>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && (
        <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>
      )}
      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Aún no hay servicios. Crea el primero (ej. Gmail).
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
                  {item.url ? (
                    <p className="truncate text-sm text-[var(--muted)]">{item.url}</p>
                  ) : null}
                  {item.notes ? (
                    <p className="text-sm text-[var(--muted)]">{item.notes}</p>
                  ) : null}
                  <p className="mt-1 text-xs text-[var(--muted)]">
                    {item.passwordCount} contraseña(s)
                    {!item.isActive ? " · Inactivo" : ""}
                  </p>
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
                    onClick={() => remove(item)}
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
        title="Editar servicio"
        size="xs"
      >
        {editing ? (
          <ServiceForm
            initial={editing}
            onClose={() => setEditing(null)}
            onSubmit={update}
            submitLabel="Actualizar"
            pending={mutations.update.isPending}
          />
        ) : null}
      </FormModal>
    </div>
  );
}
