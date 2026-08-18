"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { upsertCareerProfile } from "@/lib/api/career";
import { careerProfileKeys } from "@/lib/query/keys";
import type { CareerProfileInput } from "@/types/api";

export function useCareerProfileMutations() {
  const queryClient = useQueryClient();

  const upsert = useMutation({
    mutationFn: (body: CareerProfileInput) => upsertCareerProfile(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: careerProfileKeys.all });
    },
  });

  return { upsert };
}
