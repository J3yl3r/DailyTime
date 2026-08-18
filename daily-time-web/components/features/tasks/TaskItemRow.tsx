"use client";

import { useState } from "react";
import type { TaskItem } from "@/types/api";
import { useTaskItemChildren } from "@/hooks/queries/use-task-items";
import { useTaskItemMutations } from "@/hooks/mutations/use-task-item-mutations";
import { formatDurationMinutes } from "@/lib/utils/time";
import { useConfirm } from "@/providers/confirm-provider";
import { toast } from "sonner";

type Props = {
  task: TaskItem;
  workDate: string;
  depth: number;
  onAddTime: (taskItemId: number, label: string) => void;
};

export function TaskItemRow({ task, workDate, depth, onAddTime }: Props) {
  const [expanded, setExpanded] = useState(false);
  const { data: children } = useTaskItemChildren(task.id, expanded);
  const { update, remove } = useTaskItemMutations(workDate);
  const confirm = useConfirm();

  const toggleComplete = () => {
    update.mutate({
      id: task.id,
        body: {
          title: task.title,
          content: task.content,
          workDate: task.workDate,
          startTime: task.startTime?.slice(0, 5) ?? null,
          endTime: task.endTime?.slice(0, 5) ?? null,
          parentTaskId: task.parentTaskId,
          statusId: task.statusId,
          categoryId: task.categoryId,
          personId: task.personId,
          projectId: task.projectId,
          companyId: task.companyId,
          isCompleted: !task.isCompleted,
          sortOrder: task.sortOrder,
          durationMinutes: task.durationMinutes,
        },
    });
  };

  const handleDelete = async () => {
    const ok = await confirm({
      title: "Eliminar tarea",
      description: `¿Estás seguro de eliminar “${task.title}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    remove.mutate(task.id, {
      onSuccess: () => toast.success("Tarea eliminada"),
      onError: (e) => toast.error(e.message),
    });
  };

  return (
    <li>
      <div
        className="flex items-center gap-2 rounded-md border border-zinc-200 bg-white px-2 py-1"
        style={{ marginLeft: depth * 16 }}
      >
        <button type="button" onClick={() => setExpanded((v) => !v)} className="text-xs">
          {expanded ? "▼" : "▶"}
        </button>
        <input
          type="checkbox"
          checked={task.isCompleted}
          onChange={toggleComplete}
          disabled={update.isPending}
        />
        <span className={`flex-1 ${task.isCompleted ? "line-through text-zinc-400" : ""}`}>
          {task.title}
        </span>
        {task.durationMinutes > 0 && (
          <span className="text-xs text-zinc-500">{formatDurationMinutes(task.durationMinutes)}</span>
        )}
        <button
          type="button"
          onClick={() => onAddTime(task.id, task.title)}
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
      {expanded && !!children?.length && (
        <ul className="flex flex-col gap-1">
          {children.map((child) => (
            <TaskItemRow
              key={child.id}
              task={child}
              workDate={workDate}
              depth={depth + 1}
              onAddTime={onAddTime}
            />
          ))}
        </ul>
      )}
    </li>
  );
}