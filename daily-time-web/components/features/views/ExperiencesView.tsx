"use client";

import { useEffect, useMemo, useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { WorkExperience } from "@/types/api";
import { useWorkExperiences } from "@/hooks/queries/use-work-experiences";
import { useCareerCatalog } from "@/hooks/queries/use-career-catalog";
import { useWorkExperienceMutations } from "@/hooks/mutations/use-work-experience-mutations";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";
import { cn } from "@/lib/utils/cn";
import Link from "next/link";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

type ExperienceDraft = {
  companyId: number | "";
  positionId: number | "";
  locationId: number | "";
  fieldId: number | "";
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  summary: string;
  achievements: string;
  technologyIds: number[];
};

const EMPTY: ExperienceDraft = {
  companyId: "",
  positionId: "",
  locationId: "",
  fieldId: "",
  startDate: "",
  endDate: "",
  isCurrent: false,
  summary: "",
  achievements: "",
  technologyIds: [],
};

function ExperienceForm({
  initial,
  onSubmit,
  onClose,
  submitLabel,
  pending,
}: {
  initial?: ExperienceDraft;
  onSubmit: (draft: ExperienceDraft) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
}) {
  const companies = useCareerCatalog("companies", true).data ?? [];
  const positions = useCareerCatalog("positions", true).data ?? [];
  const locations = useCareerCatalog("locations", true).data ?? [];
  const fields = useCareerCatalog("fields", true).data ?? [];
  const technologies = useCareerCatalog("technologies", true).data ?? [];
  const [draft, setDraft] = useState<ExperienceDraft>(initial ?? EMPTY);

  const toggleTech = (id: number) => {
    setDraft((c) => ({
      ...c,
      technologyIds: c.technologyIds.includes(id)
        ? c.technologyIds.filter((x) => x !== id)
        : [...c.technologyIds, id],
    }));
  };

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!draft.companyId || !draft.positionId || !draft.startDate) {
          toast.error("Empresa, cargo e inicio son obligatorios");
          return;
        }
        onSubmit(draft);
      }}
    >
      <p className="text-xs text-[var(--muted)]">
        Los datos reutilizables se gestionan en{" "}
        <Link href="/career/catalogs" className="text-[var(--accent)] underline">
          Carrera → Datos reutilizables
        </Link>
        .
      </p>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Empresa
        <select
          value={draft.companyId}
          onChange={(e) =>
            setDraft((c) => ({
              ...c,
              companyId: e.target.value ? Number(e.target.value) : "",
            }))
          }
          className={inputClass}
          autoFocus
        >
          <option value="">Selecciona…</option>
          {companies.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Cargo
        <select
          value={draft.positionId}
          onChange={(e) =>
            setDraft((c) => ({
              ...c,
              positionId: e.target.value ? Number(e.target.value) : "",
            }))
          }
          className={inputClass}
        >
          <option value="">Selecciona…</option>
          {positions.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Ubicación
          <select
            value={draft.locationId}
            onChange={(e) =>
              setDraft((c) => ({
                ...c,
                locationId: e.target.value ? Number(e.target.value) : "",
              }))
            }
            className={inputClass}
          >
            <option value="">Ninguna</option>
            {locations.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Carrera
          <select
            value={draft.fieldId}
            onChange={(e) =>
              setDraft((c) => ({
                ...c,
                fieldId: e.target.value ? Number(e.target.value) : "",
              }))
            }
            className={inputClass}
          >
            <option value="">Ninguna</option>
            {fields.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Desde
          <input
            type="date"
            value={draft.startDate}
            onChange={(e) => setDraft((c) => ({ ...c, startDate: e.target.value }))}
            className={inputClass}
          />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Hasta
          <input
            type="date"
            value={draft.endDate}
            disabled={draft.isCurrent}
            onChange={(e) => setDraft((c) => ({ ...c, endDate: e.target.value }))}
            className={inputClass}
          />
        </label>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={draft.isCurrent}
          onChange={(e) =>
            setDraft((c) => ({
              ...c,
              isCurrent: e.target.checked,
              endDate: e.target.checked ? "" : c.endDate,
            }))
          }
        />
        Trabajo actual
      </label>
      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-medium">Tecnologías</legend>
        <div className="flex flex-wrap gap-2">
          {technologies.map((tech) => {
            const selected = draft.technologyIds.includes(tech.id);
            return (
              <button
                key={tech.id}
                type="button"
                onClick={() => toggleTech(tech.id)}
                className={
                  selected
                    ? "rounded-md bg-[var(--accent)] px-2.5 py-1 text-xs font-medium text-white"
                    : "rounded-md border border-[var(--border)] px-2.5 py-1 text-xs text-[var(--muted)]"
                }
              >
                {tech.name}
              </button>
            );
          })}
          {!technologies.length ? (
            <span className="text-xs text-[var(--muted)]">
              Crea tecnologías en Datos reutilizables.
            </span>
          ) : null}
        </div>
      </fieldset>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Resumen
        <textarea
          value={draft.summary}
          onChange={(e) => setDraft((c) => ({ ...c, summary: e.target.value }))}
          rows={3}
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Logros
        <textarea
          value={draft.achievements}
          onChange={(e) => setDraft((c) => ({ ...c, achievements: e.target.value }))}
          rows={4}
          className={inputClass}
        />
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

function toDraft(item: WorkExperience): ExperienceDraft {
  return {
    companyId: item.companyId,
    positionId: item.positionId,
    locationId: item.locationId ?? "",
    fieldId: item.fieldId ?? "",
    startDate: item.startDate.slice(0, 10),
    endDate: item.endDate?.slice(0, 10) ?? "",
    isCurrent: item.isCurrent,
    summary: item.summary ?? "",
    achievements: item.achievements ?? "",
    technologyIds: item.technologyIds ?? [],
  };
}

export function ExperiencesView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<WorkExperience | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const query = useWorkExperiences();
  const mutations = useWorkExperienceMutations();
  const confirm = useConfirm();
  const items = query.data ?? [];

  useEffect(() => {
    if (!items.length) {
      setSelectedId(null);
      return;
    }
    setSelectedId((current) =>
      current != null && items.some((item) => item.id === current)
        ? current
        : items[0].id,
    );
  }, [items]);

  const selected = useMemo(
    () => items.find((item) => item.id === selectedId) ?? items[0],
    [items, selectedId],
  );

  const save = async (draft: ExperienceDraft, id?: number) => {
    if (draft.companyId === "" || draft.positionId === "") return;
    const body = {
      companyId: Number(draft.companyId),
      positionId: Number(draft.positionId),
      locationId: draft.locationId === "" ? null : draft.locationId,
      fieldId: draft.fieldId === "" ? null : draft.fieldId,
      startDate: draft.startDate,
      endDate: draft.endDate,
      isCurrent: draft.isCurrent,
      summary: draft.summary.trim(),
      achievements: draft.achievements.trim(),
      technologyIds: draft.technologyIds,
    };
    try {
      if (id != null) {
        await mutations.update.mutateAsync({ id, body });
        setEditing(null);
        toast.success("Experiencia actualizada");
      } else {
        await mutations.create.mutateAsync(body);
        setCreating(false);
        toast.success("Experiencia guardada");
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  };

  const remove = async (item: WorkExperience) => {
    const ok = await confirm({
      title: "Eliminar experiencia",
      description: `¿Estás seguro de eliminar “${item.positionName} @ ${item.companyName}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Experiencia eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Experiencias armadas con catálogos reutilizables (empresa, cargo, ubicación, carrera, tecnologías).
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nueva experiencia"
        >
          <ExperienceForm
            onClose={() => setCreating(false)}
            onSubmit={(draft) => save(draft)}
            submitLabel="Guardar"
            pending={mutations.create.isPending}
          />
        </CreatePanel>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>}
      {!query.isLoading && !query.error && (
        items.length === 0 ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Aún no has guardado experiencias. Primero crea datos en{" "}
            <Link href="/career/catalogs" className="text-[var(--accent)] underline">
              Datos reutilizables
            </Link>
            .
          </p>
        ) : (
          <section className="grid gap-3 lg:grid-cols-[220px_1fr]">
            <div className="rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 shadow-[var(--shadow-card)]">
              <p className="mb-2 px-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                Experiencias
              </p>
              <div className="flex flex-col gap-1">
                {items.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => setSelectedId(item.id)}
                    className={cn(
                      "rounded-lg px-3 py-2 text-left text-sm transition-colors",
                      item.id === selected?.id
                        ? "bg-[var(--accent-soft)] font-medium text-[var(--accent-strong)]"
                        : "text-[var(--ink)] hover:bg-[var(--surface-muted)]",
                    )}
                  >
                    {item.positionName}
                    <span className="mt-0.5 block text-[11px] font-normal text-[var(--muted)]">
                      {[item.companyName, item.isCurrent ? "Actual" : item.endDate?.slice(0, 10)]
                        .filter(Boolean)
                        .join(" · ")}
                    </span>
                  </button>
                ))}
              </div>
            </div>

            <article className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-4 shadow-[var(--shadow-card)]">
              <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
                <div>
                  <h3 className="font-semibold text-[var(--ink)]">
                    {selected?.positionName} · {selected?.companyName}
                  </h3>
                  <p className="text-xs text-[var(--muted)]">
                    {[
                      selected
                        ? `${selected.startDate.slice(0, 10)} — ${
                            selected.isCurrent
                              ? "Actual"
                              : selected.endDate?.slice(0, 10) ?? "—"
                          }`
                        : null,
                      selected?.locationName,
                      selected?.fieldName,
                    ]
                      .filter(Boolean)
                      .join(" · ")}
                  </p>
                </div>
                {selected ? (
                  <div className="flex gap-1">
                    <button
                      type="button"
                      onClick={() => setEditing(selected)}
                      className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]"
                    >
                      <Pencil className="size-3.5" /> Editar
                    </button>
                    <button
                      type="button"
                      onClick={() => remove(selected)}
                      className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
                    >
                      <Trash2 className="size-3.5" /> Eliminar
                    </button>
                  </div>
                ) : null}
              </div>

              {selected?.technologyNames?.length ? (
                <div className="mb-3 flex flex-wrap gap-1.5">
                  {selected.technologyNames.map((tech) => (
                    <span
                      key={tech}
                      className="rounded-md bg-[var(--accent-soft)] px-2 py-0.5 text-[11px] font-medium text-[var(--accent-strong)]"
                    >
                      {tech}
                    </span>
                  ))}
                </div>
              ) : null}

              <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                Resumen
              </p>
              <pre className="max-h-[16rem] overflow-auto whitespace-pre-wrap rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] px-4 py-3 text-sm leading-relaxed text-[var(--ink)]">
                {selected?.summary || "Sin resumen."}
              </pre>

              <p className="mb-1 mt-3 text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                Logros
              </p>
              <pre className="max-h-[16rem] overflow-auto whitespace-pre-wrap rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] px-4 py-3 text-sm leading-relaxed text-[var(--ink)]">
                {selected?.achievements || "Sin logros."}
              </pre>
            </article>
          </section>
        )
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => !open && setEditing(null)}
        title="Editar experiencia"
        size="sm"
      >
        {editing ? (
          <ExperienceForm
            initial={toDraft(editing)}
            onClose={() => setEditing(null)}
            onSubmit={(draft) => save(draft, editing.id)}
            submitLabel="Actualizar"
            pending={mutations.update.isPending}
          />
        ) : null}
      </FormModal>
    </div>
  );
}
