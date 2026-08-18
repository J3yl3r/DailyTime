"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createVaultAccount,
  deleteVaultAccount,
  updateVaultAccount,
} from "@/lib/api/vault";
import { vaultAccountKeys, vaultPasswordKeys } from "@/lib/query/keys";
import type { VaultAccountInput } from "@/schemas/vault.schema";

export function useVaultAccountMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: vaultAccountKeys.all });
    queryClient.invalidateQueries({ queryKey: vaultPasswordKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: VaultAccountInput) => createVaultAccount(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: VaultAccountInput }) =>
      updateVaultAccount(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteVaultAccount(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
