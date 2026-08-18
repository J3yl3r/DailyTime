"use client";

import { useState } from "react";
import { ExternalLink, Gauge, Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { JobApplication } from "@/types/api";
import { evaluateFit, type FitScoreResult } from "@/lib/api/career";
import { useJobApplications } from "@/hooks/queries/use-job-applications";
import { useWorkExperiences } from "@/hooks/queries/use-work-experiences";
import { useCareerCatalog } from "@/hooks/queries/use-career-catalog";
import { useJobApplicationMutations } from "@/hooks/mutations/use-job-application-mutations";
import { CreatePanel } from "@/components/ui/CreatePanel";
import {
  DataList,
  DataListAction,
  DataListBadge,
  DataListLink,
} from "@/components/ui/DataList";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";
import Link from "next/link";
import { cn } from "@/lib/utils/cn";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";


type ApplicationDraft = {
  companyId: number | "";
  positionId: number | "";
  locationId: number | "";
  fieldId: number | "";
  statusId: number | "";
  appliedAt: string;
  url: string;
  contact: string;
  notes: string;
  workExperienceId: number | "";
};

const EMPTY: ApplicationDraft = {
  companyId: "",
  positionId: "",
  locationId: "",
  fieldId: "",
  statusId: "",
  appliedAt: new Date().toISOString().slice(0, 10),
  url: "",
  contact: "",
  notes: "",
  workExperienceId: "",
};

function ApplicationForm({
  initial,
  onSubmit,
  onClose,
  submitLabel,
  pending,
}: {
  initial?: ApplicationDraft;
  onSubmit: (draft: ApplicationDraft) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
}) {
  const companies = useCareerCatalog("companies", true).data ?? [];
  const positions = useCareerCatalog("positions", true).data ?? [];
  const locations = useCareerCatalog("locations", true).data ?? [];
  const fields = useCareerCatalog("fields", true).data ?? [];
  const statuses = useCareerCatalog("application-statuses", true).data ?? [];
  const experiences = useWorkExperiences().data ?? [];
  const [draft, setDraft] = useState<ApplicationDraft>(() => {
    if (initial) return initial;
    const firstStatus = statuses[0]?.id;
    return {
      ...EMPTY,
      statusId: firstStatus ?? "",
    };
  });

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!draft.companyId || !draft.positionId || !draft.statusId || !draft.appliedAt) {
          toast.error("Empresa, cargo, estado y fecha son obligatorios");
          return;
        }
        onSubmit(draft);
      }}
    >
      <p className="text-xs text-[var(--muted)]">
        Empresa/cargo/ubicación/carrera vienen de{" "}
        <Link href="/career/catalogs" className="text-[var(--accent)] underline">
          Datos reutilizables
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
          Fecha
          <input
            type="date"
            value={draft.appliedAt}
            onChange={(e) => setDraft((c) => ({ ...c, appliedAt: e.target.value }))}
            className={inputClass}
          />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Estado
          <select
            value={draft.statusId}
            onChange={(e) =>
              setDraft((c) => ({
                ...c,
                statusId: e.target.value ? Number(e.target.value) : "",
              }))
            }
            className={inputClass}
          >
            <option value="">Selecciona…</option>
            {statuses.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Experiencia relacionada (opcional)
        <select
          value={draft.workExperienceId}
          onChange={(e) =>
            setDraft((c) => ({
              ...c,
              workExperienceId: e.target.value ? Number(e.target.value) : "",
            }))
          }
          className={inputClass}
        >
          <option value="">Ninguna</option>
          {experiences.map((exp) => (
            <option key={exp.id} value={exp.id}>
              {exp.positionName} @ {exp.companyName}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        URL
        <input
          value={draft.url}
          onChange={(e) => setDraft((c) => ({ ...c, url: e.target.value }))}
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Contacto
        <input
          value={draft.contact}
          onChange={(e) => setDraft((c) => ({ ...c, contact: e.target.value }))}
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Notas
        <textarea
          value={draft.notes}
          onChange={(e) => setDraft((c) => ({ ...c, notes: e.target.value }))}
          rows={3}
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

function toDraft(item: JobApplication): ApplicationDraft {
  return {
    companyId: item.companyId,
    positionId: item.positionId,
    locationId: item.locationId ?? "",
    fieldId: item.fieldId ?? "",
    statusId: item.statusId,
    appliedAt: item.appliedAt.slice(0, 10),
    url: item.url ?? "",
    contact: item.contact ?? "",
    notes: item.notes ?? "",
    workExperienceId: item.workExperienceId ?? "",
  };
}

export function ApplicationsView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<JobApplication | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [fitResult, setFitResult] = useState<FitScoreResult | null>(null);
  const [fitFor, setFitFor] = useState<JobApplication | null>(null);
  const [evaluatingId, setEvaluatingId] = useState<number | null>(null);
  const query = useJobApplications();
  const mutations = useJobApplicationMutations();
  const confirm = useConfirm();
  const items = query.data ?? [];

  const allVisibleSelected =
    items.length > 0 && items.every((item) => selectedIds.has(item.id));
  const someSelected = selectedIds.size > 0;

  const evaluateEncaje = async (item: JobApplication) => {
    setEvaluatingId(item.id);
    try {
      const result = await evaluateFit({
        jobApplicationId: item.id,
        offerUrl: item.url,
        // No enviamos workExperienceId: se usa el perfil completo (todas las experiencias).
      });
      setFitFor(item);
      setFitResult(result);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudo evaluar el encaje");
    } finally {
      setEvaluatingId(null);
    }
  };

  const toggleSelect = (id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (allVisibleSelected) {
      setSelectedIds(new Set());
      return;
    }
    setSelectedIds(new Set(items.map((item) => item.id)));
  };

  const clearSelection = () => setSelectedIds(new Set());

  const save = async (draft: ApplicationDraft, id?: number) => {
    if (draft.companyId === "" || draft.positionId === "" || draft.statusId === "") return;
    const body = {
      companyId: Number(draft.companyId),
      positionId: Number(draft.positionId),
      locationId: draft.locationId === "" ? null : draft.locationId,
      fieldId: draft.fieldId === "" ? null : draft.fieldId,
      statusId: Number(draft.statusId),
      appliedAt: draft.appliedAt,
      url: draft.url.trim(),
      contact: draft.contact.trim(),
      notes: draft.notes.trim(),
      workExperienceId: draft.workExperienceId === "" ? null : draft.workExperienceId,
    };
    try {
      if (id != null) {
        await mutations.update.mutateAsync({ id, body });
        setEditing(null);
        toast.success("Postulación actualizada");
      } else {
        await mutations.create.mutateAsync(body);
        setCreating(false);
        toast.success("Postulación guardada");
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  };

  const remove = async (item: JobApplication) => {
    const ok = await confirm({
      title: "Eliminar postulación",
      description: `¿Estás seguro de eliminar la postulación a “${item.companyName}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => {
        toast.success("Postulación eliminada");
        setSelectedIds((prev) => {
          const next = new Set(prev);
          next.delete(item.id);
          return next;
        });
      },
      onError: (error) => toast.error(error.message),
    });
  };

  const bulkRemove = async () => {
    const ids = Array.from(selectedIds);
    const ok = await confirm({
      title: "Eliminar postulaciones",
      description: `¿Estás seguro de eliminar ${ids.length} postulación(es)? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.bulkRemove.mutate(ids, {
      onSuccess: (result) => {
        toast.success(`${result.affected} postulación(es) eliminada(s)`);
        clearSelection();
      },
      onError: (error) => toast.error(error.message),
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Postulaciones con evaluación de encaje usando tu perfil completo (todas las
          experiencias) y un borrador de CV orientado a la vacante.
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nueva postulación"
        >
          <ApplicationForm
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
        <DataList
          items={items}
          getKey={(item) => item.id}
          emptyMessage="Aún no hay postulaciones."
          selected={(item) => selectedIds.has(item.id)}
          onToggleSelect={(item) => toggleSelect(item.id)}
          selectAriaLabel={(item) => `Seleccionar ${item.positionName}`}
          header={
            items.length > 0 ? (
              <label className="inline-flex items-center gap-2 text-sm text-[var(--muted)]">
                <input
                  type="checkbox"
                  checked={allVisibleSelected}
                  onChange={toggleSelectAll}
                />
                Seleccionar todas
              </label>
            ) : null
          }
          selectionBar={
            someSelected ? (
              <div className="sticky top-2 z-10 flex flex-wrap items-center gap-2 rounded-lg border border-[var(--accent)] bg-[var(--accent-soft)] px-4 py-2.5">
                <span className="text-sm font-medium">
                  {selectedIds.size} seleccionada(s)
                </span>
                <button
                  type="button"
                  onClick={bulkRemove}
                  disabled={mutations.bulkRemove.isPending}
                  className="rounded-md bg-white px-3 py-1.5 text-xs text-[var(--danger)] hover:bg-red-50"
                >
                  Eliminar
                </button>
                <button
                  type="button"
                  onClick={clearSelection}
                  className="rounded-md bg-white px-3 py-1.5 text-xs hover:bg-[var(--surface-muted)]"
                >
                  Limpiar selección
                </button>
              </div>
            ) : null
          }
          title={(item) => (
            <>
              {item.positionName} · {item.companyName}
            </>
          )}
          badge={(item) => (
            <DataListBadge color={item.statusColor}>{item.statusName}</DataListBadge>
          )}
          meta={(item) =>
            [
              item.appliedAt.slice(0, 10),
              item.locationName,
              item.fieldName,
              item.workExperienceLabel
                ? `CV: ${item.workExperienceLabel}`
                : null,
            ]
              .filter(Boolean)
              .join(" · ")
          }
          description={(item) => (item.notes ? item.notes : null)}
          actions={(item) => (
            <>
              {item.url ? (
                <DataListLink href={item.url}>
                  <ExternalLink className="size-3.5" /> Abrir
                </DataListLink>
              ) : null}
              <DataListAction
                onClick={() => void evaluateEncaje(item)}
                disabled={evaluatingId === item.id}
              >
                <Gauge className="size-3.5" />
                {evaluatingId === item.id ? "Evaluando…" : "Evaluar encaje"}
              </DataListAction>
              <DataListAction onClick={() => setEditing(item)}>
                <Pencil className="size-3.5" /> Editar
              </DataListAction>
              <DataListAction danger onClick={() => remove(item)}>
                <Trash2 className="size-3.5" /> Eliminar
              </DataListAction>
            </>
          )}
        />
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => !open && setEditing(null)}
        title="Editar postulación"
        size="sm"
      >
        {editing ? (
          <ApplicationForm
            initial={toDraft(editing)}
            onClose={() => setEditing(null)}
            onSubmit={(draft) => save(draft, editing.id)}
            submitLabel="Actualizar"
            pending={mutations.update.isPending}
          />
        ) : null}
      </FormModal>

      <FormModal
        open={fitResult != null}
        onOpenChange={(open) => {
          if (!open) {
            setFitResult(null);
            setFitFor(null);
          }
        }}
        title={
          fitFor
            ? `Encaje · ${fitFor.positionName} · ${fitFor.companyName}`
            : "Encaje CV ↔ oferta"
        }
        description="Compara tu historial completo contra la oferta capturada y sugiere cómo enfocar el CV."
        size="md"
      >
        {fitResult ? (
          <div className="flex flex-col gap-4">
            <div
              className={cn(
                "rounded-lg border px-3 py-2",
                fitResult.verdict === "yes" && "border-emerald-300 bg-emerald-50",
                fitResult.verdict === "maybe" && "border-amber-300 bg-amber-50",
                fitResult.verdict === "no" && "border-red-300 bg-red-50",
                !["yes", "maybe", "no"].includes(fitResult.verdict) &&
                  "border-[var(--border)] bg-[var(--surface-muted)]",
              )}
            >
              <p className="text-2xl font-semibold tabular-nums text-[var(--ink)]">
                {fitResult.score}
                <span className="text-sm font-normal text-[var(--muted)]"> / 100</span>
              </p>
              <p className="text-sm font-medium text-[var(--ink)]">{fitResult.verdictLabel}</p>
              <p className="mt-1 text-xs text-[var(--muted)]">
                {fitResult.workExperienceLabel ??
                  `Perfil completo (${fitResult.experiencesUsed || 0} experiencia(s))`}
              </p>
              {fitResult.offerFound && fitResult.jobOfferTitle ? (
                <p className="text-xs text-[var(--muted)]">
                  Oferta: {fitResult.jobOfferTitle}
                </p>
              ) : (
                <p className="text-xs text-[var(--muted)]">
                  No se encontró oferta scrapada con esa URL.
                </p>
              )}
            </div>

            {fitResult.profileExperienceLabels?.length ? (
              <div>
                <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                  Experiencias usadas
                </p>
                <ul className="flex list-disc flex-col gap-0.5 pl-5 text-sm text-[var(--ink)]">
                  {fitResult.profileExperienceLabels.map((label) => (
                    <li key={label}>{label}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            <div>
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                Hallazgos
              </p>
              <ul className="flex list-disc flex-col gap-1 pl-5 text-sm text-[var(--ink)]">
                {fitResult.reasons.map((reason) => (
                  <li key={reason}>{reason}</li>
                ))}
              </ul>
            </div>

            {fitResult.recommendations?.length ? (
              <div>
                <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                  Recomendaciones
                </p>
                <ul className="flex list-disc flex-col gap-1 pl-5 text-sm text-[var(--ink)]">
                  {fitResult.recommendations.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {fitResult.suggestedCv ? (
              <div>
                <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                  Borrador de CV para esta vacante
                </p>
                <pre className="max-h-72 overflow-auto whitespace-pre-wrap rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] px-3 py-2 text-xs leading-relaxed text-[var(--ink)]">
                  {fitResult.suggestedCv}
                </pre>
              </div>
            ) : null}
          </div>
        ) : null}
      </FormModal>
    </div>
  );
}
