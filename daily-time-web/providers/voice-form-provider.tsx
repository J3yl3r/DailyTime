"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { FormModal } from "@/components/shared/form-modal";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { VoiceEntityForm } from "@/components/features/voice/VoiceEntityForm";
import {
  VoiceCareerForm,
  type CareerVoiceValues,
} from "@/components/features/voice/VoiceCareerForm";
import { useStatuses } from "@/hooks/queries/use-statuses";
import { useCategories } from "@/hooks/queries/use-categories";
import { usePeople } from "@/hooks/queries/use-people";
import { useProjects } from "@/hooks/queries/use-projects";
import { useVaultAccounts } from "@/hooks/queries/use-vault-accounts";
import {
  parseVoiceFormCommand,
  type VoiceFormDraft,
  type VoiceFormFieldPatch,
  type VoiceFormKind,
} from "@/lib/voice/form-commands";
import type { CreateTaskItemFormValues } from "@/schemas/task-item.schema";
import type { CreateNoteFormValues } from "@/schemas/note.schema";
import type { CareerCatalogKind } from "@/types/api";

type CatalogKind = Exclude<
  VoiceFormKind,
  "task" | "note" | "work_experience" | "job_application"
>;

type EntityInitial = {
  name?: string;
  description?: string;
  color?: string;
  isFinal?: boolean;
  isActive?: boolean;
  itemType?: "task" | "note";
  serviceName?: string;
  username?: string;
  password?: string;
  url?: string;
  notes?: string;
  tags?: string;
};

type VoiceFormContextValue = {
  isOpen: boolean;
  kind: VoiceFormKind | null;
  openTaskForm: (draft?: VoiceFormDraft) => void;
  openNoteForm: (draft?: VoiceFormDraft) => void;
  openPersonForm: (draft?: VoiceFormDraft) => void;
  openProjectForm: (draft?: VoiceFormDraft) => void;
  openStatusForm: (draft?: VoiceFormDraft) => void;
  openCategoryForm: (draft?: VoiceFormDraft) => void;
  openVaultAccountForm: (draft?: VoiceFormDraft) => void;
  openVaultPasswordForm: (draft?: VoiceFormDraft) => void;
  openCareerCatalogForm: (draft?: VoiceFormDraft) => void;
  openVaultServiceForm: (draft?: VoiceFormDraft) => void;
  openWorkExperienceForm: (draft?: VoiceFormDraft) => void;
  openJobApplicationForm: (draft?: VoiceFormDraft) => void;
  closeForm: () => void;
  handleVoiceText: (text: string) => { handled: boolean; message: string };
};

const VoiceFormContext = createContext<VoiceFormContextValue | null>(null);

function todayIso(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function resolveDraftIds(
  draft: VoiceFormDraft | undefined,
  statuses: { id: number; name: string }[],
  categories: { id: number; name: string; isActive?: boolean }[],
  people: { id: number; name: string; isActive?: boolean }[],
  projects: { id: number; name: string; isActive?: boolean }[],
): VoiceFormDraft {
  if (!draft) return {};
  const next = { ...draft };

  if (draft.statusName && next.statusId == null) {
    const match = statuses.find(
      (item) => item.name.toLowerCase() === draft.statusName!.toLowerCase(),
    );
    if (match) next.statusId = match.id;
  }

  if (draft.categoryName && next.categoryId == null) {
    const match = categories.find(
      (item) => item.name.toLowerCase() === draft.categoryName!.toLowerCase(),
    );
    if (match) next.categoryId = match.id;
  }

  if (draft.personName && next.personId == null) {
    const match = people.find(
      (item) => item.name.toLowerCase() === draft.personName!.toLowerCase(),
    );
    if (match) next.personId = match.id;
  }

  if (draft.projectName && next.projectId == null) {
    const match = projects.find(
      (item) => item.name.toLowerCase() === draft.projectName!.toLowerCase(),
    );
    if (match) next.projectId = match.id;
  }

  return next;
}

const TITLES: Record<VoiceFormKind, string> = {
  task: "Nueva tarea (voz)",
  note: "Nueva nota (voz)",
  person: "Nueva persona (voz)",
  project: "Nuevo proyecto (voz)",
  status: "Nuevo estado (voz)",
  category: "Nueva categoría (voz)",
  vault_account: "Nueva cuenta de bóveda (voz)",
  vault_password: "Nueva contraseña (voz)",
  career_catalog: "Nuevo dato reutilizable (voz)",
  vault_service: "Nuevo servicio (voz)",
  work_experience: "Nueva experiencia (voz)",
  job_application: "Nueva postulación (voz)",
};

const EDIT_TITLES: Record<VoiceFormKind, string> = {
  task: "Editar tarea (voz)",
  note: "Editar nota (voz)",
  person: "Editar persona (voz)",
  project: "Editar proyecto (voz)",
  status: "Editar estado (voz)",
  category: "Editar categoría (voz)",
  vault_account: "Editar cuenta de bóveda (voz)",
  vault_password: "Editar contraseña (voz)",
  career_catalog: "Editar dato reutilizable (voz)",
  vault_service: "Editar servicio (voz)",
  work_experience: "Editar experiencia (voz)",
  job_application: "Editar postulación (voz)",
};

const DESCRIPTIONS: Record<VoiceFormKind, string> = {
  task: "Dicta «título …», «persona …», «proyecto …» o «guardar».",
  note: "Dicta «contenido …», «persona …», «proyecto …» o «guardar».",
  person: "Dicta «nombre …», «descripción …» o «guardar».",
  project: "Dicta «nombre …», «descripción …» o «guardar».",
  status: "Dicta «nombre …», «tipo tarea|nota», «color …» o «guardar».",
  category: "Dicta «nombre …», «tipo tarea|nota» o «guardar».",
  vault_account: "Dicta «nombre …», «descripción …» o «guardar».",
  vault_password: "Dicta «servicio …», «usuario …», «contraseña …» o «guardar».",
  career_catalog: "Dicta «nombre …», «descripción …» o «guardar».",
  vault_service: "Dicta «nombre …», «url …», «notas …» o «guardar».",
  work_experience: "Dicta «empresa …», «cargo …», «inicio …» o «guardar».",
  job_application: "Dicta «empresa …», «cargo …», «estado …» o «guardar».",
};

function draftToCareerInitial(draft?: VoiceFormDraft): Partial<CareerVoiceValues> {
  return {
    companyName: draft?.companyName ?? draft?.title ?? "",
    positionName: draft?.positionName ?? "",
    locationName: draft?.locationName ?? "",
    fieldName: draft?.fieldName ?? "",
    statusName: draft?.statusName ?? "",
    startDate: draft?.startDate ?? "",
    endDate: draft?.endDate ?? "",
    appliedAt: draft?.appliedAt ?? todayIso(),
    isCurrent: draft?.isCurrent ?? false,
    summary: draft?.summary ?? "",
    achievements: draft?.achievements ?? "",
    technologies: draft?.technologies ?? "",
    url: draft?.url ?? "",
    contact: draft?.contact ?? "",
    notes: draft?.notes ?? "",
    companyId: draft?.companyId ?? null,
    positionId: draft?.positionId ?? null,
    locationId: draft?.locationId ?? null,
    fieldId: draft?.fieldId ?? null,
    statusId: draft?.statusId ?? null,
    technologyIds: draft?.technologyIds ?? [],
    workExperienceId: draft?.workExperienceId ?? null,
  };
}

export function VoiceFormProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [kind, setKind] = useState<VoiceFormKind | null>(null);
  const [workDate, setWorkDate] = useState(todayIso());
  const [taskInitial, setTaskInitial] = useState<Partial<CreateTaskItemFormValues>>({});
  const [noteInitial, setNoteInitial] = useState<Partial<CreateNoteFormValues>>({});
  const [entityInitial, setEntityInitial] = useState<EntityInitial>({});
  const [careerInitial, setCareerInitial] = useState<Partial<CareerVoiceValues>>({});
  const [vaultAccountId, setVaultAccountId] = useState<number | null>(null);
  const [careerCatalogKind, setCareerCatalogKind] =
    useState<CareerCatalogKind>("companies");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [taskCompleted, setTaskCompleted] = useState(false);
  const [voicePatch, setVoicePatch] = useState<VoiceFormFieldPatch | null>(null);
  const [submitRequestId, setSubmitRequestId] = useState(0);
  const patchCounter = useRef(0);

  const taskStatuses = useStatuses("task");
  const noteStatuses = useStatuses("note");
  const taskCategories = useCategories("task");
  const noteCategories = useCategories("note");
  const people = usePeople(true);
  const projects = useProjects(true);
  const vaultAccounts = useVaultAccounts();

  const closeForm = useCallback(() => {
    setOpen(false);
    setKind(null);
    setVoicePatch(null);
    setEditingId(null);
    setTaskCompleted(false);
  }, []);

  const openCatalog = useCallback((nextKind: CatalogKind, draft?: VoiceFormDraft) => {
    setEntityInitial({
      name: draft?.name ?? draft?.title ?? "",
      description: draft?.description ?? draft?.content ?? "",
      color: draft?.color ?? "#64748B",
      isFinal: draft?.isFinal ?? false,
      isActive: draft?.isActive ?? true,
      itemType: draft?.itemType ?? "task",
      serviceName: draft?.serviceName ?? "",
      username: draft?.username ?? "",
      password: draft?.password ?? "",
      url: draft?.url ?? "",
      notes: draft?.notes ?? "",
      tags: draft?.tags ?? "",
    });
    setVaultAccountId(draft?.accountId ?? null);
    if (draft?.catalogKind) setCareerCatalogKind(draft.catalogKind);
    setEditingId(draft?.id ?? null);
    setKind(nextKind);
    setOpen(true);
  }, []);

  const openTaskForm = useCallback(
    (draft?: VoiceFormDraft) => {
      const resolved = resolveDraftIds(
        draft,
        taskStatuses.data ?? [],
        taskCategories.data ?? [],
        people.data ?? [],
        projects.data ?? [],
      );
      const date = resolved.workDate ?? todayIso();
      setWorkDate(date);
      setEditingId(resolved.id ?? null);
      setTaskCompleted(resolved.isCompleted ?? false);
      setTaskInitial({
        title: resolved.title ?? "",
        content: resolved.content ?? "",
        workDate: date,
        startTime: resolved.startTime ?? null,
        endTime: resolved.endTime ?? null,
        statusId: resolved.statusId,
        categoryId: resolved.categoryId,
        personId: resolved.personId ?? null,
        projectId: resolved.projectId ?? null,
        companyId: resolved.companyId ?? null,
        sortOrder: resolved.sortOrder ?? 0,
        durationMinutes: resolved.durationMinutes ?? 0,
        parentTaskId: resolved.parentTaskId ?? null,
      });
      setKind("task");
      setOpen(true);
    },
    [people.data, projects.data, taskCategories.data, taskStatuses.data],
  );

  const openNoteForm = useCallback(
    (draft?: VoiceFormDraft) => {
      const resolved = resolveDraftIds(
        draft,
        noteStatuses.data ?? [],
        noteCategories.data ?? [],
        people.data ?? [],
        projects.data ?? [],
      );
      const date = resolved.workDate ?? todayIso();
      setWorkDate(date);
      setEditingId(resolved.id ?? null);
      setNoteInitial({
        title: resolved.title ?? "",
        content: resolved.content ?? "",
        workDate: date,
        startTime: resolved.startTime ?? null,
        endTime: resolved.endTime ?? null,
        statusId: resolved.statusId,
        categoryId: resolved.categoryId,
        personId: resolved.personId ?? null,
        projectId: resolved.projectId ?? null,
        companyId: resolved.companyId ?? null,
        sortOrder: resolved.sortOrder ?? 0,
        durationMinutes: resolved.durationMinutes ?? 0,
        parentNoteId: resolved.parentNoteId ?? null,
      });
      setKind("note");
      setOpen(true);
    },
    [noteCategories.data, noteStatuses.data, people.data, projects.data],
  );

  const openPersonForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("person", draft),
    [openCatalog],
  );
  const openProjectForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("project", draft),
    [openCatalog],
  );
  const openStatusForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("status", draft),
    [openCatalog],
  );
  const openCategoryForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("category", draft),
    [openCatalog],
  );
  const openVaultAccountForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("vault_account", draft),
    [openCatalog],
  );
  const openVaultPasswordForm = useCallback(
    (draft?: VoiceFormDraft) => {
      const accounts = vaultAccounts.data ?? [];
      const accountId = draft?.accountId ?? accounts[0]?.id ?? null;
      if (accountId == null) {
        openCatalog("vault_account", {
          name: "Mi bóveda",
          description: "Cuenta creada para guardar contraseñas",
        });
        return;
      }
      openCatalog("vault_password", { ...draft, accountId });
    },
    [openCatalog, vaultAccounts.data],
  );
  const openCareerCatalogForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("career_catalog", draft),
    [openCatalog],
  );
  const openVaultServiceForm = useCallback(
    (draft?: VoiceFormDraft) => openCatalog("vault_service", draft),
    [openCatalog],
  );
  const openWorkExperienceForm = useCallback((draft?: VoiceFormDraft) => {
    setCareerInitial(draftToCareerInitial(draft));
    setEditingId(draft?.id ?? null);
    setKind("work_experience");
    setOpen(true);
  }, []);
  const openJobApplicationForm = useCallback((draft?: VoiceFormDraft) => {
    setCareerInitial(draftToCareerInitial(draft));
    setEditingId(draft?.id ?? null);
    setKind("job_application");
    setOpen(true);
  }, []);

  const handleVoiceText = useCallback(
    (text: string): { handled: boolean; message: string } => {
      if (!open || !kind) return { handled: false, message: "" };

      const statuses =
        kind === "task" ? taskStatuses.data ?? [] : noteStatuses.data ?? [];
      const categories =
        kind === "task" ? taskCategories.data ?? [] : noteCategories.data ?? [];

      const result = parseVoiceFormCommand(text, {
        kind,
        statuses,
        categories,
        people: people.data ?? [],
        projects: projects.data ?? [],
        today: new Date(),
      });

      if (result.type === "field") {
        patchCounter.current += 1;
        setVoicePatch({
          id: patchCounter.current,
          ...result.patch,
        });
        return { handled: true, message: result.message };
      }

      if (result.type === "submit") {
        setSubmitRequestId((current) => current + 1);
        return { handled: true, message: result.message };
      }

      if (result.type === "cancel") {
        closeForm();
        return { handled: true, message: result.message };
      }

      return { handled: true, message: result.message };
    },
    [
      closeForm,
      kind,
      noteCategories.data,
      noteStatuses.data,
      open,
      people.data,
      projects.data,
      taskCategories.data,
      taskStatuses.data,
    ],
  );

  const value = useMemo(
    () => ({
      isOpen: open,
      kind,
      openTaskForm,
      openNoteForm,
      openPersonForm,
      openProjectForm,
      openStatusForm,
      openCategoryForm,
      openVaultAccountForm,
      openVaultPasswordForm,
      openCareerCatalogForm,
      openVaultServiceForm,
      openWorkExperienceForm,
      openJobApplicationForm,
      closeForm,
      handleVoiceText,
    }),
    [
      closeForm,
      handleVoiceText,
      kind,
      open,
      openCategoryForm,
      openCareerCatalogForm,
      openJobApplicationForm,
      openNoteForm,
      openPersonForm,
      openProjectForm,
      openStatusForm,
      openTaskForm,
      openVaultAccountForm,
      openVaultPasswordForm,
      openVaultServiceForm,
      openWorkExperienceForm,
    ],
  );

  const isEntityCatalog =
    kind != null &&
    kind !== "task" &&
    kind !== "note" &&
    kind !== "work_experience" &&
    kind !== "job_application";
  const isCareerRecord = kind === "work_experience" || kind === "job_application";

  return (
    <VoiceFormContext.Provider value={value}>
      {children}
      <FormModal
        open={open}
        onOpenChange={(next) => {
          if (!next) closeForm();
        }}
        title={
          kind
            ? editingId != null
              ? EDIT_TITLES[kind]
              : TITLES[kind]
            : "Formulario"
        }
        description={kind ? DESCRIPTIONS[kind] : undefined}
        size={kind === "task" || kind === "note" ? "lg" : "sm"}
      >
        {kind === "task" ? (
          <TaskItemForm
            key={`voice-task-${editingId ?? "new"}-${workDate}-${JSON.stringify(taskInitial)}`}
            workDate={workDate}
            initialValues={taskInitial}
            voicePatch={voicePatch}
            submitRequestId={submitRequestId}
            parentTaskId={taskInitial.parentTaskId ?? null}
            editingId={editingId}
            isCompleted={taskCompleted}
            onClose={closeForm}
          />
        ) : null}
        {kind === "note" ? (
          <NoteForm
            key={`voice-note-${editingId ?? "new"}-${workDate}-${JSON.stringify(noteInitial)}`}
            workDate={workDate}
            initialValues={noteInitial}
            voicePatch={voicePatch}
            submitRequestId={submitRequestId}
            parentNoteId={noteInitial.parentNoteId ?? null}
            editingId={editingId}
            onClose={closeForm}
          />
        ) : null}
        {isEntityCatalog && kind ? (
          <VoiceEntityForm
            key={`voice-${kind}-${editingId ?? "new"}-${JSON.stringify(entityInitial)}-${vaultAccountId}-${careerCatalogKind}`}
            kind={kind}
            initialValues={entityInitial}
            accountId={vaultAccountId}
            catalogKind={careerCatalogKind}
            editingId={editingId}
            voicePatch={voicePatch}
            submitRequestId={submitRequestId}
            onClose={closeForm}
          />
        ) : null}
        {isCareerRecord && kind ? (
          <VoiceCareerForm
            key={`voice-${kind}-${editingId ?? "new"}-${JSON.stringify(careerInitial)}`}
            kind={kind}
            initialValues={careerInitial}
            editingId={editingId}
            voicePatch={voicePatch}
            submitRequestId={submitRequestId}
            onClose={closeForm}
          />
        ) : null}
      </FormModal>
    </VoiceFormContext.Provider>
  );
}

export function useVoiceForm() {
  const context = useContext(VoiceFormContext);
  if (!context) {
    throw new Error("useVoiceForm debe usarse dentro de VoiceFormProvider");
  }
  return context;
}
