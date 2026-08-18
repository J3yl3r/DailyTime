"use client";

import { useQuery } from "@tanstack/react-query";
import { getProjects } from "@/lib/api/projects";
import { projectKeys } from "@/lib/query/keys";

export function useProjects(onlyActive?: boolean) {
  return useQuery({
    queryKey: projectKeys.list(onlyActive),
    queryFn: () => getProjects(onlyActive),
  });
}
