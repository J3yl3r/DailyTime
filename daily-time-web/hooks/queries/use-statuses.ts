"use client";

import { useQuery } from "@tanstack/react-query";
import { getStatuses } from "@/lib/api/statuses";
import { statusKeys } from "@/lib/query/keys";
import type { WorkItemType } from "@/types/api";

export function useStatuses(itemType?: WorkItemType) {
  return useQuery({
    queryKey: statusKeys.byType(itemType),
    queryFn: () => getStatuses(itemType),
  });
}
