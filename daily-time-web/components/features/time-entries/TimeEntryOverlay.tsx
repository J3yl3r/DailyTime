"use client";

import { useState } from "react";
import { TimeEntryForm } from "@/components/features/time-entries/TimeEntryForm";
import { FormModal } from "@/components/shared/form-modal";

export type TimeTarget =
  | { kind: "task"; taskItemId: number; label: string }
  | { kind: "note"; noteId: number; label: string };

type Props = {
  workDate: string;
  target: TimeTarget | null;
  onClose: () => void;
};

export function TimeEntryOverlay({ workDate, target, onClose }: Props) {
  return (
    <FormModal
      open={target !== null}
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
      title="Registrar tiempo"
      description={
        target
          ? `Añade tiempo a ${target.label}.`
          : "Añade un registro de tiempo."
      }
      size="sm"
    >
      {target ? (
        <TimeEntryForm
          workDate={workDate}
          taskItemId={target.kind === "task" ? target.taskItemId : null}
          noteId={target.kind === "note" ? target.noteId : null}
          ownerLabel={target.label}
          onClose={onClose}
        />
      ) : null}
    </FormModal>
  );
}

export function useTimeTarget() {
  const [target, setTarget] = useState<TimeTarget | null>(null);

  const openForTask = (taskItemId: number, label: string) => {
    setTarget({ kind: "task", taskItemId, label });
  };

  const openForNote = (noteId: number, label: string) => {
    setTarget({ kind: "note", noteId, label });
  };

  const close = () => setTarget(null);

  return { target, openForTask, openForNote, close };
}
