"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCompany, deleteCompany, updateCompany } from "@/lib/api/companies";
import { companyKeys, noteKeys, taskItemKeys } from "@/lib/query/keys";
import type { CompanyInput } from "@/schemas/company.schema";

export function useCompanyMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: companyKeys.all });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CompanyInput) => createCompany(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: CompanyInput }) =>
      updateCompany(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteCompany(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
