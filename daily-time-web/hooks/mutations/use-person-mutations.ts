"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPerson, deletePerson, updatePerson } from "@/lib/api/people";
import { personKeys, noteKeys, taskItemKeys } from "@/lib/query/keys";
import type { PersonInput } from "@/schemas/person.schema";

export function usePersonMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: personKeys.all });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: PersonInput) => createPerson(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: PersonInput }) =>
      updatePerson(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deletePerson(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
