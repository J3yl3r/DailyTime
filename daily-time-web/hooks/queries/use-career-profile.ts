"use client";

import { useQuery } from "@tanstack/react-query";
import { getCareerProfile } from "@/lib/api/career";
import { careerProfileKeys } from "@/lib/query/keys";

export function useCareerProfile() {
  return useQuery({
    queryKey: careerProfileKeys.current(),
    queryFn: getCareerProfile,
  });
}
