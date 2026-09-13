import { z } from "zod";

export const SCHEDULE_TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;

export const scrapeScheduleSchema = z
  .object({
    enabled: z.boolean(),
    times: z
      .array(z.string().regex(SCHEDULE_TIME_PATTERN, "Usa el formato HH:mm"))
      .max(24, "Máximo 24 horas por día"),
    /** Días ISO (1 = lunes … 7 = domingo). Vacío = todos los días. */
    days: z.array(z.number().int().min(1).max(7)),
  })
  .refine((value) => !value.enabled || value.times.length > 0, {
    message: "Agrega al menos una hora para activar la ejecución automática.",
    path: ["times"],
  });

export type ScrapeScheduleInput = z.output<typeof scrapeScheduleSchema>;
