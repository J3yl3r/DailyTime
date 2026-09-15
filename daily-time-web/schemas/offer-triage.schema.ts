import { z } from "zod";

const textList = z.array(z.string().trim().min(1).max(80)).max(100);

export const offerTriageSchema = z
  .object({
    autoDiscardEnabled: z.boolean(),
    maxAgeDays: z
      .number({ error: "Indica un número de días" })
      .int()
      .min(0, "Mínimo 0 (sin límite)")
      .max(365, "Máximo 365 días"),
    allowedCountries: textList,
    discardOnsiteAbroad: z.boolean(),
    discardOnsiteLocal: z.boolean(),
    discardResidencyAbroad: z.boolean(),
    discardOutOfProfile: z.boolean(),
    discardDuplicates: z.boolean(),
    excludedTitleKeywords: textList,
    blockedCompanies: textList,
    penalizeEnglishGap: z.boolean(),
    tierAMin: z
      .number({ error: "Indica un puntaje" })
      .int()
      .min(1, "Mínimo 1")
      .max(100, "Máximo 100"),
    tierBMin: z
      .number({ error: "Indica un puntaje" })
      .int()
      .min(0, "Mínimo 0")
      .max(99, "Máximo 99"),
  })
  .refine((value) => value.tierBMin < value.tierAMin, {
    message: "El mínimo de B debe ser menor que el de A",
    path: ["tierBMin"],
  });

export type OfferTriageFormValues = z.input<typeof offerTriageSchema>;
export type OfferTriageInput = z.output<typeof offerTriageSchema>;
