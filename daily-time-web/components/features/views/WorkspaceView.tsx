"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  Building2,
  Clock3,
  FolderKanban,
  ListChecks,
  StickyNote,
  Users,
} from "lucide-react";
import { useTimeEntriesByDate } from "@/hooks/queries/use-time-entries";
import { useTaskItemsByDate } from "@/hooks/queries/use-task-items";
import { useNotesByDate } from "@/hooks/queries/use-notes";
import { usePeople } from "@/hooks/queries/use-people";
import { useProjects } from "@/hooks/queries/use-projects";
import { useCompanies } from "@/hooks/queries/use-companies";
import { createDefaultDateFilter, getDateFilterRange } from "@/lib/utils/date";
import { formatDurationMinutes, sumDurationMinutes } from "@/lib/utils/time";
import { ViewDateFilter } from "@/components/features/day/ViewDateFilter";
import { TimeEntryDayList } from "@/components/features/time-entries/TimeEntryDayList";
import type { TimeEntry } from "@/types/api";

type OwnerMeta = {
  personId: number | null;
  projectId: number | null;
  companyId: number | null;
};

function parseFilterParam(value: string | null): number | "all" {
  if (!value || value === "all") return "all";
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : "all";
}

function matchesOwnerFilters(
  entry: TimeEntry,
  taskMeta: Map<number, OwnerMeta>,
  noteMeta: Map<number, OwnerMeta>,
  personId: number | "all",
  projectId: number | "all",
  companyId: number | "all",
) {
  if (personId === "all" && projectId === "all" && companyId === "all") return true;

  const meta =
    entry.taskItemId != null
      ? taskMeta.get(entry.taskItemId)
      : entry.noteId != null
        ? noteMeta.get(entry.noteId)
        : undefined;

  if (!meta) return false;
  if (personId !== "all" && meta.personId !== personId) return false;
  if (projectId !== "all" && meta.projectId !== projectId) return false;
  if (companyId !== "all" && meta.companyId !== companyId) return false;
  return true;
}

export function WorkspaceView() {
  const searchParams = useSearchParams();
  const [dateFilter, setDateFilter] = useState(createDefaultDateFilter);
  const [personFilter, setPersonFilter] = useState<number | "all">("all");
  const [projectFilter, setProjectFilter] = useState<number | "all">("all");
  const [companyFilter, setCompanyFilter] = useState<number | "all">("all");
  const { fromDate, toDate } = getDateFilterRange(dateFilter);

  useEffect(() => {
    const person = searchParams.get("person");
    const project = searchParams.get("project");
    const company = searchParams.get("company");
    if (person != null) setPersonFilter(parseFilterParam(person));
    if (project != null) setProjectFilter(parseFilterParam(project));
    if (company != null) setCompanyFilter(parseFilterParam(company));
  }, [searchParams]);

  const entries = useTimeEntriesByDate(fromDate, toDate);
  const tasks = useTaskItemsByDate(fromDate, toDate);
  const notes = useNotesByDate(fromDate, toDate);
  const people = usePeople(true);
  const projects = useProjects(true);
  const companies = useCompanies(true);

  const taskTitles = useMemo(() => {
    const map = new Map<number, string>();
    for (const task of tasks.data ?? []) {
      map.set(task.id, task.title);
    }
    return map;
  }, [tasks.data]);

  const noteTitles = useMemo(() => {
    const map = new Map<number, string>();
    for (const note of notes.data ?? []) {
      map.set(note.id, note.title?.trim() || note.content.slice(0, 48));
    }
    return map;
  }, [notes.data]);

  const taskMeta = useMemo(() => {
    const map = new Map<number, OwnerMeta>();
    for (const task of tasks.data ?? []) {
      map.set(task.id, {
        personId: task.personId,
        projectId: task.projectId,
        companyId: task.companyId,
      });
    }
    return map;
  }, [tasks.data]);

  const noteMeta = useMemo(() => {
    const map = new Map<number, OwnerMeta>();
    for (const note of notes.data ?? []) {
      map.set(note.id, {
        personId: note.personId,
        projectId: note.projectId,
        companyId: note.companyId,
      });
    }
    return map;
  }, [notes.data]);

  const data = useMemo(
    () =>
      (entries.data ?? []).filter((entry) =>
        matchesOwnerFilters(
          entry,
          taskMeta,
          noteMeta,
          personFilter,
          projectFilter,
          companyFilter,
        ),
      ),
    [entries.data, taskMeta, noteMeta, personFilter, projectFilter, companyFilter],
  );

  const totalMinutes = sumDurationMinutes(data);
  const taskMinutes = sumDurationMinutes(
    data.filter((entry) => entry.taskItemId != null),
  );
  const noteMinutes = sumDurationMinutes(
    data.filter((entry) => entry.noteId != null),
  );
  const loading = entries.isLoading || tasks.isLoading || notes.isLoading;
  const error = entries.error;

  const selectClass =
    "h-9 min-w-[10rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--ink)] outline-none transition focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent)]/15";

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <p className="text-sm text-[var(--muted)]">
        Informe de tiempo registrado en el periodo. Para crear o editar tareas y
        notas usa las otras vistas.
      </p>

      <ViewDateFilter value={dateFilter} onChange={setDateFilter} />

      <section className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)]/80 px-4 py-3 shadow-[var(--shadow-card)] lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2 text-sm font-medium text-[var(--ink)]">
          <Users className="size-4 text-[var(--accent)]" />
          Persona, proyecto y empresa
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
          <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)] sm:flex-row sm:items-center sm:gap-2">
            <span className="inline-flex items-center gap-1.5">
              <Users className="size-3.5" />
              Persona
            </span>
            <select
              value={personFilter === "all" ? "all" : String(personFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setPersonFilter(value === "all" ? "all" : Number(value));
              }}
              className={selectClass}
              disabled={people.isLoading}
              aria-label="Filtrar por persona"
            >
              <option value="all">Todas</option>
              {people.data?.map((person) => (
                <option key={person.id} value={person.id}>
                  {person.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)] sm:flex-row sm:items-center sm:gap-2">
            <span className="inline-flex items-center gap-1.5">
              <FolderKanban className="size-3.5" />
              Proyecto
            </span>
            <select
              value={projectFilter === "all" ? "all" : String(projectFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setProjectFilter(value === "all" ? "all" : Number(value));
              }}
              className={selectClass}
              disabled={projects.isLoading}
              aria-label="Filtrar por proyecto"
            >
              <option value="all">Todos</option>
              {projects.data?.map((project) => (
                <option key={project.id} value={project.id}>
                  {project.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)] sm:flex-row sm:items-center sm:gap-2">
            <span className="inline-flex items-center gap-1.5">
              <Building2 className="size-3.5" />
              Empresa
            </span>
            <select
              value={companyFilter === "all" ? "all" : String(companyFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setCompanyFilter(value === "all" ? "all" : Number(value));
              }}
              className={selectClass}
              disabled={companies.isLoading}
              aria-label="Filtrar por empresa"
            >
              <option value="all">Todas</option>
              {companies.data?.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </label>
        </div>
      </section>

      <div className="grid gap-3 sm:grid-cols-3">
        <SummaryCard
          icon={Clock3}
          label="Total registrado"
          value={loading ? "…" : formatDurationMinutes(totalMinutes)}
          hint={`${data.length} registro${data.length === 1 ? "" : "s"}`}
        />
        <SummaryCard
          icon={ListChecks}
          label="En tareas"
          value={loading ? "…" : formatDurationMinutes(taskMinutes)}
          hint={`${data.filter((e) => e.taskItemId != null).length} entradas`}
        />
        <SummaryCard
          icon={StickyNote}
          label="En notas"
          value={loading ? "…" : formatDurationMinutes(noteMinutes)}
          hint={`${data.filter((e) => e.noteId != null).length} entradas`}
        />
      </div>

      <section className="rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[var(--shadow-card)]">
        <h2 className="mb-4 font-[family-name:var(--font-fraunces)] text-lg font-semibold text-[var(--ink)]">
          Detalle del periodo
        </h2>

        {loading && (
          <p className="text-sm text-[var(--muted)]">Cargando informe…</p>
        )}
        {error && (
          <p className="text-sm text-[var(--danger)]">{String(error)}</p>
        )}
        {!loading && !error && (
          <TimeEntryDayList
            fromDate={fromDate}
            toDate={toDate}
            taskTitles={taskTitles}
            noteTitles={noteTitles}
            taskMeta={taskMeta}
            noteMeta={noteMeta}
            personId={personFilter}
            projectId={projectFilter}
            companyId={companyFilter}
          />
        )}
      </section>
    </div>
  );
}

function SummaryCard({
  icon: Icon,
  label,
  value,
  hint,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  hint: string;
}) {
  return (
    <div className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]">
      <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
        <Icon className="size-3.5 text-[var(--accent)]" />
        {label}
      </div>
      <p className="font-[family-name:var(--font-fraunces)] text-2xl font-semibold text-[var(--ink)]">
        {value}
      </p>
      <p className="mt-1 text-xs text-[var(--muted)]">{hint}</p>
    </div>
  );
}
