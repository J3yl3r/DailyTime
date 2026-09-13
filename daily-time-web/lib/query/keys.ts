import type { JobOfferFilters } from "@/types/api";

export const taskItemKeys = {
  all: ["task-items"] as const,
  byDate: (fromDate: string, toDate = fromDate) =>
    [...taskItemKeys.all, "date-range", fromDate, toDate] as const,
  detail: (id: number) => [...taskItemKeys.all, "detail", id] as const,
  children: (id: number) => [...taskItemKeys.all, "children", id] as const,
};

export const noteKeys = {
  all: ["notes"] as const,
  byDate: (fromDate?: string, toDate = fromDate) =>
    [...noteKeys.all, "date-range", fromDate, toDate] as const,
  detail: (id: number) => [...noteKeys.all, "detail", id] as const,
  children: (id: number) => [...noteKeys.all, "children", id] as const,
};

export const timeEntryKeys = {
  all: ["time-entries"] as const,
  byDate: (fromDate: string, toDate = fromDate) =>
    [...timeEntryKeys.all, "date-range", fromDate, toDate] as const,
  byTask: (taskItemId: number) => [...timeEntryKeys.all, "task", taskItemId] as const,
  byNote: (noteId: number) => [...timeEntryKeys.all, "note", noteId] as const,
  detail: (id: number) => [...timeEntryKeys.all, "detail", id] as const,
};

export const statusKeys = {
  all: ["statuses"] as const,
  byType: (itemType?: string) =>
    [...statusKeys.all, "type", itemType ?? "all"] as const,
  detail: (id: number) => [...statusKeys.all, "detail", id] as const,
};

export const categoryKeys = {
  all: ["categories"] as const,
  byType: (itemType?: string) =>
    [...categoryKeys.all, "type", itemType ?? "all"] as const,
  detail: (id: number) => [...categoryKeys.all, "detail", id] as const,
};

export const personKeys = {
  all: ["people"] as const,
  list: (onlyActive?: boolean) =>
    [...personKeys.all, "list", onlyActive ?? false] as const,
  detail: (id: number) => [...personKeys.all, "detail", id] as const,
};

export const projectKeys = {
  all: ["projects"] as const,
  list: (onlyActive?: boolean) =>
    [...projectKeys.all, "list", onlyActive ?? false] as const,
  detail: (id: number) => [...projectKeys.all, "detail", id] as const,
};

export const companyKeys = {
  all: ["companies"] as const,
  list: (onlyActive?: boolean) =>
    [...companyKeys.all, "list", onlyActive ?? false] as const,
  detail: (id: number) => [...companyKeys.all, "detail", id] as const,
};

export const vaultAccountKeys = {
  all: ["vault-accounts"] as const,
  list: () => [...vaultAccountKeys.all, "list"] as const,
  detail: (id: number) => [...vaultAccountKeys.all, "detail", id] as const,
};

export const vaultPasswordKeys = {
  all: ["vault-passwords"] as const,
  byAccount: (accountId: number) =>
    [...vaultPasswordKeys.all, "account", accountId] as const,
  detail: (id: number) => [...vaultPasswordKeys.all, "detail", id] as const,
};

export const vaultServiceKeys = {
  all: ["vault-services"] as const,
  list: (onlyActive?: boolean) =>
    [...vaultServiceKeys.all, "list", onlyActive ?? false] as const,
  detail: (id: number) => [...vaultServiceKeys.all, "detail", id] as const,
};

export const workExperienceKeys = {
  all: ["work-experiences"] as const,
  list: () => [...workExperienceKeys.all, "list"] as const,
  detail: (id: number) => [...workExperienceKeys.all, "detail", id] as const,
};

export const jobApplicationKeys = {
  all: ["job-applications"] as const,
  list: () => [...jobApplicationKeys.all, "list"] as const,
  detail: (id: number) => [...jobApplicationKeys.all, "detail", id] as const,
};

export const careerCatalogKeys = {
  all: ["career-catalogs"] as const,
  list: (kind: string, onlyActive?: boolean) =>
    [...careerCatalogKeys.all, kind, "list", onlyActive ?? false] as const,
};

export const jobPortalKeys = {
  all: ["job-portals"] as const,
  list: (onlyActive?: boolean) =>
    [...jobPortalKeys.all, "list", onlyActive ?? false] as const,
  detail: (id: number) => [...jobPortalKeys.all, "detail", id] as const,
};

export const scrapeScheduleKeys = {
  all: ["scrape-schedule"] as const,
  current: () => [...scrapeScheduleKeys.all, "current"] as const,
};

export const careerProfileKeys = {
  all: ["career-profile"] as const,
  current: () => [...careerProfileKeys.all, "current"] as const,
};

export const jobOfferKeys = {
  all: ["job-offers"] as const,
  list: (filters?: JobOfferFilters | null) =>
    [...jobOfferKeys.all, "list", filters ?? {}] as const,
  meta: () => [...jobOfferKeys.all, "meta"] as const,
  detail: (id: number) => [...jobOfferKeys.all, "detail", id] as const,
};