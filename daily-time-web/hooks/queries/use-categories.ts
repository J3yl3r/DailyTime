"use client";

import { useQuery } from "@tanstack/react-query";
import { getCategories } from "@/lib/api/categories";
import { categoryKeys } from "@/lib/query/keys";
import type { WorkItemType } from "@/types/api";

export function useCategories(itemType?: WorkItemType) {
  return useQuery({
    queryKey: categoryKeys.byType(itemType),
    queryFn: () => getCategories(itemType),
  });
}
