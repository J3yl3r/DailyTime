import { apiClient } from "@/lib/api/client";
import type { WorkItemStatus, WorkItemType } from "@/types/api";
import type { CreateStatusInput, UpdateStatusInput } from "@/schemas/status.schema";

export function getStatuses(itemType?: WorkItemType) {
  const query = itemType ? `?itemType=${itemType}` : "";
  return apiClient.get<WorkItemStatus[]>(`/api/statuses${query}`);
}

export function getStatus(id: number) {
  return apiClient.get<WorkItemStatus>(`/api/statuses/${id}`);
}

export function createStatus(body: CreateStatusInput) {
  return apiClient.post<WorkItemStatus>("/api/statuses", body);
}

export function updateStatus(id: number, body: UpdateStatusInput) {
  return apiClient.put<WorkItemStatus>(`/api/statuses/${id}`, body);
}

export function deleteStatus(id: number) {
  return apiClient.delete<object>(`/api/statuses/${id}`);
}
