"use client";

import { useQuery } from "@tanstack/react-query";
import { getOfferAiStatus } from "@/lib/api/job-offers";
import { jobOfferKeys } from "@/lib/query/keys";

export function useOfferAiStatus(enabled = true) {
  return useQuery({
    queryKey: jobOfferKeys.aiStatus(),
    queryFn: getOfferAiStatus,
    enabled,
    // Solo se refresca solo mientras la API está analizando.
    refetchInterval: (query) => (query.state.data?.isRunning ? 10_000 : false),
  });
}
