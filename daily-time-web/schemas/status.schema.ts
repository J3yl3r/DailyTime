import { z } from "zod";

export const workItemTypeSchema = z.enum(["task", "note"]);

export const createStatusSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().min(1, "La descripción es obligatoria").max(300),
  color: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Usa un color hexadecimal válido"),
  isFinal: z.boolean().default(false),
  itemType: workItemTypeSchema,
});

export const updateStatusSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().min(1, "La descripción es obligatoria").max(300),
  color: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Usa un color hexadecimal válido"),
  isFinal: z.boolean(),
  itemType: workItemTypeSchema,
});

export type CreateStatusFormValues = z.input<typeof createStatusSchema>;
export type UpdateStatusFormValues = z.input<typeof updateStatusSchema>;
export type CreateStatusInput = z.output<typeof createStatusSchema>;
export type UpdateStatusInput = z.output<typeof updateStatusSchema>;
