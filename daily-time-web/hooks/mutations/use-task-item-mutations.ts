"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createTaskItem,
  updateTaskItem,
  deleteTaskItem,
} from "@/lib/api/task-items";
import { taskItemKeys } from "@/lib/query/keys";
import type { CreateTaskItemInput, UpdateTaskItemInput } from "@/schemas/task-item.schema";

export function useTaskItemMutations(workDate: string) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: taskItemKeys.byDate(workDate) });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CreateTaskItemInput) => createTaskItem(body),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateTaskItemInput }) =>
      updateTaskItem(id, body),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (id: number) => deleteTaskItem(id),
    onSuccess: invalidate,
  });

  return { create, update, remove };
}