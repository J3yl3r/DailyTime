import { apiClient } from "@/lib/api/client";
import type {
  CareerCatalog,
  CareerCatalogKind,
  CareerProfile,
  CareerProfileInput,
  JobApplication,
  WorkExperience,
} from "@/types/api";
import type {
  CareerCatalogInput,
  JobApplicationInput,
  WorkExperienceInput,
} from "@/schemas/career.schema";

function optionalId(value: number | "" | null | undefined): number | null {
  if (value === "" || value == null) return null;
  return Number(value);
}

export function getCareerCatalog(kind: CareerCatalogKind, onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<CareerCatalog[]>(`/api/career/${kind}${query}`);
}

export function createCareerCatalog(kind: CareerCatalogKind, body: CareerCatalogInput) {
  return apiClient.post<CareerCatalog>(`/api/career/${kind}`, {
    name: body.name,
    description: body.description || null,
    color: body.color || null,
    sortOrder: body.sortOrder ?? null,
    isActive: body.isActive,
  });
}

export function updateCareerCatalog(
  kind: CareerCatalogKind,
  id: number,
  body: CareerCatalogInput,
) {
  return apiClient.put<CareerCatalog>(`/api/career/${kind}/${id}`, {
    name: body.name,
    description: body.description || null,
    color: body.color || null,
    sortOrder: body.sortOrder ?? null,
    isActive: body.isActive,
  });
}

export function deleteCareerCatalog(kind: CareerCatalogKind, id: number) {
  return apiClient.delete<object>(`/api/career/${kind}/${id}`);
}

export function getWorkExperiences() {
  return apiClient.get<WorkExperience[]>("/api/work-experiences");
}

export function createWorkExperience(body: WorkExperienceInput) {
  return apiClient.post<WorkExperience>("/api/work-experiences", {
    companyId: body.companyId,
    positionId: body.positionId,
    locationId: optionalId(body.locationId),
    fieldId: optionalId(body.fieldId),
    startDate: body.startDate,
    endDate: body.isCurrent ? null : body.endDate || null,
    isCurrent: body.isCurrent,
    summary: body.summary || null,
    achievements: body.achievements || null,
    technologyIds: body.technologyIds ?? [],
  });
}

export function updateWorkExperience(id: number, body: WorkExperienceInput) {
  return apiClient.put<WorkExperience>(`/api/work-experiences/${id}`, {
    companyId: body.companyId,
    positionId: body.positionId,
    locationId: optionalId(body.locationId),
    fieldId: optionalId(body.fieldId),
    startDate: body.startDate,
    endDate: body.isCurrent ? null : body.endDate || null,
    isCurrent: body.isCurrent,
    summary: body.summary || null,
    achievements: body.achievements || null,
    technologyIds: body.technologyIds ?? [],
  });
}

export function deleteWorkExperience(id: number) {
  return apiClient.delete<object>(`/api/work-experiences/${id}`);
}

export function getJobApplications() {
  return apiClient.get<JobApplication[]>("/api/job-applications");
}

export function createJobApplication(body: JobApplicationInput) {
  return apiClient.post<JobApplication>("/api/job-applications", {
    companyId: body.companyId,
    positionId: body.positionId,
    locationId: optionalId(body.locationId),
    fieldId: optionalId(body.fieldId),
    statusId: body.statusId,
    appliedAt: body.appliedAt,
    url: body.url || null,
    contact: body.contact || null,
    notes: body.notes || null,
    workExperienceId: optionalId(body.workExperienceId),
  });
}

export function updateJobApplication(id: number, body: JobApplicationInput) {
  return apiClient.put<JobApplication>(`/api/job-applications/${id}`, {
    companyId: body.companyId,
    positionId: body.positionId,
    locationId: optionalId(body.locationId),
    fieldId: optionalId(body.fieldId),
    statusId: body.statusId,
    appliedAt: body.appliedAt,
    url: body.url || null,
    contact: body.contact || null,
    notes: body.notes || null,
    workExperienceId: optionalId(body.workExperienceId),
  });
}

export function deleteJobApplication(id: number) {
  return apiClient.delete<object>(`/api/job-applications/${id}`);
}

export function bulkDeleteJobApplications(ids: number[]) {
  return apiClient.post<{ affected: number }>("/api/job-applications/bulk/delete", {
    ids,
  });
}

export type FitScoreResult = {
  score: number;
  verdict: "yes" | "maybe" | "no" | string;
  verdictLabel: string;
  reasons: string[];
  recommendations: string[];
  suggestedCv: string | null;
  profileExperienceLabels: string[];
  experiencesUsed: number;
  workExperienceId: number | null;
  workExperienceLabel: string | null;
  jobOfferId: number | null;
  jobOfferTitle: string | null;
  offerFound: boolean;
};

export function getCareerProfile() {
  return apiClient.get<CareerProfile>("/api/career-profile");
}

export function upsertCareerProfile(body: CareerProfileInput) {
  return apiClient.put<CareerProfile>("/api/career-profile", body);
}

export function evaluateFit(body: {
  jobApplicationId?: number | null;
  workExperienceId?: number | null;
  jobOfferId?: number | null;
  offerUrl?: string | null;
}) {
  return apiClient.post<FitScoreResult>("/api/fit-score/evaluate", {
    jobApplicationId: body.jobApplicationId ?? null,
    // Por defecto el backend usa TODAS las experiencias (perfil completo).
    workExperienceId: body.workExperienceId ?? null,
    jobOfferId: body.jobOfferId ?? null,
    offerUrl: body.offerUrl ?? null,
  });
}
