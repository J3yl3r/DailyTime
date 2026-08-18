"use client";

import { useQuery } from "@tanstack/react-query";
import { getCareerCatalog } from "@/lib/api/career";
import { careerCatalogKeys } from "@/lib/query/keys";
import type { CareerCatalogKind } from "@/types/api";

export function useCareerCatalog(kind: CareerCatalogKind, onlyActive?: boolean) {
  return useQuery({
    queryKey: careerCatalogKeys.list(kind, onlyActive),
    queryFn: () => getCareerCatalog(kind, onlyActive),
  });
}
