"use client";

import { useQuery } from "@tanstack/react-query";
import { getVaultPasswords } from "@/lib/api/vault";
import { vaultPasswordKeys } from "@/lib/query/keys";

export function useVaultPasswords(accountId: number | null) {
  return useQuery({
    queryKey: vaultPasswordKeys.byAccount(accountId ?? 0),
    queryFn: () => getVaultPasswords(accountId!),
    enabled: accountId != null && accountId > 0,
  });
}
