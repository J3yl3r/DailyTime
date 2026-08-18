import { z } from "zod";

export const companySchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().max(300).optional().or(z.literal("")),
  isActive: z.boolean().default(true),
});

export type CompanyFormValues = z.input<typeof companySchema>;
export type CompanyInput = z.output<typeof companySchema>;
