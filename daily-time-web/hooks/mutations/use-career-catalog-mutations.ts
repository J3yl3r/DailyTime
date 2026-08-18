"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createCareerCatalog,
  deleteCareerCatalog,
  updateCareerCatalog,
} from "@/lib/api/career";
import {
  careerCatalogKeys,
  jobApplicationKeys,
  workExperienceKeys,
} from "@/lib/query/keys";
import type { CareerCatalogInput } from "@/schemas/career.schema";
import type { CareerCatalogKind } from "@/types/api";

export function useCareerCatalogMutations(kind: CareerCatalogKind) {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: careerCatalogKeys.all });
    queryClient.invalidateQueries({ queryKey: workExperienceKeys.all });
    queryClient.invalidateQueries({ queryKey: jobApplicationKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CareerCatalogInput) => createCareerCatalog(kind, body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: CareerCatalogInput }) =>
      updateCareerCatalog(kind, id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteCareerCatalog(kind, id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
