"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createJobApplication,
  deleteJobApplication,
  bulkDeleteJobApplications,
  updateJobApplication,
} from "@/lib/api/career";
import { jobApplicationKeys } from "@/lib/query/keys";
import type { JobApplicationInput } from "@/schemas/career.schema";

export function useJobApplicationMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: jobApplicationKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: JobApplicationInput) => createJobApplication(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: JobApplicationInput }) =>
      updateJobApplication(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteJobApplication(id),
    onSuccess: invalidate,
  });
  const bulkRemove = useMutation({
    mutationFn: (ids: number[]) => bulkDeleteJobApplications(ids),
    onSuccess: invalidate,
  });
  return { create, update, remove, bulkRemove };
}
