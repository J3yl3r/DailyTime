"use client";

import { useQuery } from "@tanstack/react-query";
import { getCompanies } from "@/lib/api/companies";
import { companyKeys } from "@/lib/query/keys";

export function useCompanies(onlyActive?: boolean) {
  return useQuery({
    queryKey: companyKeys.list(onlyActive),
    queryFn: () => getCompanies(onlyActive),
  });
}
