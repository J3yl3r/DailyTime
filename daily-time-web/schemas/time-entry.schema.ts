import { z } from "zod";

const apiDateSchema = z
  .string()
  .regex(/^\d{4}-\d{2}-\d{2}$/, "Formato de fecha: YYYY-MM-DD");

const ownerRefine = (
  data: { taskItemId?: number | null; noteId?: number | null },
  ctx: z.RefinementCtx
) => {
  const hasTask = data.taskItemId != null;
  const hasNote = data.noteId != null;

  if (hasTask === hasNote) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: "Indica taskItemId o noteId, pero no ambos ni ninguno.",
      path: ["taskItemId"],
    });
  }
};

export const createTimeEntrySchema = z
  .object({
    taskItemId: z.number().int().positive().nullable().optional(),
    noteId: z.number().int().positive().nullable().optional(),
    workDate: apiDateSchema,
    durationMinutes: z.coerce.number().int().min(1, "La duración debe ser mayor a 0"),
    description: z.string().trim().max(500).nullable().optional(),
  })
  .superRefine(ownerRefine);

export const updateTimeEntrySchema = z.object({
  workDate: apiDateSchema,
  durationMinutes: z.coerce.number().int().min(1, "La duración debe ser mayor a 0"),
  description: z.string().trim().max(500).nullable().optional(),
});

export type CreateTimeEntryFormValues = z.input<typeof createTimeEntrySchema>;
export type UpdateTimeEntryFormValues = z.input<typeof updateTimeEntrySchema>;
export type CreateTimeEntryInput = z.output<typeof createTimeEntrySchema>;
export type UpdateTimeEntryInput = z.output<typeof updateTimeEntrySchema>;