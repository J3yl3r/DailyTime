"use client";

import { useQuery } from "@tanstack/react-query";
import { getJobOffers } from "@/lib/api/job-offers";
import { jobOfferKeys } from "@/lib/query/keys";
import type { JobOfferFilters } from "@/types/api";

export function useJobOffers(filters?: JobOfferFilters | null) {
  return useQuery({
    queryKey: jobOfferKeys.list(filters),
    queryFn: () => getJobOffers(filters ?? undefined),
  });
}
