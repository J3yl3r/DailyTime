import { apiClient } from "@/lib/api/client";
import type { Project } from "@/types/api";
import type { ProjectInput } from "@/schemas/project.schema";

export function getProjects(onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<Project[]>(`/api/projects${query}`);
}

export function createProject(body: ProjectInput) {
  return apiClient.post<Project>("/api/projects", body);
}

export function updateProject(id: number, body: ProjectInput) {
  return apiClient.put<Project>(`/api/projects/${id}`, body);
}

export function deleteProject(id: number) {
  return apiClient.delete<object>(`/api/projects/${id}`);
}
