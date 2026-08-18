"use client";

import { useEffect, useRef } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import {
  createTaskItemSchema,
  type CreateTaskItemFormValues,
} from "@/schemas/task-item.schema";
import { useTaskItemMutations } from "@/hooks/mutations/use-task-item-mutations";
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
  initialValues?: Partial<CreateTaskItemFormValues>;
  voicePatch?: VoiceFormFieldPatch | null;
  submitRequestId?: number;
  parentTaskId?: number | null;
  parentLabel?: string;
  editingId?: number | null;
  isCompleted?: boolean;
  onClose: () => void;
};

export function TaskItemForm({
  workDate,
  initialStartTime = null,
  initialEndTime = null,
  initialValues,
  voicePatch = null,
  submitRequestId = 0,
  parentTaskId = null,
  parentLabel,
  editingId = null,
  isCompleted = false,
  onClose,
}: Props) {
  const { create, update } = useTaskItemMutations(workDate);
  const statuses = useStatuses("task");
  const categories = useCategories("task");
  const people = usePeople(true);
  const projects = useProjects(true);
  const companies = useCompanies(true);
  const defaultStatusId = statuses.data?.[0]?.id ?? null;
  const defaultCategoryId = categories.data?.find((item) => item.isActive)?.id ?? null;

  const form = useForm<CreateTaskItemFormValues>({
    resolver: zodResolver(createTaskItemSchema),
    defaultValues: {
      title: initialValues?.title ?? "",
      content: initialValues?.content ?? "",
      workDate: initialValues?.workDate ?? workDate,
      startTime: initialValues?.startTime ?? initialStartTime,
      endTime: initialValues?.endTime ?? initialEndTime,
      parentTaskId,
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
      const payload = createTaskItemSchema.parse({
        ...values,
        workDate: values.workDate ?? workDate,
        parentTaskId: parentTaskId ?? null,
        statusId: values.statusId ?? defaultStatusId,
        categoryId: values.categoryId ?? defaultCategoryId,
      });

      if (editingId != null) {
        await update.mutateAsync({
          id: editingId,
          body: {
            ...payload,
            statusId: payload.statusId!,
            categoryId: payload.categoryId!,
            isCompleted,
          },
        });
        toast.success("Tarea actualizada");
      } else {
        await create.mutateAsync(payload);
        toast.success("Tarea creada");
      }
      onClose();
    } catch (error) {
      form.setError("title", {
        message:
          error instanceof Error
            ? error.message
            : editingId != null
              ? "Error al actualizar la tarea"
              : "Error al crear la tarea",
      });
    }
  });

  const submitRef = useRef(onSubmit);
  submitRef.current = onSubmit;

  useEffect(() => {
    if (!voicePatch) return;
    form.setValue(voicePatch.field as keyof CreateTaskItemFormValues, voicePatch.value as never, {
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
      <input type="hidden" {...form.register("parentTaskId")} />
      <input type="hidden" {...form.register("sortOrder", { valueAsNumber: true })} />
      <input type="hidden" {...form.register("durationMinutes", { valueAsNumber: true })} />

      {parentTaskId != null && (
        <p className="rounded-lg bg-[var(--accent-soft)] px-3 py-2 text-sm text-[var(--accent)]">
          Subtarea de{" "}
          <span className="font-medium">{parentLabel ?? `#${parentTaskId}`}</span>
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
                  placeholder="Ej: revisar informe"
                  className={fieldClass}
                  autoFocus
                />
              )}
            />
          </label>
          {form.formState.errors.title && (
            <p className="text-sm text-[var(--danger)]">{form.formState.errors.title.message}</p>
          )}

          <label className={labelClass}>
            Contenido
            <Controller
              name="content"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  placeholder="Detalle opcional de la tarea"
                  rows={4}
                  className={`${fieldClass} min-h-[6.5rem] resize-y`}
                />
              )}
            />
          </label>
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
            Deja ambas horas vacías para mantener la tarea sin horario.
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
