"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createProject, deleteProject, updateProject } from "@/lib/api/projects";
import { projectKeys, noteKeys, taskItemKeys } from "@/lib/query/keys";
import type { ProjectInput } from "@/schemas/project.schema";

export function useProjectMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: projectKeys.all });
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const create = useMutation({
    mutationFn: (body: ProjectInput) => createProject(body),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: ProjectInput }) =>
      updateProject(id, body),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (id: number) => deleteProject(id),
    onSuccess: invalidate,
  });
  return { create, update, remove };
}
