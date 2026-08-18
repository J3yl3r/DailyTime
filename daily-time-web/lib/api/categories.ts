import { apiClient } from "@/lib/api/client";
import type { WorkItemCategory, WorkItemType } from "@/types/api";
import type { CategoryInput } from "@/schemas/category.schema";

export function getCategories(itemType?: WorkItemType) {
  const query = itemType ? `?itemType=${itemType}` : "";
  return apiClient.get<WorkItemCategory[]>(`/api/categories${query}`);
}

export function createCategory(body: CategoryInput) {
  return apiClient.post<WorkItemCategory>("/api/categories", body);
}

export function updateCategory(id: number, body: CategoryInput) {
  return apiClient.put<WorkItemCategory>(`/api/categories/${id}`, body);
}

export function deleteCategory(id: number) {
  return apiClient.delete<object>(`/api/categories/${id}`);
}
