"use client";

import { useQuery } from "@tanstack/react-query";
import { getOfferTriageSettings } from "@/lib/api/job-offers";
import { jobOfferKeys } from "@/lib/query/keys";

export function useOfferTriageSettings(enabled = true) {
  return useQuery({
    queryKey: jobOfferKeys.triageSettings(),
    queryFn: getOfferTriageSettings,
    enabled,
  });
}
