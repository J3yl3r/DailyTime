"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCategory, deleteCategory, updateCategory } from "@/lib/api/categories";
import { categoryKeys, noteKeys, taskItemKeys } from "@/lib/query/keys";
import type { CategoryInput } from "@/schemas/category.schema";

export function useCategoryMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: categoryKeys.all });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CategoryInput) => createCategory(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: CategoryInput }) =>
      updateCategory(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteCategory(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
