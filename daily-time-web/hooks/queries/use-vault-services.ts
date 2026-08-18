"use client";

import { useQuery } from "@tanstack/react-query";
import { getVaultServices } from "@/lib/api/vault";
import { vaultServiceKeys } from "@/lib/query/keys";

export function useVaultServices(onlyActive?: boolean) {
  return useQuery({
    queryKey: vaultServiceKeys.list(onlyActive),
    queryFn: () => getVaultServices(onlyActive),
  });
}
