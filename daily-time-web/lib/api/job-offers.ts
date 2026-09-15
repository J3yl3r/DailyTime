import { apiClient } from "@/lib/api/client";
import type {
  JobOffer,
  JobOfferFilters,
  JobOfferMeta,
  OfferAiStatus,
  OfferTriageSettings,
  RescoreJobOffersResult,
  UpdateOfferTriageResult,
} from "@/types/api";

function buildQuery(filters?: JobOfferFilters) {
  const params = new URLSearchParams();
  if (!filters) return "";

  if (filters.portalIds?.length) {
    params.set("portalIdsCsv", filters.portalIds.join(","));
  } else if (filters.portalId != null) {
    params.set("portalId", String(filters.portalId));
  }

  if (filters.statuses?.length) {
    params.set("statuses", filters.statuses.join(","));
  } else if (filters.status) {
    params.set("status", filters.status);
  }

  if (filters.search) params.set("search", filters.search);

  if (filters.countries?.length) {
    params.set("countries", filters.countries.join(","));
  } else if (filters.country) {
    params.set("country", filters.country);
  }

  if (filters.languages?.length) {
    params.set("languages", filters.languages.join(","));
  } else if (filters.language) {
    params.set("language", filters.language);
  }

  if (filters.workModalities?.length) {
    params.set("workModalities", filters.workModalities.join(","));
  } else if (filters.workModality) {
    params.set("workModality", filters.workModality);
  }

  if (filters.contractTypes?.length) {
    params.set("contractTypes", filters.contractTypes.join(","));
  } else if (filters.contractType) {
    params.set("contractType", filters.contractType);
  }

  if (filters.techStacks?.length) {
    params.set("techStacks", filters.techStacks.join(","));
  } else if (filters.techStack) {
    params.set("techStack", filters.techStack);
  }

  if (filters.capturedFrom) params.set("capturedFrom", filters.capturedFrom);
  if (filters.capturedTo) params.set("capturedTo", filters.capturedTo);
  if (filters.postedFrom) params.set("postedFrom", filters.postedFrom);
  if (filters.postedTo) params.set("postedTo", filters.postedTo);
  if (filters.tiers?.length) params.set("tiers", filters.tiers.join(","));
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

/** Fija las ofertas indicadas en ese orden, por encima del puntaje. */
export function reorderJobOffers(ids: number[]) {
  return apiClient.put<object>("/api/job-offers/reorder", { ids });
}

export function setJobOfferPinned(id: number, pinned: boolean) {
  return apiClient.put<JobOffer>(`/api/job-offers/${id}/pin`, { pinned });
}

export function getOfferTriageSettings() {
  return apiClient.get<OfferTriageSettings>("/api/job-offers/triage/settings");
}

/** Guarda las reglas y recalcula todas las ofertas. */
export function updateOfferTriageSettings(settings: OfferTriageSettings) {
  return apiClient.put<UpdateOfferTriageResult>("/api/job-offers/triage/settings", settings);
}

/** Vista previa del recálculo con estas reglas, sin guardar nada. */
export function previewOfferTriage(settings: OfferTriageSettings) {
  return apiClient.post<RescoreJobOffersResult>("/api/job-offers/triage/preview", settings);
}

/** Recalcula todas las ofertas con las reglas guardadas (p. ej. tras cambiar el perfil). */
export function rescoreJobOffers() {
  return apiClient.post<RescoreJobOffersResult>("/api/job-offers/triage/rescore", {});
}

export function getOfferAiStatus() {
  return apiClient.get<OfferAiStatus>("/api/job-offers/ai/status");
}

/** Pide analizar ya las ofertas A/B pendientes (corre en segundo plano en la API). */
export function requestPendingAiAnalysis() {
  return apiClient.post<OfferAiStatus>("/api/job-offers/ai/analyze-pending", {});
}

/** Analiza o reanaliza una oferta y espera el resultado. */
export function analyzeJobOffer(id: number) {
  return apiClient.post<JobOffer>(`/api/job-offers/ai/${id}/analyze`, {});
}

