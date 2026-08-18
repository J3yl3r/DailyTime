import { apiClient } from "@/lib/api/client";
import type { Company } from "@/types/api";
import type { CompanyInput } from "@/schemas/company.schema";

export function getCompanies(onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<Company[]>(`/api/companies${query}`);
}

export function createCompany(body: CompanyInput) {
  return apiClient.post<Company>("/api/companies", body);
}

export function updateCompany(id: number, body: CompanyInput) {
  return apiClient.put<Company>(`/api/companies/${id}`, body);
}

export function deleteCompany(id: number) {
  return apiClient.delete<object>(`/api/companies/${id}`);
}
