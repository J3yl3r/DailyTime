"use client";

import { useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { CareerCatalog, CareerCatalogKind } from "@/types/api";
import { useCareerCatalog } from "@/hooks/queries/use-career-catalog";
import { useCareerCatalogMutations } from "@/hooks/mutations/use-career-catalog-mutations";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

const TABS: { kind: CareerCatalogKind; label: string; hint: string }[] = [
  { kind: "companies", label: "Empresas", hint: "Empresas donde trabajaste o a las que postulaste." },
  { kind: "positions", label: "Cargos", hint: "Roles / títulos reutilizables." },
  { kind: "locations", label: "Ubicaciones", hint: "Ciudad, remoto, híbrido…" },
  { kind: "fields", label: "Carreras", hint: "Área profesional (ej. Desarrollo de software)." },
  { kind: "technologies", label: "Tecnologías", hint: "Stack reutilizable en experiencias." },
  {
    kind: "application-statuses",
    label: "Estados",
    hint: "Estados del proceso de postulación (Enviada, Entrevista, etc.).",
  },
];

function CatalogForm({
  initial,
  onSubmit,
  onClose,
  submitLabel,
  pending,
  showStatusExtras = false,
}: {
  initial?: Partial<CareerCatalog>;
  onSubmit: (values: {
    name: string;
    description: string;
    color: string;
    sortOrder: number;
    isActive: boolean;
  }) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
  showStatusExtras?: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [color, setColor] = useState(initial?.color ?? "#64748B");
  const [sortOrder, setSortOrder] = useState(String(initial?.sortOrder ?? 0));
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!name.trim()) {
          toast.error("El nombre es obligatorio");
          return;
        }
        onSubmit({
          name: name.trim(),
          description: description.trim(),
          color: color.trim() || "#64748B",
          sortOrder: Number(sortOrder) || 0,
          isActive,
        });
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Nombre
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={inputClass}
          autoFocus
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Descripción
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={2}
          className={inputClass}
        />
      </label>
      {showStatusExtras ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Color
            <input
              type="color"
              value={color || "#64748B"}
              onChange={(e) => setColor(e.target.value)}
              className="h-10 w-full cursor-pointer rounded-md border border-[var(--border)] bg-white p-1"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Orden
            <input
              type="number"
              value={sortOrder}
              onChange={(e) => setSortOrder(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
      ) : null}
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
        Activo
      </label>
      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        <button type="button" onClick={onClose} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm">
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

function CatalogSection({ kind, label, hint }: (typeof TABS)[number]) {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<CareerCatalog | null>(null);
  const query = useCareerCatalog(kind);
  const mutations = useCareerCatalogMutations(kind);
  const confirm = useConfirm();
  const items = query.data ?? [];
  const showStatusExtras = kind === "application-statuses";

  const create = async (values: {
    name: string;
    description: string;
    color: string;
    sortOrder: number;
    isActive: boolean;
  }) => {
    try {
      await mutations.create.mutateAsync(values);
      setCreating(false);
      toast.success("Creado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error");
    }
  };

  const update = async (values: {
    name: string;
    description: string;
    color: string;
    sortOrder: number;
    isActive: boolean;
  }) => {
    if (!editing) return;
    try {
      await mutations.update.mutateAsync({ id: editing.id, body: values });
      setEditing(null);
      toast.success("Actualizado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error");
    }
  };

  const remove = async (item: CareerCatalog) => {
    const ok = await confirm({
      title: "Eliminar elemento",
      description: `¿Estás seguro de eliminar “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Eliminado"),
      onError: (error) => toast.error(error.message),
    });
  };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="font-medium text-[var(--ink)]">{label}</h2>
          <p className="text-sm text-[var(--muted)]">{hint}</p>
        </div>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label={`Nuevo`}
        >
          <CatalogForm
            onClose={() => setCreating(false)}
            onSubmit={create}
            submitLabel="Crear"
            pending={mutations.create.isPending}
            showStatusExtras={showStatusExtras}
          />
        </CreatePanel>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>}
      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Vacío. Crea el primero.
          </p>
        ) : (
          <div className="flex flex-col gap-2">
            {items.map((item) => (
              <article
                key={item.id}
                className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]"
              >
                <div className="flex items-start gap-3">
                  {showStatusExtras ? (
                    <span
                      className="mt-1 size-3 shrink-0 rounded-full"
                      style={{ backgroundColor: item.color || "#64748B" }}
                      aria-hidden
                    />
                  ) : null}
                  <div>
                    <p className="font-medium">{item.name}</p>
                    {item.description ? (
                      <p className="text-sm text-[var(--muted)]">{item.description}</p>
                    ) : null}
                    {!item.isActive ? (
                      <p className="text-xs text-[var(--muted)]">Inactivo</p>
                    ) : null}
                  </div>
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
        title={`Editar ${label.toLowerCase()}`}
        size="xs"
      >
        {editing ? (
          <CatalogForm
            initial={editing}
            onClose={() => setEditing(null)}
            onSubmit={update}
            submitLabel="Actualizar"
            pending={mutations.update.isPending}
            showStatusExtras={showStatusExtras}
          />
        ) : null}
      </FormModal>
    </section>
  );
}

export function CareerCatalogsView() {
  const [tab, setTab] = useState<CareerCatalogKind>("companies");
  const current = TABS.find((t) => t.kind === tab) ?? TABS[0];

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <p className="text-sm text-[var(--muted)]">
        Catálogos compartidos por Experiencias y Postulaciones. Créalos una vez y reutilízalos.
      </p>
      <div className="flex flex-wrap gap-2">
        {TABS.map((item) => (
          <button
            key={item.kind}
            type="button"
            onClick={() => setTab(item.kind)}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm",
              tab === item.kind
                ? "bg-[var(--accent)] text-white"
                : "border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)]",
            )}
          >
            {item.label}
          </button>
        ))}
      </div>
      <CatalogSection {...current} />
    </div>
  );
}
