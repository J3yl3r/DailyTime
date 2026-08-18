"use client";

import { useQuery } from "@tanstack/react-query";
import { getPeople } from "@/lib/api/people";
import { personKeys } from "@/lib/query/keys";

export function usePeople(onlyActive?: boolean) {
  return useQuery({
    queryKey: personKeys.list(onlyActive),
    queryFn: () => getPeople(onlyActive),
  });
}
