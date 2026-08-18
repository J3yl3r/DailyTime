import { z } from "zod";
import { workItemTypeSchema } from "@/schemas/status.schema";

export const categorySchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().min(1, "La descripción es obligatoria").max(300),
  itemType: workItemTypeSchema,
  isActive: z.boolean().default(true),
});

export type CategoryFormValues = z.input<typeof categorySchema>;
export type CategoryInput = z.output<typeof categorySchema>;
