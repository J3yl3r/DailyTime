"use client";

import { useEffect, useRef } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { VoiceFormFieldPatch } from "@/lib/voice/form-commands";
import { useCareerCatalog } from "@/hooks/queries/use-career-catalog";
import { useCareerCatalogMutations } from "@/hooks/mutations/use-career-catalog-mutations";
import { useWorkExperienceMutations } from "@/hooks/mutations/use-work-experience-mutations";
import { useJobApplicationMutations } from "@/hooks/mutations/use-job-application-mutations";
import type { CareerCatalog } from "@/types/api";

export type CareerVoiceKind = "work_experience" | "job_application";

export type CareerVoiceValues = {
  companyName: string;
  positionName: string;
  locationName: string;
  fieldName: string;
  statusName: string;
  startDate: string;
  endDate: string;
  appliedAt: string;
  isCurrent: boolean;
  summary: string;
  achievements: string;
  technologies: string;
  url: string;
  contact: string;
  notes: string;
  companyId?: number | null;
  positionId?: number | null;
  locationId?: number | null;
  fieldId?: number | null;
  statusId?: number | null;
  technologyIds?: number[];
  workExperienceId?: number | null;
};

type Props = {
  kind: CareerVoiceKind;
  initialValues?: Partial<CareerVoiceValues>;
  editingId?: number | null;
  voicePatch?: VoiceFormFieldPatch | null;
  submitRequestId?: number;
  onClose: () => void;
};

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 text-sm font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

function findByName(items: CareerCatalog[], name: string): CareerCatalog | null {
  const needle = name.trim().toLowerCase();
  if (!needle) return null;
  return (
    items.find((item) => item.name.toLowerCase() === needle) ??
    items.find((item) => item.name.toLowerCase().includes(needle)) ??
    null
  );
}

async function ensureNamed(
  name: string,
  items: CareerCatalog[],
  create: (body: {
    name: string;
    description: string;
    color: string;
    sortOrder: number;
    isActive: boolean;
  }) => Promise<CareerCatalog>,
): Promise<number | null> {
  const trimmed = name.trim();
  if (!trimmed) return null;
  const existing = findByName(items, trimmed);
  if (existing) return existing.id;
  const created = await create({
    name: trimmed,
    description: "",
    color: "#64748B",
    sortOrder: 0,
    isActive: true,
  });
  return created.id;
}

export function VoiceCareerForm({
  kind,
  initialValues,
  editingId = null,
  voicePatch = null,
  submitRequestId = 0,
  onClose,
}: Props) {
  const companies = useCareerCatalog("companies", true).data ?? [];
  const positions = useCareerCatalog("positions", true).data ?? [];
  const locations = useCareerCatalog("locations", true).data ?? [];
  const fields = useCareerCatalog("fields", true).data ?? [];
  const technologies = useCareerCatalog("technologies", true).data ?? [];
  const statuses = useCareerCatalog("application-statuses", true).data ?? [];

  const companyMutations = useCareerCatalogMutations("companies");
  const positionMutations = useCareerCatalogMutations("positions");
  const locationMutations = useCareerCatalogMutations("locations");
  const fieldMutations = useCareerCatalogMutations("fields");
  const technologyMutations = useCareerCatalogMutations("technologies");
  const statusMutations = useCareerCatalogMutations("application-statuses");
  const experienceMutations = useWorkExperienceMutations();
  const applicationMutations = useJobApplicationMutations();

  const form = useForm<CareerVoiceValues>({
    defaultValues: {
      companyName: initialValues?.companyName ?? "",
      positionName: initialValues?.positionName ?? "",
      locationName: initialValues?.locationName ?? "",
      fieldName: initialValues?.fieldName ?? "",
      statusName: initialValues?.statusName ?? "",
      startDate: initialValues?.startDate?.slice(0, 10) ?? "",
      endDate: initialValues?.endDate?.slice(0, 10) ?? "",
      appliedAt: initialValues?.appliedAt?.slice(0, 10) ?? "",
      isCurrent: initialValues?.isCurrent ?? false,
      summary: initialValues?.summary ?? "",
      achievements: initialValues?.achievements ?? "",
      technologies: initialValues?.technologies ?? "",
      url: initialValues?.url ?? "",
      contact: initialValues?.contact ?? "",
      notes: initialValues?.notes ?? "",
      companyId: initialValues?.companyId ?? null,
      positionId: initialValues?.positionId ?? null,
      locationId: initialValues?.locationId ?? null,
      fieldId: initialValues?.fieldId ?? null,
      statusId: initialValues?.statusId ?? null,
      technologyIds: initialValues?.technologyIds ?? [],
      workExperienceId: initialValues?.workExperienceId ?? null,
    },
  });

  useEffect(() => {
    if (!voicePatch) return;
    form.setValue(voicePatch.field as keyof CareerVoiceValues, voicePatch.value as never, {
      shouldDirty: true,
      shouldValidate: true,
      shouldTouch: true,
    });
  }, [form, voicePatch]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const companyId =
        values.companyId ??
        (await ensureNamed(values.companyName, companies, (body) =>
          companyMutations.create.mutateAsync(body),
        ));
      const positionId =
        values.positionId ??
        (await ensureNamed(values.positionName, positions, (body) =>
          positionMutations.create.mutateAsync(body),
        ));

      if (!companyId || !positionId) {
        toast.error("Empresa y cargo son obligatorios");
        return;
      }

      const locationId =
        values.locationId ??
        (await ensureNamed(values.locationName, locations, (body) =>
          locationMutations.create.mutateAsync(body),
        ));
      const fieldId =
        values.fieldId ??
        (await ensureNamed(values.fieldName, fields, (body) =>
          fieldMutations.create.mutateAsync(body),
        ));

      if (kind === "work_experience") {
        if (!values.startDate.trim()) {
          toast.error("La fecha de inicio es obligatoria");
          return;
        }

        let technologyIds = values.technologyIds ?? [];
        if (values.technologies.trim()) {
          const names = values.technologies
            .split(/[,;]/)
            .map((part) => part.trim())
            .filter(Boolean);
          const ids: number[] = [];
          for (const name of names) {
            const id = await ensureNamed(name, technologies, (body) =>
              technologyMutations.create.mutateAsync(body),
            );
            if (id != null) ids.push(id);
          }
          technologyIds = ids;
        }

        const body = {
          companyId,
          positionId,
          locationId,
          fieldId,
          startDate: values.startDate,
          endDate: values.isCurrent ? "" : values.endDate,
          isCurrent: values.isCurrent,
          summary: values.summary.trim(),
          achievements: values.achievements.trim(),
          technologyIds,
        };

        if (editingId != null) {
          await experienceMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Experiencia actualizada");
        } else {
          await experienceMutations.create.mutateAsync(body);
          toast.success("Experiencia creada");
        }
      } else {
        const statusId =
          values.statusId ??
          (await ensureNamed(values.statusName || "Enviada", statuses, (body) =>
            statusMutations.create.mutateAsync(body),
          ));
        if (!statusId) {
          toast.error("El estado de postulación es obligatorio");
          return;
        }
        if (!values.appliedAt.trim()) {
          toast.error("La fecha de postulación es obligatoria");
          return;
        }

        const body = {
          companyId,
          positionId,
          locationId,
          fieldId,
          statusId,
          appliedAt: values.appliedAt,
          url: values.url.trim(),
          contact: values.contact.trim(),
          notes: values.notes.trim(),
          workExperienceId: values.workExperienceId ?? null,
        };

        if (editingId != null) {
          await applicationMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Postulación actualizada");
        } else {
          await applicationMutations.create.mutateAsync(body);
          toast.success("Postulación creada");
        }
      }

      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  });

  const submitRef = useRef(onSubmit);
  submitRef.current = onSubmit;

  useEffect(() => {
    if (!submitRequestId) return;
    void submitRef.current();
  }, [submitRequestId]);

  const pending =
    companyMutations.create.isPending ||
    positionMutations.create.isPending ||
    locationMutations.create.isPending ||
    fieldMutations.create.isPending ||
    technologyMutations.create.isPending ||
    statusMutations.create.isPending ||
    experienceMutations.create.isPending ||
    experienceMutations.update.isPending ||
    applicationMutations.create.isPending ||
    applicationMutations.update.isPending;

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3">
      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Empresa
        <Controller
          name="companyName"
          control={form.control}
          render={({ field }) => (
            <input
              {...field}
              value={field.value ?? ""}
              placeholder="Google"
              className={inputClass}
              autoFocus
            />
          )}
        />
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Cargo
        <Controller
          name="positionName"
          control={form.control}
          render={({ field }) => (
            <input
              {...field}
              value={field.value ?? ""}
              placeholder="Desarrollador"
              className={inputClass}
            />
          )}
        />
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Ubicación
        <Controller
          name="locationName"
          control={form.control}
          render={({ field }) => (
            <input {...field} value={field.value ?? ""} className={inputClass} />
          )}
        />
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Carrera / área
        <Controller
          name="fieldName"
          control={form.control}
          render={({ field }) => (
            <input {...field} value={field.value ?? ""} className={inputClass} />
          )}
        />
      </label>

      {kind === "work_experience" ? (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Inicio
            <Controller
              name="startDate"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  type="date"
                  value={field.value ?? ""}
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Fin
            <Controller
              name="endDate"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  type="date"
                  value={field.value ?? ""}
                  className={inputClass}
                  disabled={form.watch("isCurrent")}
                />
              )}
            />
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              {...form.register("isCurrent")}
              className="size-4 accent-[var(--accent)]"
            />
            Trabajo actual
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Resumen
            <Controller
              name="summary"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  rows={2}
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Logros
            <Controller
              name="achievements"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  rows={2}
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Tecnologías
            <Controller
              name="technologies"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  value={field.value ?? ""}
                  placeholder="React, TypeScript"
                  className={inputClass}
                />
              )}
            />
          </label>
        </>
      ) : (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Estado
            <Controller
              name="statusName"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  value={field.value ?? ""}
                  placeholder="Enviada"
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Fecha
            <Controller
              name="appliedAt"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  type="date"
                  value={field.value ?? ""}
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            URL
            <Controller
              name="url"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Contacto
            <Controller
              name="contact"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Notas
            <Controller
              name="notes"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  rows={2}
                  className={inputClass}
                />
              )}
            />
          </label>
        </>
      )}

      <div className="flex gap-2 border-t border-[var(--border)] pt-3">
        <button
          type="submit"
          disabled={pending}
          className="rounded-md bg-[var(--accent)] px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
        >
          {pending ? "Guardando…" : "Guardar"}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-3 py-1.5 text-sm text-[var(--muted)]"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}
