"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  bulkDeleteJobOffers,
  bulkUpdateJobOfferStatus,
  deleteJobOffer,
  previewOfferTriage,
  reorderJobOffers,
  rescoreJobOffers,
  setJobOfferPinned,
  updateJobOfferStatus,
  updateOfferTriageSettings,
} from "@/lib/api/job-offers";
import { jobApplicationKeys, jobOfferKeys } from "@/lib/query/keys";
import type { OfferTriageSettings } from "@/types/api";

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

  const setPinned = useMutation({
    mutationFn: ({ id, pinned }: { id: number; pinned: boolean }) =>
      setJobOfferPinned(id, pinned),
    onSuccess: invalidateOffers,
  });

  /** Vista previa: no cambia datos, así que no invalida nada. */
  const previewTriage = useMutation({
    mutationFn: (settings: OfferTriageSettings) => previewOfferTriage(settings),
  });

  const saveTriageSettings = useMutation({
    mutationFn: (settings: OfferTriageSettings) => updateOfferTriageSettings(settings),
    onSuccess: (data) => {
      queryClient.setQueryData(jobOfferKeys.triageSettings(), data.settings);
      invalidateOffers();
    },
  });

  const rescore = useMutation({
    mutationFn: () => rescoreJobOffers(),
    onSuccess: invalidateOffers,
  });

  return {
    updateStatus,
    bulkUpdateStatus,
    reorder,
    remove,
    bulkRemove,
    setPinned,
    previewTriage,
    saveTriageSettings,
    rescore,
  };
}
