"use client";

import { useMemo } from "react";
import { useTimeEntriesByDate } from "@/hooks/queries/use-time-entries";
import { useTimeEntryMutations } from "@/hooks/mutations/use-time-entry-mutations";
import { formatDurationMinutes, sumDurationMinutes } from "@/lib/utils/time";
import { useConfirm } from "@/providers/confirm-provider";
import { toast } from "sonner";
import type { TimeEntry } from "@/types/api";

type OwnerMeta = {
  personId: number | null;
  projectId: number | null;
  companyId: number | null;
};

type Props = {
  fromDate: string;
  toDate: string;
  taskTitles?: Map<number, string>;
  noteTitles?: Map<number, string>;
  taskMeta?: Map<number, OwnerMeta>;
  noteMeta?: Map<number, OwnerMeta>;
  personId?: number | "all";
  projectId?: number | "all";
  companyId?: number | "all";
};

function matchesOwnerFilters(
  entry: TimeEntry,
  taskMeta: Map<number, OwnerMeta> | undefined,
  noteMeta: Map<number, OwnerMeta> | undefined,
  personId: number | "all",
  projectId: number | "all",
  companyId: number | "all",
) {
  if (personId === "all" && projectId === "all" && companyId === "all") return true;

  const meta =
    entry.taskItemId != null
      ? taskMeta?.get(entry.taskItemId)
      : entry.noteId != null
        ? noteMeta?.get(entry.noteId)
        : undefined;

  if (!meta) return false;
  if (personId !== "all" && meta.personId !== personId) return false;
  if (projectId !== "all" && meta.projectId !== projectId) return false;
  if (companyId !== "all" && meta.companyId !== companyId) return false;
  return true;
}

export function TimeEntryDayList({
  fromDate,
  toDate,
  taskTitles,
  noteTitles,
  taskMeta,
  noteMeta,
  personId = "all",
  projectId = "all",
  companyId = "all",
}: Props) {
  const { data, isLoading, error } = useTimeEntriesByDate(fromDate, toDate);
  const { remove } = useTimeEntryMutations(fromDate);
  const confirm = useConfirm();

  const handleDelete = async (entry: TimeEntry) => {
    const ok = await confirm({
      title: "Eliminar registro de tiempo",
      description:
        "¿Estás seguro de eliminar este registro de tiempo? Esta acción no se puede deshacer.",
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(entry.id, {
      onSuccess: () => toast.success("Registro eliminado"),
      onError: (e) => toast.error(e.message),
    });
  };

  const filtered = useMemo(
    () =>
      (data ?? []).filter((entry) =>
        matchesOwnerFilters(
          entry,
          taskMeta,
          noteMeta,
          personId,
          projectId,
          companyId,
        ),
      ),
    [data, taskMeta, noteMeta, personId, projectId, companyId],
  );

  const groups = useMemo(() => {
    const map = new Map<string, TimeEntry[]>();
    for (const entry of filtered) {
      const list = map.get(entry.workDate) ?? [];
      list.push(entry);
      map.set(entry.workDate, list);
    }
    return [...map.entries()].sort(([a], [b]) => b.localeCompare(a));
  }, [filtered]);

  if (isLoading) {
    return <p className="text-sm text-[var(--muted)]">Cargando tiempo…</p>;
  }
  if (error) {
    return <p className="text-sm text-[var(--danger)]">{String(error)}</p>;
  }
  if (!filtered.length) {
    return (
      <p className="rounded-lg border border-dashed border-[var(--border)] px-4 py-8 text-center text-sm text-[var(--muted)]">
        Sin registros de tiempo en este periodo
        {personId !== "all" || projectId !== "all" || companyId !== "all"
          ? " con los filtros seleccionados"
          : ""}
        .
      </p>
    );
  }

  const total = sumDurationMinutes(filtered);

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm font-medium text-[var(--ink)]">
        Total del periodo:{" "}
        <span className="text-[var(--accent)]">{formatDurationMinutes(total)}</span>
      </p>

      {groups.map(([date, entries]) => {
        const dayTotal = sumDurationMinutes(entries);
        return (
          <section key={date} className="flex flex-col gap-2">
            <div className="flex items-center justify-between gap-2">
              <h3 className="text-sm font-semibold text-[var(--ink)]">{date}</h3>
              <span className="text-xs text-[var(--muted)]">
                {formatDurationMinutes(dayTotal)}
              </span>
            </div>
            <ul className="flex flex-col gap-1.5">
              {entries.map((entry) => {
                const ownerLabel =
                  entry.taskItemId != null
                    ? taskTitles?.get(entry.taskItemId) ??
                      `Tarea #${entry.taskItemId}`
                    : entry.noteId != null
                      ? noteTitles?.get(entry.noteId) ?? `Nota #${entry.noteId}`
                      : "Sin referencia";
                const kind =
                  entry.taskItemId != null
                    ? "Tarea"
                    : entry.noteId != null
                      ? "Nota"
                      : null;

                return (
                  <li
                    key={entry.id}
                    className="flex items-start justify-between gap-3 rounded-lg border border-[var(--border)] bg-white px-3 py-2.5"
                  >
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-sm font-semibold text-[var(--ink)]">
                          {formatDurationMinutes(entry.durationMinutes)}
                        </span>
                        {kind && (
                          <span
                            className={
                              kind === "Tarea"
                                ? "rounded-md bg-[var(--accent-soft)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-[var(--accent)]"
                                : "rounded-md bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-[var(--note)]"
                            }
                          >
                            {kind}
                          </span>
                        )}
                      </div>
                      <p className="mt-1 truncate text-sm text-[var(--ink)]">
                        {ownerLabel}
                      </p>
                      {entry.description && (
                        <p className="mt-0.5 text-xs text-[var(--muted)]">
                          {entry.description}
                        </p>
                      )}
                    </div>
                    <button
                      type="button"
                      onClick={() => handleDelete(entry)}
                      className="shrink-0 text-xs text-[var(--danger)] hover:underline"
                      disabled={remove.isPending}
                    >
                      Eliminar
                    </button>
                  </li>
                );
              })}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
