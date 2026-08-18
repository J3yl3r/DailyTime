import { z } from "zod";

const timeSchema = z.string().regex(/^\d{2}:\d{2}$/).nullable();

function validateSchedule(
  data: { startTime: string | null; endTime: string | null },
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
}

export const createTaskItemSchema = z.object({
  title: z.string().trim().min(1).max(300),
  content: z.string().trim().optional().or(z.literal("")),
  workDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  startTime: timeSchema.default(null),
  endTime: timeSchema.default(null),
  parentTaskId: z.number().int().positive().nullable().optional(),
  statusId: z.number().int().positive().nullable().optional(),
  categoryId: z.number().int().positive().nullable().optional(),
  personId: z.number().int().positive().nullable().optional(),
  projectId: z.number().int().positive().nullable().optional(),
  companyId: z.number().int().positive().nullable().optional(),
  sortOrder: z.number().int().min(0).default(0),
  durationMinutes: z.number().int().min(0).default(0),
}).superRefine(validateSchedule);

export const updateTaskItemSchema = z.object({
  title: z.string().trim().min(1).max(300),
  content: z.string().trim().optional().nullable().or(z.literal("")),
  workDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  startTime: timeSchema,
  endTime: timeSchema,
  parentTaskId: z.number().int().positive().nullable().optional(),
  statusId: z.number().int().positive(),
  categoryId: z.number().int().positive(),
  personId: z.number().int().positive().nullable().optional(),
  projectId: z.number().int().positive().nullable().optional(),
  companyId: z.number().int().positive().nullable().optional(),
  isCompleted: z.boolean(),
  sortOrder: z.number().int().min(0),
  durationMinutes: z.number().int().min(0),
}).superRefine(validateSchedule);

export type CreateTaskItemFormValues = z.input<typeof createTaskItemSchema>;
export type CreateTaskItemInput = z.output<typeof createTaskItemSchema>;
export type UpdateTaskItemInput = z.infer<typeof updateTaskItemSchema>;
