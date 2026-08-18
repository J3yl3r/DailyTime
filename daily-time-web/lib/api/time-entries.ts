import { apiClient } from "@/lib/api/client";
import type { TimeEntry } from "@/types/api";
import type {
  CreateTimeEntryInput,
  UpdateTimeEntryInput,
} from "@/schemas/time-entry.schema";

export function getTimeEntriesByDateRange(fromDate: string, toDate: string) {
  return apiClient.get<TimeEntry[]>(
    `/api/timeentries?fromDate=${fromDate}&toDate=${toDate}`
  );
}

export function getTimeEntriesByTaskItemId(taskItemId: number) {
  return apiClient.get<TimeEntry[]>(`/api/timeentries/by-task/${taskItemId}`);
}

export function getTimeEntriesByNoteId(noteId: number) {
  return apiClient.get<TimeEntry[]>(`/api/timeentries/by-note/${noteId}`);
}

export function getTimeEntry(id: number) {
  return apiClient.get<TimeEntry>(`/api/timeentries/${id}`);
}

export function createTimeEntry(body: CreateTimeEntryInput) {
  return apiClient.post<TimeEntry>("/api/timeentries", body);
}

export function updateTimeEntry(id: number, body: UpdateTimeEntryInput) {
  return apiClient.put<TimeEntry>(`/api/timeentries/${id}`, body);
}

export function deleteTimeEntry(id: number) {
  return apiClient.delete<null>(`/api/timeentries/${id}`);
}