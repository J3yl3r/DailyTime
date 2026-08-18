"use client";

import { useNotesByDate } from "@/hooks/queries/use-notes";
import { NoteRow } from "./NoteRow";

type Props = {
  workDate: string;
  onAddSubnote: (parentNoteId: number) => void;
  onAddTime: (noteId: number, label: string) => void;
};

export function NoteList({ workDate, onAddSubnote, onAddTime }: Props) {
  const { data, isLoading, error } = useNotesByDate(workDate);

  if (isLoading) return <p>Cargando notas...</p>;
  if (error) return <p className="text-red-600">{String(error)}</p>;
  if (!data?.length) return <p className="text-zinc-500">Sin notas este día.</p>;

  return (
    <ul className="flex flex-col gap-1">
      {data.map((note) => (
        <NoteRow
          key={note.id}
          note={note}
          workDate={workDate}
          depth={0}
          onAddSubnote={onAddSubnote}
          onAddTime={onAddTime}
        />
      ))}
    </ul>
  );
}