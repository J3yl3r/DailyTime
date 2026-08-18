"use client";

import { useEffect, useRef } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import {
  createNoteSchema,
  type CreateNoteFormValues,
} from "@/schemas/note.schema";
import { useNoteMutations } from "@/hooks/mutations/use-note-mutations";
import { useStatuses } from "@/hooks/queries/use-statuses";
import { useCategories } from "@/hooks/queries/use-categories";
import { usePeople } from "@/hooks/queries/use-people";
import { useProjects } from "@/hooks/queries/use-projects";
import { useCompanies } from "@/hooks/queries/use-companies";
import type { VoiceFormFieldPatch } from "@/lib/voice/form-commands";

type Props = {
  workDate: string;
  initialStartTime?: string | null;
  initialEndTime?: string | null;
  initialValues?: Partial<CreateNoteFormValues>;
  voicePatch?: VoiceFormFieldPatch | null;
  submitRequestId?: number;
  parentNoteId?: number | null;
  parentLabel?: string;
  editingId?: number | null;
  onClose: () => void;
};

export function NoteForm({
  workDate,
  initialStartTime = null,
  initialEndTime = null,
  initialValues,
  voicePatch = null,
  submitRequestId = 0,
  parentNoteId = null,
  parentLabel,
  editingId = null,
  onClose,
}: Props) {
  const { create, update } = useNoteMutations(workDate);
  const statuses = useStatuses("note");
  const categories = useCategories("note");
  const people = usePeople(true);
  const projects = useProjects(true);
  const companies = useCompanies(true);
  const defaultStatusId = statuses.data?.[0]?.id ?? null;
  const defaultCategoryId = categories.data?.find((item) => item.isActive)?.id ?? null;

  const form = useForm<CreateNoteFormValues>({
    resolver: zodResolver(createNoteSchema),
    defaultValues: {
      title: initialValues?.title ?? "",
      content: initialValues?.content ?? "",
      workDate: initialValues?.workDate ?? workDate,
      startTime: initialValues?.startTime ?? initialStartTime,
      endTime: initialValues?.endTime ?? initialEndTime,
      parentNoteId,
      statusId: initialValues?.statusId ?? defaultStatusId,
      categoryId: initialValues?.categoryId ?? defaultCategoryId,
      personId: initialValues?.personId ?? null,
      projectId: initialValues?.projectId ?? null,
      companyId: initialValues?.companyId ?? null,
      sortOrder: initialValues?.sortOrder ?? 0,
      durationMinutes: initialValues?.durationMinutes ?? 0,
    },
  });

  useEffect(() => {
    if (defaultStatusId != null && form.getValues("statusId") == null) {
      form.setValue("statusId", defaultStatusId);
    }
  }, [defaultStatusId, form]);

  useEffect(() => {
    if (defaultCategoryId != null && form.getValues("categoryId") == null) {
      form.setValue("categoryId", defaultCategoryId);
    }
  }, [defaultCategoryId, form]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const payload = createNoteSchema.parse({
        ...values,
        workDate: values.workDate ?? workDate,
        parentNoteId: parentNoteId ?? null,
        statusId: values.statusId ?? defaultStatusId,
        categoryId: values.categoryId ?? defaultCategoryId,
        title: values.title?.trim() ? values.title.trim() : null,
      });

      if (editingId != null) {
        await update.mutateAsync({
          id: editingId,
          body: {
            title: payload.title ?? null,
            content: payload.content,
            workDate: payload.workDate ?? workDate,
            startTime: payload.startTime ?? null,
            endTime: payload.endTime ?? null,
            parentNoteId: payload.parentNoteId ?? null,
            statusId: payload.statusId!,
            categoryId: payload.categoryId!,
            personId: payload.personId ?? null,
            projectId: payload.projectId ?? null,
            companyId: payload.companyId ?? null,
            sortOrder: payload.sortOrder ?? 0,
            durationMinutes: payload.durationMinutes ?? 0,
          },
        });
        toast.success("Nota actualizada");
      } else {
        await create.mutateAsync(payload);
        toast.success("Nota creada");
      }
      onClose();
    } catch (error) {
      form.setError("content", {
        message:
          error instanceof Error
            ? error.message
            : editingId != null
              ? "Error al actualizar la nota"
              : "Error al crear la nota",
      });
    }
  });

  const submitRef = useRef(onSubmit);
  submitRef.current = onSubmit;

  useEffect(() => {
    if (!voicePatch) return;
    form.setValue(voicePatch.field as keyof CreateNoteFormValues, voicePatch.value as never, {
      shouldDirty: true,
      shouldValidate: true,
      shouldTouch: true,
    });
  }, [form, voicePatch]);

  useEffect(() => {
    if (!submitRequestId) return;
    void submitRef.current();
  }, [submitRequestId]);

  const fieldClass =
    "rounded-md border border-[var(--border)] bg-white px-3 py-2 text-sm font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";
  const labelClass = "flex flex-col gap-1 text-sm font-medium text-[var(--ink)]";

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <input type="hidden" {...form.register("parentNoteId")} />
      <input type="hidden" {...form.register("sortOrder", { valueAsNumber: true })} />
      <input type="hidden" {...form.register("durationMinutes", { valueAsNumber: true })} />

      {parentNoteId != null && (
        <p className="rounded-lg bg-amber-100 px-3 py-2 text-sm text-[var(--note)]">
          Subnota de{" "}
          <span className="font-medium">{parentLabel ?? `#${parentNoteId}`}</span>
        </p>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="flex flex-col gap-3">
          <label className={labelClass}>
            Título
            <Controller
              name="title"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  value={field.value ?? ""}
                  placeholder="Opcional"
                  className={fieldClass}
                />
              )}
            />
          </label>

          <label className={labelClass}>
            Contenido
            <Controller
              name="content"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  placeholder="Escribe o dicta el contenido de la nota"
                  rows={4}
                  className={`${fieldClass} min-h-[6.5rem] resize-y`}
                  autoFocus
                />
              )}
            />
          </label>
          {form.formState.errors.content && (
            <p className="text-sm text-[var(--danger)]">{form.formState.errors.content.message}</p>
          )}
        </div>

        <div className="flex flex-col gap-3">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <label className={labelClass}>
              Fecha
              <input type="date" {...form.register("workDate")} className={fieldClass} />
            </label>
            <label className={labelClass}>
              Inicio
              <input
                type="time"
                {...form.register("startTime", {
                  setValueAs: (value) => value || null,
                })}
                className={fieldClass}
              />
            </label>
            <label className={labelClass}>
              Fin
              <input
                type="time"
                {...form.register("endTime", {
                  setValueAs: (value) => value || null,
                })}
                className={fieldClass}
              />
            </label>
          </div>
          {form.formState.errors.endTime && (
            <p className="text-sm text-[var(--danger)]">
              {form.formState.errors.endTime.message}
            </p>
          )}
          <p className="text-xs text-[var(--muted)]">
            Deja ambas horas vacías para mantener la nota sin horario.
          </p>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label className={labelClass}>
              Estado
              <select
                {...form.register("statusId", {
                  setValueAs: (value) =>
                    value === "" || value == null ? null : Number(value),
                })}
                className={fieldClass}
                disabled={statuses.isLoading || !statuses.data?.length}
              >
                {!statuses.data?.length && <option value="">Sin estados</option>}
                {statuses.data?.map((status) => (
                  <option key={status.id} value={status.id}>
                    {status.name}
                  </option>
                ))}
              </select>
            </label>
            <label className={labelClass}>
              Categoría
              <select
                {...form.register("categoryId", {
                  setValueAs: (value) => (value === "" ? null : Number(value)),
                })}
                className={fieldClass}
                disabled={categories.isLoading || !categories.data?.length}
              >
                {categories.data?.filter((item) => item.isActive).map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <label className={labelClass}>
              Persona
              <select
                {...form.register("personId", {
                  setValueAs: (value) => (value === "" ? null : Number(value)),
                })}
                className={fieldClass}
                disabled={people.isLoading}
              >
                <option value="">Sin persona</option>
                {people.data?.map((person) => (
                  <option key={person.id} value={person.id}>
                    {person.name}
                  </option>
                ))}
              </select>
            </label>
            <label className={labelClass}>
              Proyecto
              <select
                {...form.register("projectId", {
                  setValueAs: (value) => (value === "" ? null : Number(value)),
                })}
                className={fieldClass}
                disabled={projects.isLoading}
              >
                <option value="">Sin proyecto</option>
                {projects.data?.map((project) => (
                  <option key={project.id} value={project.id}>
                    {project.name}
                  </option>
                ))}
              </select>
            </label>
            <label className={labelClass}>
              Empresa
              <select
                {...form.register("companyId", {
                  setValueAs: (value) => (value === "" ? null : Number(value)),
                })}
                className={fieldClass}
                disabled={companies.isLoading}
              >
                <option value="">Sin empresa</option>
                {companies.data?.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </div>

      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--muted)] hover:bg-[var(--surface-muted)]"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={create.isPending || !statuses.data?.length || !categories.data?.length}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
        >
          {create.isPending ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
