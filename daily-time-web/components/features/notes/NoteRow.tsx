"use client";

import { useState } from "react";
import type { Note } from "@/types/api";
import { useNoteChildren } from "@/hooks/queries/use-notes";
import { useNoteMutations } from "@/hooks/mutations/use-note-mutations";
import { formatDurationMinutes } from "@/lib/utils/time";
import { useConfirm } from "@/providers/confirm-provider";
import { toast } from "sonner";

type Props = {
  note: Note;
  workDate: string;
  depth: number;
  onAddSubnote: (parentNoteId: number) => void;
  onAddTime: (noteId: number, label: string) => void;
};

function noteLabel(note: Note): string {
  if (note.title?.trim()) return note.title.trim();
  const preview = note.content.trim();
  return preview.length > 40 ? `${preview.slice(0, 40)}…` : preview;
}

export function NoteRow({
  note,
  workDate,
  depth,
  onAddSubnote,
  onAddTime,
}: Props) {
  const [expanded, setExpanded] = useState(false);
  const { data: children } = useNoteChildren(note.id, expanded);
  const { remove } = useNoteMutations(workDate);
  const confirm = useConfirm();

  const canAddSubnote = depth < 2;

  const handleDelete = async () => {
    const label = noteLabel(note) || "Nota";
    const ok = await confirm({
      title: "Eliminar nota",
      description: `¿Estás seguro de eliminar “${label}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(note.id, {
      onSuccess: () => toast.success("Nota eliminada"),
      onError: (e) => toast.error(e.message),
    });
  };

  return (
    <li>
      <div
        className="flex items-start gap-2 rounded-md border border-zinc-200 bg-white px-2 py-2"
        style={{ marginLeft: depth * 16 }}
      >
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="mt-1 text-xs"
        >
          {expanded ? "▼" : "▶"}
        </button>

        <div className="flex min-w-0 flex-1 flex-col gap-0.5">
          {note.title && (
            <span className="text-sm font-medium">{note.title}</span>
          )}
          <span className="whitespace-pre-wrap text-sm text-zinc-700">
            {note.content}
          </span>
          {note.durationMinutes > 0 && (
            <span className="text-xs text-zinc-500">
              {formatDurationMinutes(note.durationMinutes)}
            </span>
          )}
        </div>

        <div className="flex shrink-0 flex-col items-end gap-1">
          {canAddSubnote && (
            <button
              type="button"
              onClick={() => onAddSubnote(note.id)}
              className="text-xs text-blue-600 hover:underline"
            >
              + Sub
            </button>
          )}
          <button
            type="button"
            onClick={() => onAddTime(note.id, noteLabel(note))}
            className="text-xs text-blue-600 hover:underline"
          >
            + Tiempo
          </button>
          <button
            type="button"
            onClick={handleDelete}
            className="text-xs text-red-600 hover:underline"
            disabled={remove.isPending}
          >
            Eliminar
          </button>
        </div>
      </div>

      {expanded && !!children?.length && (
        <ul className="flex flex-col gap-1">
          {children.map((child) => (
            <NoteRow
              key={child.id}
              note={child}
              workDate={workDate}
              depth={depth + 1}
              onAddSubnote={onAddSubnote}
              onAddTime={onAddTime}
            />
          ))}
        </ul>
      )}
    </li>
  );
}