"use client";

import { useQuery } from "@tanstack/react-query";
import { getJobOfferMeta } from "@/lib/api/job-offers";
import { jobOfferKeys } from "@/lib/query/keys";

export function useJobOfferMeta() {
  return useQuery({
    queryKey: jobOfferKeys.meta(),
    queryFn: getJobOfferMeta,
  });
}
