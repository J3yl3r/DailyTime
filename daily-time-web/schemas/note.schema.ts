import { z } from "zod";

const apiDateSchema = z
  .string()
  .regex(/^\d{4}-\d{2}-\d{2}$/, "Formato de fecha: YYYY-MM-DD");

const timeSchema = z.string().regex(/^\d{2}:\d{2}$/).nullable();

function validateSchedule(
  data: {
    workDate?: string | null;
    startTime: string | null;
    endTime: string | null;
  },
  context: z.RefinementCtx
) {
  if ((data.startTime === null) !== (data.endTime === null)) {
    context.addIssue({
      code: "custom",
      path: ["endTime"],
      message: "Indica la hora de inicio y la hora final.",
    });
  } else if (
    data.startTime !== null &&
    data.endTime !== null &&
    data.endTime <= data.startTime
  ) {
    context.addIssue({
      code: "custom",
      path: ["endTime"],
      message: "La hora final debe ser posterior a la hora de inicio.",
    });
  }

  if (data.startTime !== null && !data.workDate) {
    context.addIssue({
      code: "custom",
      path: ["workDate"],
      message: "Una nota con horario debe tener una fecha.",
    });
  }
}

export const createNoteSchema = z.object({
  title: z.string().trim().max(200).nullable().optional(),
  content: z.string().trim().min(1, "El contenido es obligatorio"),
  workDate: apiDateSchema.nullable().optional(),
  startTime: timeSchema.default(null),
  endTime: timeSchema.default(null),
  parentNoteId: z.number().int().positive().nullable().optional(),
  statusId: z.number().int().positive().nullable().optional(),
  categoryId: z.number().int().positive().nullable().optional(),
  personId: z.number().int().positive().nullable().optional(),
  projectId: z.number().int().positive().nullable().optional(),
  companyId: z.number().int().positive().nullable().optional(),
  sortOrder: z.number().int().min(0).default(0),
  durationMinutes: z.number().int().min(0).default(0),
}).superRefine(validateSchedule);

export const updateNoteSchema = z.object({
  title: z.string().trim().max(200).nullable().optional(),
  content: z.string().trim().min(1, "El contenido es obligatorio"),
  workDate: apiDateSchema.nullable().optional(),
  startTime: timeSchema,
  endTime: timeSchema,
  parentNoteId: z.number().int().positive().nullable().optional(),
  statusId: z.number().int().positive(),
  categoryId: z.number().int().positive(),
  personId: z.number().int().positive().nullable().optional(),
  projectId: z.number().int().positive().nullable().optional(),
  companyId: z.number().int().positive().nullable().optional(),
  sortOrder: z.number().int().min(0),
  durationMinutes: z.number().int().min(0),
}).superRefine(validateSchedule);

export type CreateNoteFormValues = z.input<typeof createNoteSchema>;
export type UpdateNoteFormValues = z.input<typeof updateNoteSchema>;
export type CreateNoteInput = z.output<typeof createNoteSchema>;
export type UpdateNoteInput = z.output<typeof updateNoteSchema>;
