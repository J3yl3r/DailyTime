"use client";

import { useQuery } from "@tanstack/react-query";
import { getWorkExperiences } from "@/lib/api/career";
import { workExperienceKeys } from "@/lib/query/keys";

export function useWorkExperiences() {
  return useQuery({
    queryKey: workExperienceKeys.list(),
    queryFn: getWorkExperiences,
  });
}
