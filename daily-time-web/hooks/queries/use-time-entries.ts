"use client";

import { useQuery } from "@tanstack/react-query";
import {
  getTimeEntriesByDateRange,
  getTimeEntriesByTaskItemId,
  getTimeEntriesByNoteId,
} from "@/lib/api/time-entries";
import { timeEntryKeys } from "@/lib/query/keys";

export function useTimeEntriesByDate(fromDate: string, toDate = fromDate) {
  return useQuery({
    queryKey: timeEntryKeys.byDate(fromDate, toDate),
    queryFn: () => getTimeEntriesByDateRange(fromDate, toDate),
    enabled: !!fromDate && !!toDate,
  });
}

export function useTimeEntriesByTask(taskItemId: number, enabled = true) {
  return useQuery({
    queryKey: timeEntryKeys.byTask(taskItemId),
    queryFn: () => getTimeEntriesByTaskItemId(taskItemId),
    enabled: enabled && taskItemId > 0,
  });
}

export function useTimeEntriesByNote(noteId: number, enabled = true) {
  return useQuery({
    queryKey: timeEntryKeys.byNote(noteId),
    queryFn: () => getTimeEntriesByNoteId(noteId),
    enabled: enabled && noteId > 0,
  });
}