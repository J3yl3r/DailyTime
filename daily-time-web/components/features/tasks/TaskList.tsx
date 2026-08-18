"use client";

import { useTaskItemsByDate } from "@/hooks/queries/use-task-items";
import { TaskItemRow } from "./TaskItemRow";

type Props = {
  workDate: string;
  onAddTime: (taskItemId: number, label: string) => void;
};

export function TaskList({ workDate, onAddTime }: Props) {
  const { data, isLoading, error } = useTaskItemsByDate(workDate);

  if (isLoading) return <p>Cargando tareas...</p>;
  if (error) return <p className="text-red-600">{String(error)}</p>;
  if (!data?.length) return <p className="text-zinc-500">Sin tareas este día.</p>;

  return (
    <ul className="flex flex-col gap-1">
      {data.map((task) => (
        <TaskItemRow
          key={task.id}
          task={task}
          workDate={workDate}
          depth={0}
          onAddTime={onAddTime}
        />
      ))}
    </ul>
  );
}