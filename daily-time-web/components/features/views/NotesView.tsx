"use client";

import { useState } from "react";
import { useNotesByDate } from "@/hooks/queries/use-notes";
import { createDefaultDateFilter, getDateFilterRange } from "@/lib/utils/date";
import { NoteCardTree } from "@/components/features/cards/NoteCardTree";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { ViewDateFilter } from "@/components/features/day/ViewDateFilter";
import { CreatePanel } from "@/components/ui/CreatePanel";
import {
  TimeEntryOverlay,
  useTimeTarget,
} from "@/components/features/time-entries/TimeEntryOverlay";

export function NotesView() {
  const [dateFilter, setDateFilter] = useState(createDefaultDateFilter);
  const { fromDate, toDate, anchorDate: workDate } = getDateFilterRange(dateFilter);
  const { data, isLoading, error } = useNotesByDate(fromDate, toDate);
  const [showForm, setShowForm] = useState(false);
  const time = useTimeTarget();

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <p className="text-sm text-[var(--muted)]">
          Notas del periodo como tarjetas para revisarlas rápido.
        </p>
        <CreatePanel
          open={showForm}
          onOpen={() => setShowForm(true)}
          onClose={() => setShowForm(false)}
          label="Nueva nota"
          modalSize="lg"
          className="sm:max-w-sm sm:items-end"
        >
          <NoteForm workDate={workDate} onClose={() => setShowForm(false)} />
        </CreatePanel>
      </div>

      <ViewDateFilter value={dateFilter} onChange={setDateFilter} />

      {isLoading && <p className="text-sm text-[var(--muted)]">Cargando notas…</p>}
      {error && <p className="text-sm text-[var(--danger)]">{String(error)}</p>}
      {!isLoading && !error && !data?.length && (
        <p className="rounded-xl border border-dashed border-[var(--border)] bg-[var(--surface)]/60 px-4 py-10 text-center text-sm text-[var(--muted)]">
          No hay notas para este periodo. Crea la primera con el botón de arriba.
        </p>
      )}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {data?.map((note) => (
          <NoteCardTree
            key={note.id}
            note={note}
            workDate={workDate}
            onAddTime={time.openForNote}
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
