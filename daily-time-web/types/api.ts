export type ApiResponse<T> = {
  success: boolean;
  data: T | null;
  message?: string | null;
  errors?: string[] | null;
};

export class ApiClientError extends Error {
  constructor(
    message: string,
    public status: number,
    public errors?: string[] | null
  ) {
    super(message);
    this.name = "ApiClientError";
  }
}

export type WorkItemType = "task" | "note";

export type WorkItemStatusSummary = {
  id: number;
  name: string;
  description: string;
  color: string;
  isFinal: boolean;
  itemType: WorkItemType | string;
};

export type WorkItemStatus = {
  id: number;
  name: string;
  description: string;
  color: string;
  isFinal: boolean;
  itemType: WorkItemType | string;
  createdAt: string;
};

export type WorkItemCategorySummary = {
  id: number;
  name: string;
  description: string;
  itemType: WorkItemType | string;
  isActive: boolean;
};

export type WorkItemCategory = WorkItemCategorySummary & {
  createdAt: string;
};

export type PersonSummary = {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
};

export type Person = PersonSummary & {
  createdAt: string;
};

export type ProjectSummary = {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
};

export type Project = ProjectSummary & {
  createdAt: string;
};

export type CompanySummary = {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
};

export type Company = CompanySummary & {
  createdAt: string;
};

export type TaskItem = {
  id: number;
  parentTaskId: number | null;
  title: string;
  content: string | null;
  isCompleted: boolean;
  completedAt: string | null;
  workDate: string;
  startTime: string | null;
  endTime: string | null;
  durationMinutes: number;
  sortOrder: number;
  statusId: number;
  status: WorkItemStatusSummary | null;
  categoryId: number;
  category: WorkItemCategorySummary | null;
  personId: number | null;
  person: PersonSummary | null;
  projectId: number | null;
  project: ProjectSummary | null;
  companyId: number | null;
  company: CompanySummary | null;
  createdAt: string;
  updatedAt: string;
};

export type Note = {
  id: number;
  parentNoteId: number | null;
  title: string | null;
  content: string;
  workDate: string | null;
  startTime: string | null;
  endTime: string | null;
  durationMinutes: number;
  sortOrder: number;
  statusId: number;
  status: WorkItemStatusSummary | null;
  categoryId: number;
  category: WorkItemCategorySummary | null;
  personId: number | null;
  person: PersonSummary | null;
  projectId: number | null;
  project: ProjectSummary | null;
  companyId: number | null;
  company: CompanySummary | null;
  createdAt: string;
  updatedAt: string;
};

export type TimeEntry = {
  id: number;
  taskItemId: number | null;
  noteId: number | null;
  workDate: string;
  durationMinutes: number;
  description: string | null;
  createdAt: string;
};

export type VaultAccount = {
  id: number;
  name: string;
  description: string | null;
  passwordCount: number;
  createdAt: string;
  updatedAt: string;
};

export type VaultService = {
  id: number;
  name: string;
  url: string | null;
  notes: string | null;
  isActive: boolean;
  passwordCount: number;
  createdAt: string;
};

export type VaultPassword = {
  id: number;
  accountId: number;
  serviceId: number;
  serviceName: string;
  username: string;
  password: string;
  url: string | null;
  notes: string | null;
  tags: string | null;
  createdAt: string;
  updatedAt: string;
};

export type WorkExperience = {
  id: number;
  companyId: number;
  companyName: string;
  positionId: number;
  positionName: string;
  locationId: number | null;
  locationName: string | null;
  fieldId: number | null;
  fieldName: string | null;
  startDate: string;
  endDate: string | null;
  isCurrent: boolean;
  summary: string | null;
  achievements: string | null;
  technologyIds: number[];
  technologyNames: string[];
  createdAt: string;
  updatedAt: string;
};

export type JobApplication = {
  id: number;
  companyId: number;
  companyName: string;
  positionId: number;
  positionName: string;
  locationId: number | null;
  locationName: string | null;
  fieldId: number | null;
  fieldName: string | null;
  statusId: number;
  statusName: string;
  statusColor: string | null;
  appliedAt: string;
  url: string | null;
  contact: string | null;
  notes: string | null;
  workExperienceId: number | null;
  workExperienceLabel: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CareerCatalog = {
  id: number;
  name: string;
  description: string | null;
  color?: string | null;
  sortOrder?: number | null;
  isActive: boolean;
  createdAt: string;
};

export type CareerCatalogKind =
  | "companies"
  | "positions"
  | "locations"
  | "fields"
  | "technologies"
  | "application-statuses";

export type JobPortal = {
  id: number;
  name: string;
  url: string;
  loginUrl: string | null;
  notes: string | null;
  scrapeConfig: string | null;
  isActive: boolean;
  lastRunAt: string | null;
  lastRunStatus: string | null;
  createdAt: string;
  updatedAt: string;
};

export type JobPortalScrapeLog = {
  id: number;
  jobPortalId: number;
  portalName: string;
  startedAt: string;
  finishedAt: string | null;
  status: string;
  message: string | null;
  offerCount: number;
  savedInserted: number;
  savedUpdated: number;
};

export type JobOfferStatus = "new" | "seen" | "discarded" | "applied";

export type JobOffer = {
  id: number;
  jobPortalId: number;
  portalName: string;
  title: string;
  company: string | null;
  location: string | null;
  url: string | null;
  externalKey: string;
  descriptionSnippet: string | null;
  description: string | null;
  country: string | null;
  language: string | null;
  workModality: string | null;
  contractType: string | null;
  techStack: string | null;
  postedAt: string | null;
  status: JobOfferStatus | string;
  capturedAt: string;
  updatedAt: string;
};

export type JobOfferFilters = {
  portalId?: number;
  status?: string;
  search?: string;
  country?: string;
  language?: string;
  workModality?: string;
  contractType?: string;
  techStack?: string;
  capturedFrom?: string;
  capturedTo?: string;
  postedFrom?: string;
  postedTo?: string;
};

export type JobOfferMeta = {
  countries: string[];
  languages: string[];
  workModalities: string[];
  contractTypes: string[];
  techStacks: string[];
};

export type CareerProfileLink = {
  id: number;
  label: string;
  url: string;
};

export type CareerProfileLanguage = {
  id: number;
  name: string;
  level: string | null;
};

export type CareerProfileEducation = {
  id: number;
  title: string;
  place: string | null;
  year: string | null;
};

export type CareerProfileCertification = {
  id: number;
  title: string;
  issuer: string | null;
  year: string | null;
};

export type CareerCoverLetter = {
  id: number;
  name: string;
  language: string;
  stack: string | null;
  body: string;
  isActive: boolean;
};

export type CareerProfileSalary = {
  min: number | null;
  max: number | null;
  currency: string | null;
  period: string | null;
  notes: string | null;
};

export type CareerProfile = {
  id: number;
  fullName: string;
  headline: string | null;
  location: string | null;
  timezone: string | null;
  availability: string | null;
  preferredModality: string | null;
  email: string | null;
  phone: string | null;
  links: CareerProfileLink[];
  languages: CareerProfileLanguage[];
  salary: CareerProfileSalary;
  preferredCountries: string[];
  preferredStacks: string[];
  summary: string | null;
  strengths: string[];
  education: CareerProfileEducation[];
  certifications: CareerProfileCertification[];
  coverLetters: CareerCoverLetter[];
  createdAt: string | null;
  updatedAt: string | null;
};

export type CareerProfileInput = {
  fullName: string;
  headline: string | null;
  location: string | null;
  timezone: string | null;
  availability: string | null;
  preferredModality: string | null;
  email: string | null;
  phone: string | null;
  salary: CareerProfileSalary;
  summary: string | null;
  links: { label: string; url: string }[];
  languages: { name: string; level: string | null }[];
  preferredCountries: string[];
  preferredStacks: string[];
  strengths: string[];
  education: { title: string; place: string | null; year: string | null }[];
  certifications: { title: string; issuer: string | null; year: string | null }[];
  coverLetters: {
    name: string;
    language: string;
    stack: string | null;
    body: string;
    isActive: boolean;
  }[];
};
