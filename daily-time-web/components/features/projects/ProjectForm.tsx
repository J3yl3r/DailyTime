"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import type { Project } from "@/types/api";
import { projectSchema, type ProjectFormValues } from "@/schemas/project.schema";
import { useProjectMutations } from "@/hooks/mutations/use-project-mutations";

export function ProjectForm({
  project,
  onClose,
}: {
  project?: Project | null;
  onClose: () => void;
}) {
  const { create, update } = useProjectMutations();
  const form = useForm<ProjectFormValues>({
    resolver: zodResolver(projectSchema),
    defaultValues: {
      name: project?.name ?? "",
      description: project?.description ?? "",
      isActive: project?.isActive ?? true,
    },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const payload = projectSchema.parse(values);
      if (project) await update.mutateAsync({ id: project.id, body: payload });
      else await create.mutateAsync(payload);
      toast.success(project ? "Proyecto actualizado" : "Proyecto creado");
      onClose();
    } catch (error) {
      form.setError("name", {
        message: error instanceof Error ? error.message : "Error al guardar",
      });
    }
  });

  const pending = create.isPending || update.isPending;
  const inputClass =
    "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Nombre
        <input {...form.register("name")} placeholder="Rediseño web" className={inputClass} autoFocus />
      </label>
      {form.formState.errors.name && (
        <p className="text-sm text-[var(--danger)]">{form.formState.errors.name.message}</p>
      )}
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Descripción (opcional)
        <textarea
          {...form.register("description")}
          rows={3}
          placeholder="Objetivo o alcance del proyecto"
          className={inputClass}
        />
      </label>
      {form.formState.errors.description && (
        <p className="text-sm text-[var(--danger)]">
          {form.formState.errors.description.message}
        </p>
      )}
      <label className="inline-flex items-center gap-2 text-sm">
        <input type="checkbox" {...form.register("isActive")} className="size-4 accent-[var(--accent)]" />
        Proyecto activo
      </label>
      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        <button type="button" onClick={onClose} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm">
          Cancelar
        </button>
        <button type="submit" disabled={pending} className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50">
          {pending ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
