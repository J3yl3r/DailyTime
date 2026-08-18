"use client";

import { useState } from "react";
import { useTaskItemsByDate } from "@/hooks/queries/use-task-items";
import { createDefaultDateFilter, getDateFilterRange } from "@/lib/utils/date";
import { TaskCardTree } from "@/components/features/cards/TaskCardTree";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { ViewDateFilter } from "@/components/features/day/ViewDateFilter";
import { CreatePanel } from "@/components/ui/CreatePanel";
import {
  TimeEntryOverlay,
  useTimeTarget,
} from "@/components/features/time-entries/TimeEntryOverlay";

export function TasksView() {
  const [dateFilter, setDateFilter] = useState(createDefaultDateFilter);
  const { fromDate, toDate, anchorDate: workDate } = getDateFilterRange(dateFilter);
  const { data, isLoading, error } = useTaskItemsByDate(fromDate, toDate);
  const [showForm, setShowForm] = useState(false);
  const time = useTimeTarget();

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <p className="text-sm text-[var(--muted)]">
          Tarjetas de tareas del periodo seleccionado.
        </p>
        <CreatePanel
          open={showForm}
          onOpen={() => setShowForm(true)}
          onClose={() => setShowForm(false)}
          label="Nueva tarea"
          modalSize="lg"
          className="sm:max-w-sm sm:items-end"
        >
          <TaskItemForm workDate={workDate} onClose={() => setShowForm(false)} />
        </CreatePanel>
      </div>

      <ViewDateFilter value={dateFilter} onChange={setDateFilter} />

      {isLoading && <p className="text-sm text-[var(--muted)]">Cargando tareas…</p>}
      {error && <p className="text-sm text-[var(--danger)]">{String(error)}</p>}
      {!isLoading && !error && !data?.length && (
        <p className="rounded-xl border border-dashed border-[var(--border)] bg-[var(--surface)]/60 px-4 py-10 text-center text-sm text-[var(--muted)]">
          No hay tareas para este periodo. Crea la primera con el botón de arriba.
        </p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {data?.map((task) => (
          <TaskCardTree
            key={task.id}
            task={task}
            workDate={workDate}
            onAddTime={time.openForTask}
          />
        ))}
      </div>

      <TimeEntryOverlay
        workDate={workDate}
        target={time.target}
        onClose={time.close}
      />
    </div>
  );
}
