"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createVaultPassword,
  deleteVaultPassword,
  updateVaultPassword,
} from "@/lib/api/vault";
import { vaultAccountKeys, vaultPasswordKeys } from "@/lib/query/keys";
import type { VaultPasswordInput } from "@/schemas/vault.schema";

export function useVaultPasswordMutations(accountId: number | null) {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: vaultAccountKeys.all });
    if (accountId != null) {
      queryClient.invalidateQueries({
        queryKey: vaultPasswordKeys.byAccount(accountId),
      });
    } else {
      queryClient.invalidateQueries({ queryKey: vaultPasswordKeys.all });
    }
  };

  const create = useMutation({
    mutationFn: (body: VaultPasswordInput) => {
      if (accountId == null) throw new Error("Selecciona una cuenta primero.");
      return createVaultPassword(accountId, body);
    },
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: VaultPasswordInput }) =>
      updateVaultPassword(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteVaultPassword(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
