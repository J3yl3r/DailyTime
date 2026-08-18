"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createStatus, deleteStatus, updateStatus } from "@/lib/api/statuses";
import { noteKeys, statusKeys, taskItemKeys } from "@/lib/query/keys";
import type { CreateStatusInput, UpdateStatusInput } from "@/schemas/status.schema";

export function useStatusMutations() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: statusKeys.all });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CreateStatusInput) => createStatus(body),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateStatusInput }) =>
      updateStatus(id, body),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (id: number) => deleteStatus(id),
    onSuccess: invalidate,
  });

  return { create, update, remove };
}
