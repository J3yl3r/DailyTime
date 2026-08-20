import { apiClient } from "@/lib/api/client";
import type { JobPortal, JobPortalScrapeLog } from "@/types/api";
import type { JobPortalInput } from "@/schemas/job-portal.schema";

export function getJobPortals(onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<JobPortal[]>(`/api/job-portals${query}`);
}

export function getJobPortal(id: number) {
  return apiClient.get<JobPortal>(`/api/job-portals/${id}`);
}

export function createJobPortal(body: JobPortalInput) {
  return apiClient.post<JobPortal>("/api/job-portals", {
    name: body.name,
    url: body.url,
    loginUrl: body.loginUrl || null,
    notes: body.notes || null,
    scrapeConfig: body.scrapeConfig || null,
    isActive: body.isActive,
  });
}

export function updateJobPortal(id: number, body: JobPortalInput) {
  return apiClient.put<JobPortal>(`/api/job-portals/${id}`, {
    name: body.name,
    url: body.url,
    loginUrl: body.loginUrl || null,
    notes: body.notes || null,
    scrapeConfig: body.scrapeConfig || null,
    isActive: body.isActive,
  });
}

export function deleteJobPortal(id: number) {
  return apiClient.delete<object>(`/api/job-portals/${id}`);
}

export function queueJobPortalScrape(id: number) {
  return apiClient.post<JobPortal>(`/api/job-portals/${id}/scrape`, {});
}

export function getJobPortalScrapeLogs(id: number, take = 30) {
  return apiClient.get<JobPortalScrapeLog[]>(
    `/api/job-portals/${id}/scrape-logs?take=${take}`,
  );
}
