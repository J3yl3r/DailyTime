"use client";

import { useState } from "react";
import { todayApiDate } from "@/lib/utils/date";
import { DatePicker } from "./DatePicker";
import { TaskList } from "@/components/features/tasks/TaskList";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { NoteList } from "@/components/features/notes/NoteList";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { TimeEntryForm } from "@/components/features/time-entries/TimeEntryForm";
import { TimeEntryDayList } from "@/components/features/time-entries/TimeEntryDayList";

type TimeTarget =
  | { kind: "task"; taskItemId: number; label: string }
  | { kind: "note"; noteId: number; label: string };

export function DayView() {
  const [workDate, setWorkDate] = useState(todayApiDate());

  const [showTaskForm, setShowTaskForm] = useState(false);
  const [showNoteForm, setShowNoteForm] = useState(false);
  const [parentNoteId, setParentNoteId] = useState<number | null>(null);

  const [showTimeForm, setShowTimeForm] = useState(false);
  const [timeTarget, setTimeTarget] = useState<TimeTarget | null>(null);

  const openNoteForm = (parentId: number | null = null) => {
    setParentNoteId(parentId);
    setShowNoteForm(true);
  };

  const closeNoteForm = () => {
    setShowNoteForm(false);
    setParentNoteId(null);
  };

  const openTimeFormForTask = (taskItemId: number, label: string) => {
    setTimeTarget({ kind: "task", taskItemId, label });
    setShowTimeForm(true);
  };

  const openTimeFormForNote = (noteId: number, label: string) => {
    setTimeTarget({ kind: "note", noteId, label });
    setShowTimeForm(true);
  };

  const closeTimeForm = () => {
    setShowTimeForm(false);
    setTimeTarget(null);
  };

  return (
    <main className="mx-auto flex max-w-5xl flex-col gap-4 bg-white p-4 text-zinc-900">
      <h1 className="text-xl font-semibold">DailyTime</h1>
      <DatePicker workDate={workDate} onChange={setWorkDate} />

      <div className="grid gap-6 md:grid-cols-2">
        <section className="flex flex-col gap-3 rounded-lg border border-zinc-200 bg-zinc-50/50 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium">Tareas</h2>
            <button
              type="button"
              onClick={() => setShowTaskForm(true)}
              className="rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm hover:bg-zinc-50"
            >
              + Nueva tarea
            </button>
          </div>
          <TaskList workDate={workDate} onAddTime={openTimeFormForTask} />
          {showTaskForm && (
            <TaskItemForm workDate={workDate} onClose={() => setShowTaskForm(false)} />
          )}
        </section>

        <section className="flex flex-col gap-3 rounded-lg border border-zinc-200 bg-zinc-50/50 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium">Notas</h2>
            <button
              type="button"
              onClick={() => openNoteForm(null)}
              className="rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm hover:bg-zinc-50"
            >
              + Nueva nota
            </button>
          </div>
          <NoteList
            workDate={workDate}
            onAddSubnote={openNoteForm}
            onAddTime={openTimeFormForNote}
          />
          {showNoteForm && (
            <NoteForm
              workDate={workDate}
              parentNoteId={parentNoteId}
              onClose={closeNoteForm}
            />
          )}
        </section>
      </div>

      <section className="flex flex-col gap-3 rounded-lg border border-zinc-200 bg-zinc-50/50 p-4">
        <h2 className="text-lg font-medium">Tiempo del día</h2>
        <TimeEntryDayList fromDate={workDate} toDate={workDate} />
      </section>

      {showTimeForm && timeTarget && (
        <TimeEntryForm
          workDate={workDate}
          taskItemId={timeTarget.kind === "task" ? timeTarget.taskItemId : null}
          noteId={timeTarget.kind === "note" ? timeTarget.noteId : null}
          ownerLabel={timeTarget.label}
          onClose={closeTimeForm}
        />
      )}
    </main>
  );
}