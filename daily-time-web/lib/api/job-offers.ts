import { apiClient } from "@/lib/api/client";
import type { JobOffer, JobOfferFilters, JobOfferMeta } from "@/types/api";

function buildQuery(filters?: JobOfferFilters) {
  const params = new URLSearchParams();
  if (!filters) return "";
  if (filters.portalId != null) params.set("portalId", String(filters.portalId));
  if (filters.status) params.set("status", filters.status);
  if (filters.search) params.set("search", filters.search);
  if (filters.country) params.set("country", filters.country);
  if (filters.language) params.set("language", filters.language);
  if (filters.workModality) params.set("workModality", filters.workModality);
  if (filters.contractType) params.set("contractType", filters.contractType);
  if (filters.techStack) params.set("techStack", filters.techStack);
  if (filters.capturedFrom) params.set("capturedFrom", filters.capturedFrom);
  if (filters.capturedTo) params.set("capturedTo", filters.capturedTo);
  if (filters.postedFrom) params.set("postedFrom", filters.postedFrom);
  if (filters.postedTo) params.set("postedTo", filters.postedTo);
  const query = params.toString();
  return query ? `?${query}` : "";
}

export function getJobOffers(filters?: JobOfferFilters) {
  return apiClient.get<JobOffer[]>(`/api/job-offers${buildQuery(filters)}`);
}

export function getJobOfferMeta() {
  return apiClient.get<JobOfferMeta>("/api/job-offers/meta");
}

export function updateJobOfferStatus(id: number, status: string) {
  return apiClient.put<JobOffer>(`/api/job-offers/${id}/status`, { status });
}

export function bulkUpdateJobOfferStatus(ids: number[], status: string) {
  return apiClient.post<{ affected: number }>("/api/job-offers/bulk/status", {
    ids,
    status,
  });
}

export function deleteJobOffer(id: number) {
  return apiClient.delete<object>(`/api/job-offers/${id}`);
}

export function bulkDeleteJobOffers(ids: number[]) {
  return apiClient.post<{ affected: number }>("/api/job-offers/bulk/delete", {
    ids,
  });
}
