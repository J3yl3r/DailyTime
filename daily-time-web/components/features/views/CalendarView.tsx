"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  addDays,
  addMonths,
  addWeeks,
  addYears,
  eachDayOfInterval,
  eachMonthOfInterval,
  endOfMonth,
  endOfWeek,
  endOfYear,
  format,
  isSameDay,
  isSameMonth,
  isToday,
  startOfMonth,
  startOfWeek,
  startOfYear,
} from "date-fns";
import { es } from "date-fns/locale";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import type { Note, TaskItem } from "@/types/api";
import { useTaskItemsByDate } from "@/hooks/queries/use-task-items";
import { useNotesByDate } from "@/hooks/queries/use-notes";
import { useTaskItemMutations } from "@/hooks/mutations/use-task-item-mutations";
import { useNoteMutations } from "@/hooks/mutations/use-note-mutations";
import { toApiDate } from "@/lib/utils/date";
import { cn } from "@/lib/utils/cn";
import { FormModal } from "@/components/shared/form-modal";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { useVoiceUiBridge } from "@/providers/voice-ui-bridge";
import type { VoiceCalendarCommand } from "@/types/voice";

type CalendarMode = "day" | "week" | "month" | "year";
type CreateKind = "task" | "note";

type CreateTarget = {
  workDate: string;
  hour?: number;
  kind: CreateKind;
};

const HOUR_START = 6;
const HOUR_END = 22;
const HOURS = Array.from(
  { length: HOUR_END - HOUR_START + 1 },
  (_, i) => HOUR_START + i
);
const ROW_HEIGHT = 56;

function weekDays(anchor: Date) {
  const start = startOfWeek(anchor, { weekStartsOn: 1 });
  const end = endOfWeek(anchor, { weekStartsOn: 1 });
  return eachDayOfInterval({ start, end });
}

function formatHour(hour: number) {
  return `${String(hour).padStart(2, "0")}:00`;
}

function timeToMinutes(value: string) {
  const [hours, minutes] = value.slice(0, 5).split(":").map(Number);
  return hours * 60 + minutes;
}

export function CalendarView() {
  const { registerCalendarHandler } = useVoiceUiBridge();
  const [mode, setMode] = useState<CalendarMode>("week");
  const [anchorDate, setAnchorDate] = useState(() => new Date());
  const [createTarget, setCreateTarget] = useState<CreateTarget | null>(null);
  const [scheduleTask, setScheduleTask] = useState<TaskItem | null>(null);
  const [scheduleNote, setScheduleNote] = useState<Note | null>(null);

  const days = useMemo(() => {
    if (mode === "day") return [anchorDate];
    if (mode === "week") return weekDays(anchorDate);
    if (mode === "month") {
      return eachDayOfInterval({
        start: startOfMonth(anchorDate),
        end: endOfMonth(anchorDate),
      });
    }
    return eachDayOfInterval({
      start: startOfYear(anchorDate),
      end: endOfYear(anchorDate),
    });
  }, [anchorDate, mode]);

  const fromDate = toApiDate(days[0]);
  const toDate = toApiDate(days[days.length - 1]);

  const tasksQuery = useTaskItemsByDate(fromDate, toDate);
  const notesQuery = useNotesByDate(fromDate, toDate);

  const tasksByDate = useMemo(() => {
    const map = new Map<string, TaskItem[]>();
    for (const task of tasksQuery.data ?? []) {
      if (task.startTime && task.endTime) continue;
      const list = map.get(task.workDate) ?? [];
      list.push(task);
      map.set(task.workDate, list);
    }
    return map;
  }, [tasksQuery.data]);

  const scheduledTasksByDate = useMemo(() => {
    const map = new Map<string, TaskItem[]>();
    for (const task of tasksQuery.data ?? []) {
      if (!task.startTime || !task.endTime) continue;
      const list = map.get(task.workDate) ?? [];
      list.push(task);
      map.set(task.workDate, list);
    }
    return map;
  }, [tasksQuery.data]);

  const notesByDate = useMemo(() => {
    const map = new Map<string, Note[]>();
    for (const note of notesQuery.data ?? []) {
      if (!note.workDate) continue;
      if (note.startTime && note.endTime) continue;
      const list = map.get(note.workDate) ?? [];
      list.push(note);
      map.set(note.workDate, list);
    }
    return map;
  }, [notesQuery.data]);

  const scheduledNotesByDate = useMemo(() => {
    const map = new Map<string, Note[]>();
    for (const note of notesQuery.data ?? []) {
      if (!note.workDate || !note.startTime || !note.endTime) continue;
      const list = map.get(note.workDate) ?? [];
      list.push(note);
      map.set(note.workDate, list);
    }
    return map;
  }, [notesQuery.data]);

  const title = useMemo(() => {
    if (mode === "day") {
      return format(anchorDate, "EEEE d 'de' MMMM yyyy", { locale: es });
    }
    if (mode === "month") {
      return format(anchorDate, "MMMM yyyy", { locale: es });
    }
    if (mode === "year") {
      return format(anchorDate, "yyyy", { locale: es });
    }
    const start = days[0];
    const end = days[days.length - 1];
    if (start.getMonth() === end.getMonth()) {
      return `${format(start, "d", { locale: es })} – ${format(end, "d 'de' MMMM yyyy", { locale: es })}`;
    }
    return `${format(start, "d MMM", { locale: es })} – ${format(end, "d MMM yyyy", { locale: es })}`;
  }, [anchorDate, days, mode]);

  const now = new Date();
  const showNowLine =
    now.getHours() >= HOUR_START &&
    now.getHours() <= HOUR_END &&
    days.some((d) => isSameDay(d, now));
  const nowTop =
    ((now.getHours() - HOUR_START) * 60 + now.getMinutes()) * (ROW_HEIGHT / 60);

  function goToday() {
    setAnchorDate(new Date());
  }

  function shiftAnchor(direction: -1 | 1, activeMode: CalendarMode = mode) {
    setAnchorDate((current) => {
      if (activeMode === "day") return addDays(current, direction);
      if (activeMode === "week") return addWeeks(current, direction);
      if (activeMode === "month") return addMonths(current, direction);
      return addYears(current, direction);
    });
  }

  function goPrev() {
    shiftAnchor(-1);
  }

  function goNext() {
    shiftAnchor(1);
  }

  useEffect(() => {
    return registerCalendarHandler((command: VoiceCalendarCommand) => {
      const nextMode = (command.mode ?? command.unit ?? mode) as CalendarMode;
      if (command.action === "set_mode" && command.mode) {
        setMode(command.mode);
        return;
      }
      if (command.mode || command.unit) {
        setMode(nextMode);
      }
      if (command.action === "today") {
        setAnchorDate(new Date());
        return;
      }
      if (command.action === "prev") {
        shiftAnchor(-1, nextMode);
        return;
      }
      if (command.action === "next") {
        shiftAnchor(1, nextMode);
      }
    });
  }, [mode, registerCalendarHandler]);

  function openCreate(workDate: string, kind: CreateKind, hour?: number) {
    setCreateTarget({ workDate, kind, hour });
  }

  const loading = tasksQuery.isLoading || notesQuery.isLoading;
  const error = tasksQuery.error || notesQuery.error;

  return (
    <div className="flex h-[calc(100dvh-7.5rem)] min-h-[32rem] flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={goToday}
            className="rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 py-1.5 text-sm font-medium text-[var(--ink)] hover:bg-[var(--surface-muted)]"
          >
            Hoy
          </button>
          <div className="flex items-center rounded-lg border border-[var(--border)] bg-[var(--surface)]">
            <button
              type="button"
              onClick={goPrev}
              className="rounded-l-lg p-1.5 text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
              aria-label="Anterior"
            >
              <ChevronLeft className="size-4" />
            </button>
            <button
              type="button"
              onClick={goNext}
              className="rounded-r-lg p-1.5 text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
              aria-label="Siguiente"
            >
              <ChevronRight className="size-4" />
            </button>
          </div>
          <h2 className="font-[family-name:var(--font-fraunces)] text-lg font-semibold capitalize text-[var(--ink)]">
            {title}
          </h2>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="flex rounded-lg border border-[var(--border)] bg-[var(--surface)] p-1">
            {(
              [
                ["day", "Día"],
                ["week", "Semana"],
                ["month", "Mes"],
                ["year", "Año"],
              ] as const
            ).map(([value, label]) => (
              <button
                key={value}
                type="button"
                onClick={() => setMode(value)}
                className={cn(
                  "rounded-md px-3 py-1.5 text-sm transition-colors",
                  mode === value
                    ? "bg-[var(--accent)] text-white"
                    : "text-[var(--muted)] hover:text-[var(--ink)]"
                )}
              >
                {label}
              </button>
            ))}
          </div>
          <p className="max-w-sm text-xs text-[var(--muted)]">
            Las tareas con horario aparecen en la grilla. Haz clic en una tarea
            para cambiar su inicio y final.
          </p>
        </div>
      </div>

      {loading && (
        <p className="text-sm text-[var(--muted)]">Cargando calendario…</p>
      )}
      {error && (
        <p className="text-sm text-[var(--danger)]">{String(error)}</p>
      )}

      {!loading && !error && mode === "month" && (
        <MonthCalendar
          anchorDate={anchorDate}
          tasks={tasksQuery.data ?? []}
          notes={notesQuery.data ?? []}
          onSelectDay={(day) => {
            setAnchorDate(day);
            setMode("day");
          }}
          onCreate={openCreate}
          onOpenTask={setScheduleTask}
          onOpenNote={setScheduleNote}
        />
      )}

      {!loading && !error && mode === "year" && (
        <YearCalendar
          anchorDate={anchorDate}
          tasks={tasksQuery.data ?? []}
          notes={notesQuery.data ?? []}
          onSelectDay={(day) => {
            setAnchorDate(day);
            setMode("day");
          }}
          onSelectMonth={(month) => {
            setAnchorDate(month);
            setMode("month");
          }}
        />
      )}

      {!loading && !error && (mode === "day" || mode === "week") && (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-[var(--shadow-card)]">
          <div
            className="grid border-b border-[var(--border)]"
            style={{
              gridTemplateColumns: `4.5rem repeat(${days.length}, minmax(0, 1fr))`,
            }}
          >
            <div className="border-r border-[var(--border)]" />
            {days.map((day) => {
              const apiDate = toApiDate(day);
              const today = isToday(day);
              return (
                <button
                  key={apiDate}
                  type="button"
                  onClick={() => {
                    setMode("day");
                    setAnchorDate(day);
                  }}
                  className="border-r border-[var(--border)] px-2 py-3 text-center last:border-r-0 hover:bg-[var(--surface-muted)]"
                >
                  <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--muted)]">
                    {format(day, "EEE", { locale: es })}
                  </p>
                  <p
                    className={cn(
                      "mx-auto mt-1 flex size-8 items-center justify-center rounded-full text-base font-semibold",
                      today
                        ? "bg-[var(--accent)] text-white"
                        : "text-[var(--ink)]"
                    )}
                  >
                    {format(day, "d")}
                  </p>
                </button>
              );
            })}
          </div>

          <div
            className="grid max-h-44 overflow-y-auto border-b border-[var(--border)] bg-[var(--surface-muted)]/40"
            style={{
              gridTemplateColumns: `4.5rem repeat(${days.length}, minmax(0, 1fr))`,
            }}
          >
            <div className="flex items-start justify-end border-r border-[var(--border)] px-2 py-2">
              <span className="text-[10px] font-medium uppercase tracking-wide text-[var(--muted)]">
                Sin horario
              </span>
            </div>
            {days.map((day) => {
              const apiDate = toApiDate(day);
              const dayTasks = tasksByDate.get(apiDate) ?? [];
              const dayNotes = notesByDate.get(apiDate) ?? [];
              return (
                <div
                  key={`allday-${apiDate}`}
                  className="flex flex-col gap-1 border-r border-[var(--border)] p-1.5 last:border-r-0"
                >
                  {dayTasks.map((task) => (
                    <button
                      key={`task-${task.id}`}
                      type="button"
                      onClick={() => setScheduleTask(task)}
                      className="truncate rounded-md bg-[var(--accent-soft)] px-2 py-1 text-left text-[11px] font-medium text-[var(--accent-strong)] hover:ring-1 hover:ring-[var(--accent)]"
                      title={task.title}
                    >
                      {task.title}
                    </button>
                  ))}
                  {dayNotes.map((note) => (
                    <button
                      key={`note-${note.id}`}
                      type="button"
                      onClick={() => setScheduleNote(note)}
                      className="truncate rounded-md bg-amber-100 px-2 py-1 text-left text-[11px] font-medium text-[var(--note)] hover:ring-1 hover:ring-[var(--note)]"
                      title={note.title?.trim() || note.content}
                    >
                      {note.title?.trim() || note.content}
                    </button>
                  ))}
                  {!dayTasks.length && !dayNotes.length && (
                    <span className="px-1 py-2 text-[10px] text-[var(--muted)]">
                      —
                    </span>
                  )}
                  <div className="mt-auto flex gap-1 pt-1">
                    <button
                      type="button"
                      onClick={() => openCreate(apiDate, "task")}
                      className="rounded px-1.5 py-0.5 text-[10px] text-[var(--accent)] hover:bg-[var(--accent-soft)]"
                    >
                      + Tarea
                    </button>
                    <button
                      type="button"
                      onClick={() => openCreate(apiDate, "note")}
                      className="rounded px-1.5 py-0.5 text-[10px] text-[var(--note)] hover:bg-amber-50"
                    >
                      + Nota
                    </button>
                  </div>
                </div>
              );
            })}
          </div>

          <div className="relative min-h-0 flex-1 overflow-auto">
            <div
              className="grid"
              style={{
                gridTemplateColumns: `4.5rem repeat(${days.length}, minmax(0, 1fr))`,
              }}
            >
              <div>
                {HOURS.map((hour) => (
                  <div
                    key={`label-${hour}`}
                    className="relative border-b border-[var(--border)]/70"
                    style={{ height: ROW_HEIGHT }}
                  >
                    <span className="absolute -top-2 right-2 text-[11px] text-[var(--muted)]">
                      {hour === HOUR_START ? "" : formatHour(hour)}
                    </span>
                  </div>
                ))}
              </div>

              {days.map((day) => {
                const apiDate = toApiDate(day);
                const isCurrentDay = isSameDay(day, now);
                const scheduledTasks = scheduledTasksByDate.get(apiDate) ?? [];
                const scheduledNotes = scheduledNotesByDate.get(apiDate) ?? [];
                const hasBothSchedules =
                  scheduledTasks.length > 0 && scheduledNotes.length > 0;
                return (
                  <div
                    key={`grid-${apiDate}`}
                    className="relative border-r border-[var(--border)] last:border-r-0"
                  >
                    {HOURS.map((hour) => (
                      <button
                        key={`${apiDate}-${hour}`}
                        type="button"
                        onClick={() => openCreate(apiDate, "task", hour)}
                        className="block w-full border-b border-[var(--border)]/70 text-left transition-colors hover:bg-[var(--accent-soft)]/40"
                        style={{ height: ROW_HEIGHT }}
                        title={`Crear en ${apiDate} ${formatHour(hour)}`}
                        aria-label={`Crear en ${apiDate} a las ${formatHour(hour)}`}
                      />
                    ))}
                    {scheduledTasks.map((task) => {
                      const startTime = task.startTime;
                      const endTime = task.endTime;
                      if (!startTime || !endTime) return null;
                      const start = Math.max(
                        timeToMinutes(startTime),
                        HOUR_START * 60
                      );
                      const end = Math.min(
                        timeToMinutes(endTime),
                        (HOUR_END + 1) * 60
                      );
                      if (end <= start) return null;
                      const top = (start - HOUR_START * 60) * (ROW_HEIGHT / 60);
                      const height = Math.max(
                        24,
                        (end - start) * (ROW_HEIGHT / 60)
                      );
                      return (
                        <button
                          key={`scheduled-${task.id}`}
                          type="button"
                          onClick={() => setScheduleTask(task)}
                          className={cn(
                            "absolute z-10 overflow-hidden rounded-md border-l-4 border-[var(--accent-strong)] bg-[var(--accent-soft)] px-2 py-1 text-left text-[11px] text-[var(--accent-strong)] shadow-sm hover:ring-2 hover:ring-[var(--accent)]/30",
                            hasBothSchedules
                              ? "right-[50%] left-1"
                              : "right-1 left-1"
                          )}
                          style={{ top, height }}
                          title={`${task.title} · ${startTime.slice(0, 5)}–${endTime.slice(0, 5)}`}
                        >
                          <span className="block truncate font-semibold">
                            {task.title}
                          </span>
                          <span className="block truncate text-[10px]">
                            {startTime.slice(0, 5)}–{endTime.slice(0, 5)}
                          </span>
                        </button>
                      );
                    })}
                    {scheduledNotes.map((note) => {
                      const startTime = note.startTime;
                      const endTime = note.endTime;
                      if (!startTime || !endTime) return null;
                      const start = Math.max(
                        timeToMinutes(startTime),
                        HOUR_START * 60
                      );
                      const end = Math.min(
                        timeToMinutes(endTime),
                        (HOUR_END + 1) * 60
                      );
                      if (end <= start) return null;
                      const top = (start - HOUR_START * 60) * (ROW_HEIGHT / 60);
                      const height = Math.max(
                        24,
                        (end - start) * (ROW_HEIGHT / 60)
                      );
                      const label = note.title?.trim() || note.content;
                      return (
                        <button
                          key={`scheduled-note-${note.id}`}
                          type="button"
                          onClick={() => setScheduleNote(note)}
                          className={cn(
                            "absolute z-20 overflow-hidden rounded-md border-l-4 border-[var(--note)] bg-amber-100 px-2 py-1 text-left text-[11px] text-[var(--note)] shadow-sm hover:ring-2 hover:ring-amber-400/40",
                            hasBothSchedules
                              ? "right-1 left-[50%]"
                              : "right-1 left-1"
                          )}
                          style={{ top, height }}
                          title={`${label} · ${startTime.slice(0, 5)}–${endTime.slice(0, 5)}`}
                        >
                          <span className="block truncate font-semibold">
                            {label}
                          </span>
                          <span className="block truncate text-[10px]">
                            {startTime.slice(0, 5)}–{endTime.slice(0, 5)}
                          </span>
                        </button>
                      );
                    })}
                    {showNowLine && isCurrentDay && (
                      <div
                        className="pointer-events-none absolute right-0 left-0 z-10"
                        style={{ top: nowTop }}
                      >
                        <div className="relative h-0.5 bg-red-500">
                          <span className="absolute -top-1.5 -left-1 size-3 rounded-full bg-red-500" />
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}

      <FormModal
        open={!!createTarget}
        onOpenChange={(open) => {
          if (!open) setCreateTarget(null);
        }}
        title={createTarget?.kind === "note" ? "Nueva nota" : "Nueva tarea"}
        description={
          createTarget
            ? createTarget.hour != null
              ? createTarget.kind === "task"
                ? `${createTarget.workDate} · ${formatHour(createTarget.hour)}`
                : `${createTarget.workDate} · ${formatHour(createTarget.hour)}`
              : createTarget.workDate
            : undefined
        }
        size="lg"
      >
        {createTarget && (
          <div className="flex flex-col gap-3">
            <div className="flex gap-1 rounded-lg bg-[var(--surface-muted)] p-1">
              <button
                type="button"
                onClick={() =>
                  setCreateTarget((current) =>
                    current ? { ...current, kind: "task" } : null
                  )
                }
                className={cn(
                  "flex-1 rounded-md px-2 py-1.5 text-xs font-medium",
                  createTarget.kind === "task"
                    ? "bg-[var(--surface)] text-[var(--ink)] shadow-sm"
                    : "text-[var(--muted)]"
                )}
              >
                Tarea
              </button>
              <button
                type="button"
                onClick={() =>
                  setCreateTarget((current) =>
                    current ? { ...current, kind: "note" } : null
                  )
                }
                className={cn(
                  "flex-1 rounded-md px-2 py-1.5 text-xs font-medium",
                  createTarget.kind === "note"
                    ? "bg-[var(--surface)] text-[var(--ink)] shadow-sm"
                    : "text-[var(--muted)]"
                )}
              >
                Nota
              </button>
            </div>
            {createTarget.kind === "task" ? (
              <TaskItemForm
                key={`task-${createTarget.workDate}-${createTarget.hour ?? "none"}`}
                workDate={createTarget.workDate}
                initialStartTime={
                  createTarget.hour != null
                    ? formatHour(createTarget.hour)
                    : null
                }
                initialEndTime={
                  createTarget.hour != null
                    ? formatHour(createTarget.hour + 1)
                    : null
                }
                onClose={() => setCreateTarget(null)}
              />
            ) : (
              <NoteForm
                key={`note-${createTarget.workDate}-${createTarget.hour ?? "none"}`}
                workDate={createTarget.workDate}
                initialStartTime={
                  createTarget.hour != null
                    ? formatHour(createTarget.hour)
                    : null
                }
                initialEndTime={
                  createTarget.hour != null
                    ? formatHour(createTarget.hour + 1)
                    : null
                }
                onClose={() => setCreateTarget(null)}
              />
            )}
          </div>
        )}
      </FormModal>

      <FormModal
        open={!!scheduleNote}
        onOpenChange={(open) => {
          if (!open) setScheduleNote(null);
        }}
        title="Programar nota"
        description={scheduleNote?.title?.trim() || scheduleNote?.content}
        size="sm"
      >
        {scheduleNote && (
          <ScheduleNoteForm
            key={`${scheduleNote.id}-${scheduleNote.startTime}-${scheduleNote.endTime}`}
            note={scheduleNote}
            onClose={() => setScheduleNote(null)}
          />
        )}
      </FormModal>

      <FormModal
        open={!!scheduleTask}
        onOpenChange={(open) => {
          if (!open) setScheduleTask(null);
        }}
        title="Programar tarea"
        description={scheduleTask?.title}
        size="sm"
      >
        {scheduleTask && (
          <ScheduleTaskForm
            key={`${scheduleTask.id}-${scheduleTask.startTime}-${scheduleTask.endTime}`}
            task={scheduleTask}
            onClose={() => setScheduleTask(null)}
          />
        )}
      </FormModal>
    </div>
  );
}

const WEEKDAY_LABELS = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];

function MonthCalendar({
  anchorDate,
  tasks,
  notes,
  onSelectDay,
  onCreate,
  onOpenTask,
  onOpenNote,
}: {
  anchorDate: Date;
  tasks: TaskItem[];
  notes: Note[];
  onSelectDay: (day: Date) => void;
  onCreate: (workDate: string, kind: CreateKind, hour?: number) => void;
  onOpenTask: (task: TaskItem) => void;
  onOpenNote: (note: Note) => void;
}) {
  const visibleDays = eachDayOfInterval({
    start: startOfWeek(startOfMonth(anchorDate), { weekStartsOn: 1 }),
    end: endOfWeek(endOfMonth(anchorDate), { weekStartsOn: 1 }),
  });

  return (
    <div className="min-h-0 flex-1 overflow-auto rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-[var(--shadow-card)]">
      <div className="grid grid-cols-7 border-b border-[var(--border)] bg-[var(--surface-muted)]/60">
        {WEEKDAY_LABELS.map((label) => (
          <div
            key={label}
            className="border-r border-[var(--border)] px-2 py-2 text-center text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)] last:border-r-0"
          >
            {label}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7">
        {visibleDays.map((day) => {
          const apiDate = toApiDate(day);
          const dayTasks = tasks.filter((task) => task.workDate === apiDate);
          const dayNotes = notes.filter((note) => note.workDate === apiDate);
          const entries = [
            ...dayTasks.map((task) => ({
              key: `task-${task.id}`,
              kind: "task" as const,
              label: task.title,
              time: task.startTime?.slice(0, 5) ?? "",
              item: task,
            })),
            ...dayNotes.map((note) => ({
              key: `note-${note.id}`,
              kind: "note" as const,
              label: note.title?.trim() || note.content,
              time: note.startTime?.slice(0, 5) ?? "",
              item: note,
            })),
          ].sort((a, b) =>
            (a.time || "99:99").localeCompare(b.time || "99:99")
          );
          const inMonth = isSameMonth(day, anchorDate);

          return (
            <div
              key={apiDate}
              className={cn(
                "group min-h-28 border-r border-b border-[var(--border)] p-1.5 last:border-r-0",
                !inMonth && "bg-[var(--surface-muted)]/45"
              )}
            >
              <div className="mb-1 flex items-center justify-between">
                <button
                  type="button"
                  onClick={() => onSelectDay(day)}
                  className={cn(
                    "flex size-7 items-center justify-center rounded-full text-xs font-semibold hover:bg-[var(--accent-soft)]",
                    isToday(day)
                      ? "bg-[var(--accent)] text-white hover:bg-[var(--accent-strong)]"
                      : inMonth
                        ? "text-[var(--ink)]"
                        : "text-[var(--muted)]"
                  )}
                >
                  {format(day, "d")}
                </button>
                {inMonth && (
                  <div className="hidden gap-0.5 group-hover:flex">
                    <button
                      type="button"
                      onClick={() => onCreate(apiDate, "task")}
                      className="rounded px-1 text-[10px] font-medium text-[var(--accent)] hover:bg-[var(--accent-soft)]"
                      title="Nueva tarea"
                    >
                      +T
                    </button>
                    <button
                      type="button"
                      onClick={() => onCreate(apiDate, "note")}
                      className="rounded px-1 text-[10px] font-medium text-[var(--note)] hover:bg-amber-50"
                      title="Nueva nota"
                    >
                      +N
                    </button>
                  </div>
                )}
              </div>

              <div className="flex flex-col gap-1">
                {entries.slice(0, 4).map((entry) => (
                  <button
                    key={entry.key}
                    type="button"
                    onClick={() =>
                      entry.kind === "task"
                        ? onOpenTask(entry.item)
                        : onOpenNote(entry.item)
                    }
                    className={cn(
                      "flex min-w-0 items-center gap-1 rounded px-1.5 py-0.5 text-left text-[10px] font-medium",
                      entry.kind === "task"
                        ? "bg-[var(--accent-soft)] text-[var(--accent-strong)]"
                        : "bg-amber-100 text-[var(--note)]"
                    )}
                    title={entry.label}
                  >
                    {entry.time && (
                      <span className="shrink-0 font-semibold">{entry.time}</span>
                    )}
                    <span className="truncate">{entry.label}</span>
                  </button>
                ))}
                {entries.length > 4 && (
                  <button
                    type="button"
                    onClick={() => onSelectDay(day)}
                    className="px-1 text-left text-[10px] font-medium text-[var(--muted)] hover:text-[var(--ink)]"
                  >
                    +{entries.length - 4} más
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function YearCalendar({
  anchorDate,
  tasks,
  notes,
  onSelectDay,
  onSelectMonth,
}: {
  anchorDate: Date;
  tasks: TaskItem[];
  notes: Note[];
  onSelectDay: (day: Date) => void;
  onSelectMonth: (month: Date) => void;
}) {
  const months = eachMonthOfInterval({
    start: startOfYear(anchorDate),
    end: endOfYear(anchorDate),
  });
  const taskDates = new Set(tasks.map((task) => task.workDate));
  const noteDates = new Set(
    notes.flatMap((note) => (note.workDate ? [note.workDate] : []))
  );

  return (
    <div className="min-h-0 flex-1 overflow-auto rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 shadow-[var(--shadow-card)]">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
        {months.map((month) => {
          const visibleDays = eachDayOfInterval({
            start: startOfWeek(startOfMonth(month), { weekStartsOn: 1 }),
            end: endOfWeek(endOfMonth(month), { weekStartsOn: 1 }),
          });
          return (
            <section
              key={toApiDate(month)}
              className="rounded-lg border border-[var(--border)] p-2"
            >
              <button
                type="button"
                onClick={() => onSelectMonth(month)}
                className="mb-2 text-sm font-semibold capitalize text-[var(--ink)] hover:text-[var(--accent)]"
              >
                {format(month, "MMMM", { locale: es })}
              </button>
              <div className="mb-1 grid grid-cols-7">
                {WEEKDAY_LABELS.map((label) => (
                  <span
                    key={`${toApiDate(month)}-${label}`}
                    className="text-center text-[9px] font-semibold text-[var(--muted)]"
                  >
                    {label.slice(0, 1)}
                  </span>
                ))}
              </div>
              <div className="grid grid-cols-7 gap-y-0.5">
                {visibleDays.map((day) => {
                  const apiDate = toApiDate(day);
                  const inMonth = isSameMonth(day, month);
                  if (!inMonth) {
                    return <span key={apiDate} className="h-7" />;
                  }
                  const hasTask = taskDates.has(apiDate);
                  const hasNote = noteDates.has(apiDate);
                  return (
                    <button
                      key={apiDate}
                      type="button"
                      onClick={() => onSelectDay(day)}
                      className={cn(
                        "relative mx-auto flex size-7 items-center justify-center rounded-full text-[10px] font-medium hover:bg-[var(--accent-soft)]",
                        isToday(day)
                          ? "bg-[var(--accent)] text-white"
                          : "text-[var(--ink)]"
                      )}
                    >
                      {format(day, "d")}
                      {(hasTask || hasNote) && (
                        <span className="absolute bottom-0 flex gap-0.5">
                          {hasTask && (
                            <span className="size-1 rounded-full bg-[var(--task)]" />
                          )}
                          {hasNote && (
                            <span className="size-1 rounded-full bg-[var(--note)]" />
                          )}
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}

function ScheduleTaskForm({
  task,
  onClose,
}: {
  task: TaskItem;
  onClose: () => void;
}) {
  const [startTime, setStartTime] = useState(task.startTime?.slice(0, 5) ?? "");
  const [endTime, setEndTime] = useState(task.endTime?.slice(0, 5) ?? "");
  const { update } = useTaskItemMutations(task.workDate);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if ((startTime === "") !== (endTime === "")) {
      toast.error("Indica la hora de inicio y la hora final.");
      return;
    }
    if (startTime && endTime && endTime <= startTime) {
      toast.error("La hora final debe ser posterior a la hora de inicio.");
      return;
    }

    try {
      await update.mutateAsync({
        id: task.id,
        body: {
          title: task.title,
          content: task.content,
          workDate: task.workDate,
          startTime: startTime || null,
          endTime: endTime || null,
          parentTaskId: task.parentTaskId,
          statusId: task.statusId,
          categoryId: task.categoryId,
          personId: task.personId,
          projectId: task.projectId,
          companyId: task.companyId,
          isCompleted: task.isCompleted,
          sortOrder: task.sortOrder,
          durationMinutes: task.durationMinutes,
        },
      });
      toast.success(startTime ? "Horario actualizado" : "Horario eliminado");
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudo actualizar");
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Inicio
          <input
            type="time"
            value={startTime}
            onChange={(event) => setStartTime(event.target.value)}
            className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)]"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Final
          <input
            type="time"
            value={endTime}
            onChange={(event) => setEndTime(event.target.value)}
            className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)]"
          />
        </label>
      </div>
      <p className="text-xs text-[var(--muted)]">
        Borra ambas horas para devolver la tarea a “Sin horario”.
      </p>
      <div className="flex gap-2">
        <button
          type="submit"
          disabled={update.isPending}
          className="rounded-md bg-[var(--accent)] px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
        >
          {update.isPending ? "Guardando…" : "Guardar horario"}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-3 py-1.5 text-sm text-[var(--muted)]"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}

function ScheduleNoteForm({
  note,
  onClose,
}: {
  note: Note;
  onClose: () => void;
}) {
  const [startTime, setStartTime] = useState(note.startTime?.slice(0, 5) ?? "");
  const [endTime, setEndTime] = useState(note.endTime?.slice(0, 5) ?? "");
  const { update } = useNoteMutations(note.workDate ?? "");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!note.workDate) {
      toast.error("La nota necesita una fecha para asignarle horario.");
      return;
    }
    if ((startTime === "") !== (endTime === "")) {
      toast.error("Indica la hora de inicio y la hora final.");
      return;
    }
    if (startTime && endTime && endTime <= startTime) {
      toast.error("La hora final debe ser posterior a la hora de inicio.");
      return;
    }

    try {
      await update.mutateAsync({
        id: note.id,
        body: {
          title: note.title,
          content: note.content,
          workDate: note.workDate,
          startTime: startTime || null,
          endTime: endTime || null,
          parentNoteId: note.parentNoteId,
          statusId: note.statusId,
          categoryId: note.categoryId,
          personId: note.personId,
          projectId: note.projectId,
          companyId: note.companyId,
          sortOrder: note.sortOrder,
          durationMinutes: note.durationMinutes,
        },
      });
      toast.success(startTime ? "Horario actualizado" : "Horario eliminado");
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudo actualizar");
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Inicio
          <input
            type="time"
            value={startTime}
            onChange={(event) => setStartTime(event.target.value)}
            className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--note)]"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Final
          <input
            type="time"
            value={endTime}
            onChange={(event) => setEndTime(event.target.value)}
            className="rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--note)]"
          />
        </label>
      </div>
      <p className="text-xs text-[var(--muted)]">
        Borra ambas horas para devolver la nota a “Sin horario”.
      </p>
      <div className="flex gap-2">
        <button
          type="submit"
          disabled={update.isPending}
          className="rounded-md bg-[var(--note)] px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
        >
          {update.isPending ? "Guardando…" : "Guardar horario"}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-3 py-1.5 text-sm text-[var(--muted)]"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}
