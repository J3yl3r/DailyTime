"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createJobPortal,
  deleteJobPortal,
  queueJobPortalScrape,
  updateJobPortal,
} from "@/lib/api/job-portals";
import { jobPortalKeys } from "@/lib/query/keys";
import type { JobPortalInput } from "@/schemas/job-portal.schema";

export function useJobPortalMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: jobPortalKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: JobPortalInput) => createJobPortal(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: JobPortalInput }) =>
      updateJobPortal(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteJobPortal(id),
    onSuccess: invalidate,
  });
  const queueScrape = useMutation({
    mutationFn: (id: number) => queueJobPortalScrape(id),
    onSuccess: invalidate,
  });

  return { create, update, remove, queueScrape };
}
