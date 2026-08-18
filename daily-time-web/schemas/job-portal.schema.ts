import { z } from "zod";

export const jobPortalSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(150),
  url: z.string().trim().min(1, "La URL es obligatoria").max(500),
  loginUrl: z.string().trim().max(500).optional().or(z.literal("")),
  notes: z.string().trim().max(2000).optional().or(z.literal("")),
  scrapeConfig: z.string().trim().max(4000).optional().or(z.literal("")),
  isActive: z.boolean().default(true),
});

export type JobPortalFormValues = z.input<typeof jobPortalSchema>;
export type JobPortalInput = z.output<typeof jobPortalSchema>;
