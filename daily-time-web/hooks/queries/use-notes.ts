"use client";

import { useQuery } from "@tanstack/react-query";
import { getNotesByDateRange, getNoteChildren } from "@/lib/api/notes";
import { noteKeys } from "@/lib/query/keys";

export function useNotesByDate(fromDate: string, toDate = fromDate) {
  return useQuery({
    queryKey: noteKeys.byDate(fromDate, toDate),
    queryFn: () => getNotesByDateRange(fromDate, toDate),
    enabled: !!fromDate && !!toDate,
  });
}

export function useNoteChildren(parentId: number, enabled = true) {
  return useQuery({
    queryKey: noteKeys.children(parentId),
    queryFn: () => getNoteChildren(parentId),
    enabled: enabled && parentId > 0,
  });
}