"use client";

import { useState, type ComponentProps } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";
import { useDraggable, useDroppable } from "@dnd-kit/core";
import type { TaskItem } from "@/types/api";
import { useTaskItemChildren } from "@/hooks/queries/use-task-items";
import { TaskCard } from "@/components/features/cards/TaskCard";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";

type Props = {
  task: TaskItem;
  workDate: string;
  depth?: number;
  enableHierarchyDnd?: boolean;
  onAddTime?: (taskItemId: number, label: string) => void;
  cardProps?: Pick<
    ComponentProps<typeof TaskCard>,
    "dragHandleProps" | "isDragging" | "className"
  >;
};

const MAX_DEPTH = 3;

export function TaskCardTree({
  task,
  workDate,
  depth = 1,
  enableHierarchyDnd = false,
  onAddTime,
  cardProps,
}: Props) {
  const [expanded, setExpanded] = useState(true);
  const [showChildForm, setShowChildForm] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  const childrenQuery = useTaskItemChildren(task.id, depth < MAX_DEPTH);
  const children = childrenQuery.data ?? [];
  const canAddChild = depth < MAX_DEPTH;
  const hierarchyEnabled = enableHierarchyDnd && depth > 1;
  const dragId = `task-${task.id}`;
  const dragData = {
    item: { kind: "task" as const, id: dragId, task },
  };
  const {
    attributes,
    listeners,
    setNodeRef: setDraggableNodeRef,
    isDragging,
  } = useDraggable({
    id: hierarchyEnabled ? dragId : `disabled-task-${task.id}`,
    data: dragData,
    disabled: !hierarchyEnabled,
  });
  const { setNodeRef: setDroppableNodeRef, isOver } = useDroppable({
    id: hierarchyEnabled ? dragId : `disabled-task-drop-${task.id}`,
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
        hierarchyEnabled && isOver && !isDragging && "rounded-xl ring-2 ring-[var(--accent)]"
      )}
    >
      <TaskCard
        task={task}
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
            depth === 1
              ? "border-[var(--accent-soft)]"
              : "border-[var(--border)]",
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
              ? "Cargando subtareas…"
              : `${children.length} ${
                  children.length === 1 ? "subtarea" : "subtareas"
                }`}
          </button>

          {expanded && (
            <div className="flex flex-col gap-2 pb-1">
              {children.map((child) => (
                <TaskCardTree
                  key={child.id}
                  task={child}
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
        title="Nueva subtarea"
        description={`Se agregará dentro de “${task.title}”.`}
        size="lg"
      >
        <TaskItemForm
          workDate={workDate}
          parentTaskId={task.id}
          parentLabel={task.title}
          onClose={() => setShowChildForm(false)}
        />
      </FormModal>

      <FormModal
        open={showEditForm}
        onOpenChange={setShowEditForm}
        title="Editar tarea"
        description={task.title}
        size="lg"
      >
        <TaskItemForm
          key={`edit-task-${task.id}-${task.updatedAt}`}
          workDate={workDate}
          editingId={task.id}
          isCompleted={task.isCompleted}
          parentTaskId={task.parentTaskId}
          parentLabel={undefined}
          initialValues={{
            title: task.title,
            content: task.content ?? "",
            workDate: task.workDate,
            startTime: task.startTime?.slice(0, 5) ?? null,
            endTime: task.endTime?.slice(0, 5) ?? null,
            statusId: task.statusId,
            categoryId: task.categoryId,
            personId: task.personId,
            projectId: task.projectId,
            companyId: task.companyId,
            sortOrder: task.sortOrder,
            durationMinutes: task.durationMinutes,
            parentTaskId: task.parentTaskId,
          }}
          onClose={() => setShowEditForm(false)}
        />
      </FormModal>
    </div>
  );
}
