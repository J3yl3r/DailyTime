"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createTimeEntrySchema,
  type CreateTimeEntryFormValues,
} from "@/schemas/time-entry.schema";
import { useTimeEntryMutations } from "@/hooks/mutations/use-time-entry-mutations";
import { toast } from "sonner";

type Props = {
  workDate: string;
  taskItemId?: number | null;
  noteId?: number | null;
  ownerLabel?: string;
  onClose: () => void;
};

export function TimeEntryForm({
  workDate,
  taskItemId = null,
  noteId = null,
  ownerLabel,
  onClose,
}: Props) {
  const { create } = useTimeEntryMutations(workDate);

  const form = useForm<CreateTimeEntryFormValues>({
    resolver: zodResolver(createTimeEntrySchema),
    defaultValues: {
      taskItemId,
      noteId,
      workDate,
      durationMinutes: 30,
      description: "",
    },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const payload = createTimeEntrySchema.parse({
        ...values,
        workDate,
        taskItemId: taskItemId ?? null,
        noteId: noteId ?? null,
        description: values.description?.trim() ? values.description.trim() : null,
      });

      await create.mutateAsync(payload);
      toast.success("Tiempo registrado");
      onClose();
    } catch (error) {
      form.setError("durationMinutes", {
        message: error instanceof Error ? error.message : "Error al registrar tiempo",
      });
    }
  });

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <input type="hidden" {...form.register("taskItemId", { valueAsNumber: true })} />
      <input type="hidden" {...form.register("noteId", { valueAsNumber: true })} />
      <input type="hidden" {...form.register("workDate")} />

      {ownerLabel && (
        <p className="rounded-lg bg-[var(--accent-soft)] px-3 py-2 text-sm text-[var(--accent)]">
          Tiempo para: <span className="font-medium">{ownerLabel}</span>
        </p>
      )}

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Duración (minutos)
        <input
          type="number"
          min={1}
          {...form.register("durationMinutes", { valueAsNumber: true })}
          className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
        />
      </label>
      {form.formState.errors.durationMinutes && (
        <p className="text-sm text-[var(--danger)]">
          {form.formState.errors.durationMinutes.message}
        </p>
      )}

      <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
        Descripción (opcional)
        <input
          {...form.register("description")}
          placeholder="Ej. Revisión, reunión..."
          className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
        />
      </label>

      {create.isError && (
        <p className="text-sm text-[var(--danger)]">{String(create.error)}</p>
      )}

      <div className="flex flex-col-reverse gap-2 border-t border-[var(--border)] pt-4 sm:flex-row sm:justify-end">
        <button
          type="submit"
          disabled={create.isPending}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
        >
          {create.isPending ? "Guardando..." : "Registrar"}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--muted)] hover:bg-[var(--surface-muted)]"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}