"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createVaultService,
  deleteVaultService,
  updateVaultService,
} from "@/lib/api/vault";
import { vaultPasswordKeys, vaultServiceKeys } from "@/lib/query/keys";
import type { VaultServiceInput } from "@/schemas/vault.schema";

export function useVaultServiceMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: vaultServiceKeys.all });
    queryClient.invalidateQueries({ queryKey: vaultPasswordKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: VaultServiceInput) => createVaultService(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: VaultServiceInput }) =>
      updateVaultService(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteVaultService(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
