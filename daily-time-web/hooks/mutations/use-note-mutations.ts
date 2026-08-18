"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createNote, updateNote, deleteNote } from "@/lib/api/notes";
import { noteKeys } from "@/lib/query/keys";
import type { CreateNoteInput, UpdateNoteInput } from "@/schemas/note.schema";

export function useNoteMutations(workDate?: string) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: noteKeys.byDate(workDate) });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: CreateNoteInput) => createNote(body),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateNoteInput }) =>
      updateNote(id, body),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (id: number) => deleteNote(id),
    onSuccess: invalidate,
  });

  return { create, update, remove };
}