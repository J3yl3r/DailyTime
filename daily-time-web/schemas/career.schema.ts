import { z } from "zod";

export const careerCatalogSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(150),
  description: z.string().trim().max(300).optional().or(z.literal("")),
  color: z.string().trim().max(20).optional().or(z.literal("")),
  sortOrder: z.coerce.number().int().optional(),
  isActive: z.boolean().default(true),
});

export type CareerCatalogFormValues = z.input<typeof careerCatalogSchema>;
export type CareerCatalogInput = z.output<typeof careerCatalogSchema>;

export const workExperienceSchema = z.object({
  companyId: z.coerce.number().int().positive("Selecciona una empresa"),
  positionId: z.coerce.number().int().positive("Selecciona un cargo"),
  locationId: z.union([z.coerce.number().int().positive(), z.literal(""), z.null()]).optional(),
  fieldId: z.union([z.coerce.number().int().positive(), z.literal(""), z.null()]).optional(),
  startDate: z.string().min(1, "La fecha de inicio es obligatoria"),
  endDate: z.string().optional().or(z.literal("")),
  isCurrent: z.boolean().default(false),
  summary: z.string().trim().max(2000).optional().or(z.literal("")),
  achievements: z.string().trim().max(4000).optional().or(z.literal("")),
  technologyIds: z.array(z.number().int().positive()).default([]),
});

export type WorkExperienceFormValues = z.input<typeof workExperienceSchema>;
export type WorkExperienceInput = z.output<typeof workExperienceSchema>;

export const jobApplicationSchema = z.object({
  companyId: z.coerce.number().int().positive("Selecciona una empresa"),
  positionId: z.coerce.number().int().positive("Selecciona un cargo"),
  locationId: z.union([z.coerce.number().int().positive(), z.literal(""), z.null()]).optional(),
  fieldId: z.union([z.coerce.number().int().positive(), z.literal(""), z.null()]).optional(),
  statusId: z.coerce.number().int().positive("Selecciona un estado"),
  appliedAt: z.string().min(1, "La fecha es obligatoria"),
  url: z.string().trim().max(500).optional().or(z.literal("")),
  contact: z.string().trim().max(200).optional().or(z.literal("")),
  notes: z.string().trim().max(2000).optional().or(z.literal("")),
  workExperienceId: z.union([z.coerce.number().int().positive(), z.literal(""), z.null()]).optional(),
});

export type JobApplicationFormValues = z.input<typeof jobApplicationSchema>;
export type JobApplicationInput = z.output<typeof jobApplicationSchema>;
