"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  bulkDeleteJobOffers,
  bulkUpdateJobOfferStatus,
  deleteJobOffer,
  reorderJobOffers,
  updateJobOfferStatus,
} from "@/lib/api/job-offers";
import { jobApplicationKeys, jobOfferKeys } from "@/lib/query/keys";

export function useJobOfferMutations() {
  const queryClient = useQueryClient();
  const invalidateOffers = () => {
    queryClient.invalidateQueries({ queryKey: jobOfferKeys.all });
  };
  const invalidateOffersAndApplications = () => {
    invalidateOffers();
    queryClient.invalidateQueries({ queryKey: jobApplicationKeys.all });
  };

  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: number; status: string }) =>
      updateJobOfferStatus(id, status),
    onSuccess: (_data, variables) => {
      if (variables.status === "applied") invalidateOffersAndApplications();
      else invalidateOffers();
    },
  });

  const bulkUpdateStatus = useMutation({
    mutationFn: ({ ids, status }: { ids: number[]; status: string }) =>
      bulkUpdateJobOfferStatus(ids, status),
    onSuccess: (_data, variables) => {
      if (variables.status === "applied") invalidateOffersAndApplications();
      else invalidateOffers();
    },
  });

  const reorder = useMutation({
    mutationFn: (ids: number[]) => reorderJobOffers(ids),
    onSuccess: invalidateOffers,
  });

  const remove = useMutation({
    mutationFn: (id: number) => deleteJobOffer(id),
    onSuccess: invalidateOffers,
  });

  const bulkRemove = useMutation({
    mutationFn: (ids: number[]) => bulkDeleteJobOffers(ids),
    onSuccess: invalidateOffers,
  });

  return { updateStatus, bulkUpdateStatus, reorder, remove, bulkRemove };
}
