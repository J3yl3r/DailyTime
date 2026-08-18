import { z } from "zod";

export const personSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().max(300).optional().or(z.literal("")),
  isActive: z.boolean().default(true),
});

export type PersonFormValues = z.input<typeof personSchema>;
export type PersonInput = z.output<typeof personSchema>;
