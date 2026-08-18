"use client";

import { useQuery } from "@tanstack/react-query";
import { getVaultAccounts } from "@/lib/api/vault";
import { vaultAccountKeys } from "@/lib/query/keys";

export function useVaultAccounts() {
  return useQuery({
    queryKey: vaultAccountKeys.list(),
    queryFn: getVaultAccounts,
  });
}
