"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import type { WorkItemStatus } from "@/types/api";
import {
  createStatusSchema,
  updateStatusSchema,
  type CreateStatusFormValues,
  type UpdateStatusFormValues,
} from "@/schemas/status.schema";
import { useStatusMutations } from "@/hooks/mutations/use-status-mutations";

type Props = {
  status?: WorkItemStatus | null;
  onClose: () => void;
};

export function StatusForm({ status = null, onClose }: Props) {
  const { create, update } = useStatusMutations();
  const isEdit = status != null;

  const form = useForm<CreateStatusFormValues | UpdateStatusFormValues>({
    resolver: zodResolver(isEdit ? updateStatusSchema : createStatusSchema),
    defaultValues: {
      name: status?.name ?? "",
      description: status?.description ?? "",
      color: status?.color ?? "#64748B",
      isFinal: status?.isFinal ?? false,
      itemType: (status?.itemType as "task" | "note") ?? "task",
    },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (isEdit && status) {
        const payload = updateStatusSchema.parse(values);
        await update.mutateAsync({ id: status.id, body: payload });
        toast.success("Estado actualizado");
      } else {
        const payload = createStatusSchema.parse(values);
        await create.mutateAsync(payload);
        toast.success("Estado creado");
      }
      onClose();
    } catch (error) {
      form.setError("name", {
        message: error instanceof Error ? error.message : "Error al guardar el estado",
      });
    }
  });

  const pending = create.isPending || update.isPending;
  const color = form.watch("color");

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Tipo
        <select
          {...form.register("itemType")}
          className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
        >
          <option value="task">Tarea</option>
          <option value="note">Nota</option>
        </select>
      </label>

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Nombre
        <input
          {...form.register("name")}
          placeholder="Pendiente"
          className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
          autoFocus
        />
      </label>
      {form.formState.errors.name && (
        <p className="text-sm text-[var(--danger)]">{form.formState.errors.name.message}</p>
      )}

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Descripción
        <textarea
          {...form.register("description")}
          rows={3}
          placeholder="Explica cuándo debe usarse este estado"
          className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
        />
      </label>
      {form.formState.errors.description && (
        <p className="text-sm text-[var(--danger)]">
          {form.formState.errors.description.message}
        </p>
      )}

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Color
        <div className="flex items-center gap-2">
          <input
            type="color"
            value={color}
            onChange={(event) =>
              form.setValue("color", event.target.value.toUpperCase(), {
                shouldDirty: true,
                shouldValidate: true,
              })
            }
            className="h-10 w-14 cursor-pointer rounded-md border border-[var(--border)] bg-white p-1"
          />
          <input
            {...form.register("color")}
            placeholder="#64748B"
            className="flex-1 rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal uppercase outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
          />
        </div>
      </label>
      {form.formState.errors.color && (
        <p className="text-sm text-[var(--danger)]">{form.formState.errors.color.message}</p>
      )}

      <label className="inline-flex items-center gap-2 text-sm text-[var(--ink)]">
        <input type="checkbox" {...form.register("isFinal")} className="size-4 accent-[var(--accent)]" />
        Estado final (marca completado en tareas)
      </label>

      <div className="flex flex-col-reverse gap-2 border-t border-[var(--border)] pt-4 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--muted)] hover:bg-[var(--surface-muted)]"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={pending}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
        >
          {pending ? "Guardando…" : isEdit ? "Actualizar" : "Crear"}
        </button>
      </div>
    </form>
  );
}
