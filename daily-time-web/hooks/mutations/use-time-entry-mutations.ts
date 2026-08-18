"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createTimeEntry,
  updateTimeEntry,
  deleteTimeEntry,
} from "@/lib/api/time-entries";
import { timeEntryKeys } from "@/lib/query/keys";
import type {
  CreateTimeEntryInput,
  UpdateTimeEntryInput,
} from "@/schemas/time-entry.schema";

export function useTimeEntryMutations(workDate: string) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: timeEntryKeys.byDate(workDate) });
    queryClient.invalidateQueries({ queryKey: timeEntryKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CreateTimeEntryInput) => createTimeEntry(body),
    onSuccess: (_, variables) => {
      invalidate();
      if (variables.taskItemId) {
        queryClient.invalidateQueries({
          queryKey: timeEntryKeys.byTask(variables.taskItemId),
        });
      }
      if (variables.noteId) {
        queryClient.invalidateQueries({
          queryKey: timeEntryKeys.byNote(variables.noteId),
        });
      }
    },
  });

  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateTimeEntryInput }) =>
      updateTimeEntry(id, body),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (id: number) => deleteTimeEntry(id),
    onSuccess: invalidate,
  });

  return { create, update, remove };
}