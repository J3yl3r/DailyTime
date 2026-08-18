"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createWorkExperience,
  deleteWorkExperience,
  updateWorkExperience,
} from "@/lib/api/career";
import { jobApplicationKeys, workExperienceKeys } from "@/lib/query/keys";
import type { WorkExperienceInput } from "@/schemas/career.schema";

export function useWorkExperienceMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: workExperienceKeys.all });
    queryClient.invalidateQueries({ queryKey: jobApplicationKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: WorkExperienceInput) => createWorkExperience(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: WorkExperienceInput }) =>
      updateWorkExperience(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteWorkExperience(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
