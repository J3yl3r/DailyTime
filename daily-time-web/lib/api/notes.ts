import { apiClient } from "@/lib/api/client";
import type { Note } from "@/types/api";
import type { CreateNoteInput, UpdateNoteInput } from "@/schemas/note.schema";
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

export function getNotesByDateRange(fromDate: string, toDate: string) {
  return apiClient.get<Note[]>(
    `/api/notes?fromDate=${fromDate}&toDate=${toDate}`
  );
}

export function getNote(id: number) {
  return apiClient.get<Note>(`/api/notes/${id}`);
}

export function getNoteChildren(id: number) {
  return apiClient.get<Note[]>(`/api/notes/${id}/children`);
}

export function createNote(body: CreateNoteInput) {
  return apiClient.post<Note>("/api/notes", withApiTimes(body));
}

export function updateNote(id: number, body: UpdateNoteInput) {
  return apiClient.put<Note>(`/api/notes/${id}`, withApiTimes(body));
}

export function deleteNote(id: number) {
  return apiClient.delete<null>(`/api/notes/${id}`);
}