import { apiClient } from "@/lib/api/client";
import type { TaskItem } from "@/types/api";
import type { CreateTaskItemInput, UpdateTaskItemInput } from "@/schemas/task-item.schema";
import { toApiTime } from "@/lib/utils/date";

function withApiTimes<T extends { startTime?: string | null; endTime?: string | null }>(
  body: T
) {
  return {
    ...body,
    startTime: toApiTime(body.startTime),
    endTime: toApiTime(body.endTime),
  };
}

export function getTaskItemsByDateRange(fromDate: string, toDate: string) {
  return apiClient.get<TaskItem[]>(
    `/api/taskitems?fromDate=${fromDate}&toDate=${toDate}`
  );
}

export function getTaskItem(id: number) {
  return apiClient.get<TaskItem>(`/api/taskitems/${id}`);
}

export function getTaskItemChildren(id: number) {
  return apiClient.get<TaskItem[]>(`/api/taskitems/${id}/children`);
}

export function createTaskItem(body: CreateTaskItemInput) {
  return apiClient.post<TaskItem>("/api/taskitems", withApiTimes(body));
}

export function updateTaskItem(id: number, body: UpdateTaskItemInput) {
  return apiClient.put<TaskItem>(`/api/taskitems/${id}`, withApiTimes(body));
}

export function deleteTaskItem(id: number) {
  return apiClient.delete<null>(`/api/taskitems/${id}`);
}
