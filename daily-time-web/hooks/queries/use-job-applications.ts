"use client";

import { useQuery } from "@tanstack/react-query";
import { getJobApplications } from "@/lib/api/career";
import { jobApplicationKeys } from "@/lib/query/keys";

export function useJobApplications() {
  return useQuery({
    queryKey: jobApplicationKeys.list(),
    queryFn: getJobApplications,
  });
}
