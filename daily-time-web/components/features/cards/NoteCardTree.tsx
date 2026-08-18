"use client";

import { useState, type ComponentProps } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { useDraggable, useDroppable } from "@dnd-kit/core";
import type { Note } from "@/types/api";
import { useNoteChildren } from "@/hooks/queries/use-notes";
import { NoteCard } from "@/components/features/cards/NoteCard";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";

type Props = {
  note: Note;
  workDate: string;
  depth?: number;
  enableHierarchyDnd?: boolean;
  onAddTime?: (noteId: number, label: string) => void;
  cardProps?: Pick<
    ComponentProps<typeof NoteCard>,
    "dragHandleProps" | "isDragging" | "className"
  >;
};

const MAX_DEPTH = 3;

function noteLabel(note: Note) {
  return note.title?.trim() || note.content.slice(0, 40) || "Nota";
}

export function NoteCardTree({
  note,
  workDate,
  depth = 1,
  enableHierarchyDnd = false,
  onAddTime,
  cardProps,
}: Props) {
  const [expanded, setExpanded] = useState(true);
  const [showChildForm, setShowChildForm] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  const childrenQuery = useNoteChildren(note.id, depth < MAX_DEPTH);
  const children = childrenQuery.data ?? [];
  const canAddChild = depth < MAX_DEPTH;
  const label = noteLabel(note);
  const hierarchyEnabled = enableHierarchyDnd && depth > 1;
  const dragId = `note-${note.id}`;
  const dragData = {
    item: { kind: "note" as const, id: dragId, note },
  };
  const {
    attributes,
    listeners,
    setNodeRef: setDraggableNodeRef,
    isDragging,
  } = useDraggable({
    id: hierarchyEnabled ? dragId : `disabled-note-${note.id}`,
    data: dragData,
    disabled: !hierarchyEnabled,
  });
  const { setNodeRef: setDroppableNodeRef, isOver } = useDroppable({
    id: hierarchyEnabled ? dragId : `disabled-note-drop-${note.id}`,
    data: dragData,
    disabled: !hierarchyEnabled,
  });
  const setNodeRef = (node: HTMLDivElement | null) => {
    setDraggableNodeRef(node);
    setDroppableNodeRef(node);
  };

  return (
    <div
      ref={setNodeRef}
      className={cn(
        "min-w-0",
        hierarchyEnabled && isDragging && "opacity-0",
        hierarchyEnabled && isOver && !isDragging && "rounded-xl ring-2 ring-[var(--note)]"
      )}
    >
      <NoteCard
        note={note}
        workDate={workDate}
        depth={depth}
        onAddTime={onAddTime}
        onAddChild={canAddChild ? () => setShowChildForm(true) : undefined}
        onEdit={() => setShowEditForm(true)}
        {...cardProps}
        dragHandleProps={
          hierarchyEnabled
            ? { ...attributes, ...listeners }
            : cardProps?.dragHandleProps
        }
        isDragging={false}
      />

      {(childrenQuery.isLoading || children.length > 0) && (
        <div
          className={cn(
            "ml-4 border-l-2 pl-3",
            depth === 1 ? "border-amber-200" : "border-[var(--border)]",
          )}
        >
          <button
            type="button"
            onClick={() => setExpanded((value) => !value)}
            className="my-2 inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs font-medium text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
            aria-expanded={expanded}
          >
            {expanded ? (
              <ChevronDown className="size-3.5" />
            ) : (
              <ChevronRight className="size-3.5" />
            )}
            {childrenQuery.isLoading
              ? "Cargando subnotas…"
              : `${children.length} ${
                  children.length === 1 ? "subnota" : "subnotas"
                }`}
          </button>

          {expanded && (
            <div className="flex flex-col gap-2 pb-1">
              {children.map((child) => (
                <NoteCardTree
                  key={child.id}
                  note={child}
                  workDate={workDate}
                  depth={depth + 1}
                  enableHierarchyDnd={enableHierarchyDnd}
                  onAddTime={onAddTime}
                  cardProps={{ className: "shadow-none" }}
                />
              ))}
            </div>
          )}
        </div>
      )}

      <FormModal
        open={showChildForm}
        onOpenChange={setShowChildForm}
        title="Nueva subnota"
        description={`Se agregará dentro de “${label}”.`}
        size="lg"
      >
        <NoteForm
          workDate={workDate}
          parentNoteId={note.id}
          parentLabel={label}
          onClose={() => setShowChildForm(false)}
        />
      </FormModal>

      <FormModal
        open={showEditForm}
        onOpenChange={setShowEditForm}
        title="Editar nota"
        description={label}
        size="lg"
      >
        <NoteForm
          key={`edit-note-${note.id}-${note.updatedAt}`}
          workDate={workDate}
          editingId={note.id}
          parentNoteId={note.parentNoteId}
          initialValues={{
            title: note.title ?? "",
            content: note.content,
            workDate: note.workDate ?? workDate,
            startTime: note.startTime?.slice(0, 5) ?? null,
            endTime: note.endTime?.slice(0, 5) ?? null,
            statusId: note.statusId,
            categoryId: note.categoryId,
            personId: note.personId,
            projectId: note.projectId,
            companyId: note.companyId,
            sortOrder: note.sortOrder,
            durationMinutes: note.durationMinutes,
            parentNoteId: note.parentNoteId,
          }}
          onClose={() => setShowEditForm(false)}
        />
      </FormModal>
    </div>
  );
}
