"use client";

import { useQuery } from "@tanstack/react-query";
import { getJobPortals } from "@/lib/api/job-portals";
import { jobPortalKeys } from "@/lib/query/keys";

export function useJobPortals(onlyActive?: boolean) {
  return useQuery({
    queryKey: jobPortalKeys.list(onlyActive),
    queryFn: () => getJobPortals(onlyActive),
  });
}
