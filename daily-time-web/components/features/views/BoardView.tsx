"use client";

import { useMemo, useState } from "react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  closestCorners,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  rectSortingStrategy,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { toast } from "sonner";
import type { Note, TaskItem } from "@/types/api";
import { useTaskItemsByDate } from "@/hooks/queries/use-task-items";
import { useNotesByDate } from "@/hooks/queries/use-notes";
import { useStatuses } from "@/hooks/queries/use-statuses";
import { useCategories } from "@/hooks/queries/use-categories";
import { usePeople } from "@/hooks/queries/use-people";
import { useProjects } from "@/hooks/queries/use-projects";
import { useCompanies } from "@/hooks/queries/use-companies";
import { createDefaultDateFilter, getDateFilterRange } from "@/lib/utils/date";
import { useTaskItemMutations } from "@/hooks/mutations/use-task-item-mutations";
import { useNoteMutations } from "@/hooks/mutations/use-note-mutations";
import { TaskCard } from "@/components/features/cards/TaskCard";
import { NoteCard } from "@/components/features/cards/NoteCard";
import { TaskCardTree } from "@/components/features/cards/TaskCardTree";
import { NoteCardTree } from "@/components/features/cards/NoteCardTree";
import { TaskItemForm } from "@/components/features/tasks/TaskItemForm";
import { NoteForm } from "@/components/features/notes/NoteForm";
import { ViewDateFilter } from "@/components/features/day/ViewDateFilter";
import { CreatePanel } from "@/components/ui/CreatePanel";
import {
  TimeEntryOverlay,
  useTimeTarget,
} from "@/components/features/time-entries/TimeEntryOverlay";
import { cn } from "@/lib/utils/cn";

type ShowFilter = "all" | "tasks" | "notes";
type CardLayout = "stack" | "split" | "wide";

type BoardItem =
  | { kind: "task"; id: string; task: TaskItem }
  | { kind: "note"; id: string; note: Note };

type ColumnDef = {
  id: string;
  title: string;
  accepts: ("task" | "note")[];
  statusId?: number;
  categoryId?: number;
};

function itemKey(kind: "task" | "note", id: number) {
  return `${kind}-${id}`;
}

function SortableBoardCard({
  item,
  workDate,
  onAddTimeTask,
  onAddTimeNote,
}: {
  item: BoardItem;
  workDate: string;
  onAddTimeTask: (id: number, label: string) => void;
  onAddTimeNote: (id: number, label: string) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
    isOver,
  } = useSortable({ id: item.id, data: { item } });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        isDragging && "opacity-0",
        isOver &&
          !isDragging &&
          "rounded-xl ring-2 ring-[var(--accent)] ring-offset-2"
      )}
    >
      {item.kind === "task" ? (
        <TaskCardTree
          task={item.task}
          workDate={workDate}
          enableHierarchyDnd
          onAddTime={onAddTimeTask}
          cardProps={{
            dragHandleProps: { ...attributes, ...listeners },
            isDragging: false,
          }}
        />
      ) : (
        <NoteCardTree
          note={item.note}
          workDate={workDate}
          enableHierarchyDnd
          onAddTime={onAddTimeNote}
          cardProps={{
            dragHandleProps: { ...attributes, ...listeners },
            isDragging: false,
          }}
        />
      )}
    </div>
  );
}

function BoardColumn({
  column,
  items,
  workDate,
  onAddTimeTask,
  onAddTimeNote,
  cardLayout,
}: {
  column: ColumnDef;
  items: BoardItem[];
  workDate: string;
  onAddTimeTask: (id: number, label: string) => void;
  onAddTimeNote: (id: number, label: string) => void;
  cardLayout: CardLayout;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: column.id,
    data: { columnId: column.id, accepts: column.accepts },
  });

  return (
    <section
      ref={setNodeRef}
      className={cn(
        "flex min-h-[280px] min-w-72 flex-1 flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface-muted)]/70 p-3 transition-colors",
        isOver && "border-[var(--accent)] bg-[var(--accent-soft)]/40"
      )}
    >
      <header className="flex items-center justify-between px-1">
        <h3 className="text-sm font-semibold text-[var(--ink)]">{column.title}</h3>
        <span className="rounded-md bg-[var(--surface)] px-2 py-0.5 text-xs text-[var(--muted)]">
          {items.length}
        </span>
      </header>
      <SortableContext
        items={items.map((i) => i.id)}
        strategy={rectSortingStrategy}
      >
        <div
          className={cn(
            "grid flex-1 content-start gap-2",
            cardLayout === "stack" && "grid-cols-1",
            cardLayout === "split" && "grid-cols-1 xl:grid-cols-2",
            cardLayout === "wide" &&
              "grid-cols-1 md:grid-cols-2 xl:grid-cols-3"
          )}
        >
          {items.map((item) => (
            <SortableBoardCard
              key={item.id}
              item={item}
              workDate={workDate}
              onAddTimeTask={onAddTimeTask}
              onAddTimeNote={onAddTimeNote}
            />
          ))}
          {!items.length && (
            <p className="col-span-full px-1 py-6 text-center text-xs text-[var(--muted)]">
              Arrastra tarjetas aquí
            </p>
          )}
        </div>
      </SortableContext>
    </section>
  );
}

function columnForItem(item: BoardItem): string {
  return item.kind === "task" ? "tasks" : "notes";
}

export function BoardView() {
  const [dateFilter, setDateFilter] = useState(createDefaultDateFilter);
  const { fromDate, toDate, anchorDate: workDate } = getDateFilterRange(dateFilter);
  const tasksQuery = useTaskItemsByDate(fromDate, toDate);
  const notesQuery = useNotesByDate(fromDate, toDate);
  const taskStatuses = useStatuses("task");
  const noteStatuses = useStatuses("note");
  const taskCategories = useCategories("task");
  const noteCategories = useCategories("note");
  const people = usePeople(true);
  const projects = useProjects(true);
  const companies = useCompanies(true);
  const taskMutations = useTaskItemMutations(workDate);
  const noteMutations = useNoteMutations(workDate);
  const time = useTimeTarget();

  const [showFilter, setShowFilter] = useState<ShowFilter>("all");
  const [companyFilter, setCompanyFilter] = useState<number | "all">("all");
  const [projectFilter, setProjectFilter] = useState<number | "all">("all");
  const [personFilter, setPersonFilter] = useState<number | "all">("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [categoryFilter, setCategoryFilter] = useState("all");
  const [activeId, setActiveId] = useState<string | null>(null);
  const [activeDragItem, setActiveDragItem] = useState<BoardItem | null>(null);
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [showNoteForm, setShowNoteForm] = useState(false);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } })
  );

  const columns: ColumnDef[] = useMemo(() => {
    const cols: ColumnDef[] = [];
    if (showFilter !== "notes") {
      cols.push({ id: "tasks", title: "Tareas", accepts: ["task"] });
    }
    if (showFilter !== "tasks") {
      cols.push({ id: "notes", title: "Notas", accepts: ["note"] });
    }
    return cols;
  }, [showFilter]);

  const boardItems = useMemo(() => {
    const items: BoardItem[] = [];
    if (showFilter !== "notes") {
      for (const task of tasksQuery.data ?? []) {
        if (companyFilter !== "all" && task.companyId !== companyFilter) continue;
        if (projectFilter !== "all" && task.projectId !== projectFilter) continue;
        if (personFilter !== "all" && task.personId !== personFilter) continue;
        if (
          statusFilter !== "all" &&
          statusFilter !== `task-${task.statusId}`
        )
          continue;
        if (
          categoryFilter !== "all" &&
          categoryFilter !== `task-${task.categoryId}`
        )
          continue;
        items.push({ kind: "task", id: itemKey("task", task.id), task });
      }
    }
    if (showFilter !== "tasks") {
      for (const note of notesQuery.data ?? []) {
        if (companyFilter !== "all" && note.companyId !== companyFilter) continue;
        if (projectFilter !== "all" && note.projectId !== projectFilter) continue;
        if (personFilter !== "all" && note.personId !== personFilter) continue;
        if (
          statusFilter !== "all" &&
          statusFilter !== `note-${note.statusId}`
        )
          continue;
        if (
          categoryFilter !== "all" &&
          categoryFilter !== `note-${note.categoryId}`
        )
          continue;
        items.push({ kind: "note", id: itemKey("note", note.id), note });
      }
    }
    return items.sort((a, b) => {
      const orderA = a.kind === "task" ? a.task.sortOrder : a.note.sortOrder;
      const orderB = b.kind === "task" ? b.task.sortOrder : b.note.sortOrder;
      return orderA - orderB;
    });
  }, [
    tasksQuery.data,
    notesQuery.data,
    showFilter,
    companyFilter,
    projectFilter,
    personFilter,
    statusFilter,
    categoryFilter,
  ]);

  const itemsByColumn = useMemo(() => {
    const map: Record<string, BoardItem[]> = {};
    for (const col of columns) map[col.id] = [];
    for (const item of boardItems) {
      const colId = columnForItem(item);
      if (map[colId]) map[colId].push(item);
    }
    return map;
  }, [boardItems, columns]);

  const activeItem =
    activeDragItem ?? boardItems.find((i) => i.id === activeId) ?? null;

  const findColumnOf = (id: string) => {
    for (const col of columns) {
      if (itemsByColumn[col.id]?.some((i) => i.id === id)) return col.id;
    }
    if (columns.some((c) => c.id === id)) return id;
    return null;
  };

  const onDragStart = (event: DragStartEvent) => {
    setActiveId(String(event.active.id));
    setActiveDragItem((event.active.data.current?.item as BoardItem) ?? null);
  };

  const moveHierarchyItem = async (
    item: BoardItem,
    newParentId: number | null,
    targetColumn?: ColumnDef
  ) => {
    if (item.kind === "task") {
      const statusId = targetColumn?.statusId ?? item.task.statusId;
      const categoryId = targetColumn?.categoryId ?? item.task.categoryId;
      const status = taskStatuses.data?.find((value) => value.id === statusId);
      await taskMutations.update.mutateAsync({
        id: item.task.id,
        body: {
          title: item.task.title,
          content: item.task.content,
          workDate: item.task.workDate,
          startTime: item.task.startTime?.slice(0, 5) ?? null,
          endTime: item.task.endTime?.slice(0, 5) ?? null,
          parentTaskId: newParentId,
          statusId,
          categoryId,
          personId: item.task.personId,
          projectId: item.task.projectId,
          companyId: item.task.companyId,
          isCompleted: status?.isFinal ?? item.task.isCompleted,
          sortOrder: item.task.sortOrder,
          durationMinutes: item.task.durationMinutes,
        },
      });
      return;
    }

    await noteMutations.update.mutateAsync({
      id: item.note.id,
      body: {
        title: item.note.title,
        content: item.note.content,
        workDate: item.note.workDate,
        startTime: item.note.startTime?.slice(0, 5) ?? null,
        endTime: item.note.endTime?.slice(0, 5) ?? null,
        parentNoteId: newParentId,
        statusId: targetColumn?.statusId ?? item.note.statusId,
        categoryId: targetColumn?.categoryId ?? item.note.categoryId,
        personId: item.note.personId,
        projectId: item.note.projectId,
        companyId: item.note.companyId,
        sortOrder: item.note.sortOrder,
        durationMinutes: item.note.durationMinutes,
      },
    });
  };

  const persistOrder = async (
    kind: "task" | "note",
    ordered: BoardItem[],
    options?: { movedId?: string; statusId?: number; categoryId?: number }
  ) => {
    const updates = ordered.map((item, index) => {
      if (kind === "task" && item.kind === "task") {
        const isMoved = options?.movedId === item.id;
        const statusId =
          isMoved && options?.statusId !== undefined
            ? options.statusId
            : item.task.statusId;
        const categoryId =
          isMoved && options?.categoryId !== undefined
            ? options.categoryId
            : item.task.categoryId;
        const status = taskStatuses.data?.find((s) => s.id === statusId);
        return taskMutations.update.mutateAsync({
          id: item.task.id,
          body: {
            title: item.task.title,
            content: item.task.content,
            workDate: item.task.workDate,
            startTime: item.task.startTime?.slice(0, 5) ?? null,
            endTime: item.task.endTime?.slice(0, 5) ?? null,
            parentTaskId: item.task.parentTaskId,
            statusId,
            categoryId,
            personId: item.task.personId,
            projectId: item.task.projectId,
            companyId: item.task.companyId,
            isCompleted: status?.isFinal ?? item.task.isCompleted,
            sortOrder: index,
            durationMinutes: item.task.durationMinutes,
          },
        });
      }
      if (kind === "note" && item.kind === "note") {
        const isMoved = options?.movedId === item.id;
        const statusId =
          isMoved && options?.statusId !== undefined
            ? options.statusId
            : item.note.statusId;
        const categoryId =
          isMoved && options?.categoryId !== undefined
            ? options.categoryId
            : item.note.categoryId;
        return noteMutations.update.mutateAsync({
          id: item.note.id,
          body: {
            title: item.note.title,
            content: item.note.content,
            workDate: item.note.workDate,
            startTime: item.note.startTime?.slice(0, 5) ?? null,
            endTime: item.note.endTime?.slice(0, 5) ?? null,
            parentNoteId: item.note.parentNoteId,
            statusId,
            categoryId,
            personId: item.note.personId,
            projectId: item.note.projectId,
            companyId: item.note.companyId,
            sortOrder: index,
            durationMinutes: item.note.durationMinutes,
          },
        });
      }
      return Promise.resolve();
    });
    await Promise.all(updates);
  };

  const onDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveId(null);
    setActiveDragItem(null);
    if (!over) return;

    const activeItemData = active.data.current?.item as BoardItem | undefined;
    if (!activeItemData) return;

    const overItemData = over.data.current?.item as BoardItem | undefined;
    if (overItemData) {
      if (overItemData.id === activeItemData.id) return;
      if (overItemData.kind !== activeItemData.kind) {
        toast.error("Solo puedes relacionar elementos del mismo tipo.");
        return;
      }

      try {
        const parentId =
          overItemData.kind === "task"
            ? overItemData.task.id
            : overItemData.note.id;
        await moveHierarchyItem(activeItemData, parentId);
        toast.success(
          activeItemData.kind === "task"
            ? "Tarea convertida en subtarea"
            : "Nota convertida en subnota"
        );
      } catch (error) {
        toast.error(
          error instanceof Error ? error.message : "No se pudo cambiar la jerarquía"
        );
      }
      return;
    }

    let toCol = findColumnOf(String(over.id));
    if (!toCol) toCol = String(over.id);
    if (!toCol) return;

    const targetColumn = columns.find((c) => c.id === toCol);
    if (!targetColumn) return;
    if (!targetColumn.accepts.includes(activeItemData.kind)) {
      toast.error(
        activeItemData.kind === "task"
          ? "Las tareas no van en esa columna"
          : "Las notas no van en esa columna"
      );
      return;
    }

    const currentParentId =
      activeItemData.kind === "task"
        ? activeItemData.task.parentTaskId
        : activeItemData.note.parentNoteId;
    if (currentParentId != null) {
      try {
        await moveHierarchyItem(activeItemData, null, targetColumn);
        toast.success(
          activeItemData.kind === "task"
            ? "Tarea movida al nivel principal"
            : "Nota movida al nivel principal"
        );
      } catch (error) {
        toast.error(
          error instanceof Error ? error.message : "No se pudo cambiar la jerarquía"
        );
      }
      return;
    }

    const fromCol = findColumnOf(String(active.id));
    if (!fromCol) return;

    const sourceItems = [...(itemsByColumn[fromCol] ?? [])];
    const destItems =
      fromCol === toCol ? sourceItems : [...(itemsByColumn[toCol] ?? [])];

    const fromIndex = sourceItems.findIndex((i) => i.id === String(active.id));
    if (fromIndex < 0) return;

    let toIndex = destItems.findIndex((i) => i.id === String(over.id));
    if (toIndex < 0) toIndex = destItems.length;

    try {
      if (fromCol === toCol) {
        const reordered = arrayMove(sourceItems, fromIndex, toIndex);
        if (activeItemData.kind === "task") {
          await persistOrder(
            "task",
            reordered.filter((i) => i.kind === "task")
          );
        } else {
          await persistOrder(
            "note",
            reordered.filter((i) => i.kind === "note")
          );
        }
        toast.success("Orden actualizado");
        return;
      }

      const [moved] = sourceItems.splice(fromIndex, 1);
      destItems.splice(toIndex, 0, moved);

      if (activeItemData.kind === "task") {
        await persistOrder("task", destItems.filter((i) => i.kind === "task"), {
          movedId: activeItemData.id,
          statusId: targetColumn.statusId,
          categoryId: targetColumn.categoryId,
        });
        await persistOrder(
          "task",
          sourceItems.filter((i) => i.kind === "task")
        );
      } else {
        await persistOrder("note", destItems.filter((i) => i.kind === "note"), {
          movedId: activeItemData.id,
          statusId: targetColumn.statusId,
          categoryId: targetColumn.categoryId,
        });
        await persistOrder(
          "note",
          sourceItems.filter((i) => i.kind === "note")
        );
      }
      toast.success("Tarjeta movida");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "No se pudo actualizar");
    }
  };

  const loading =
    tasksQuery.isLoading ||
    notesQuery.isLoading ||
    taskStatuses.isLoading ||
    noteStatuses.isLoading ||
    taskCategories.isLoading ||
    noteCategories.isLoading;
  const error =
    tasksQuery.error ||
    notesQuery.error ||
    taskStatuses.error ||
    noteStatuses.error ||
    taskCategories.error ||
    noteCategories.error;

  const cardLayout: CardLayout =
    showFilter === "all" ? "split" : "wide";

  return (
    <div className="flex flex-col gap-4">
      <ViewDateFilter value={dateFilter} onChange={setDateFilter} />

      <section className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)]/80 px-4 py-3 shadow-[var(--shadow-card)] lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap items-center gap-3">
          <label className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
            Empresa
            <select
              value={companyFilter === "all" ? "all" : String(companyFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setCompanyFilter(value === "all" ? "all" : Number(value));
              }}
              className="h-9 min-w-[9rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)]"
              disabled={companies.isLoading}
            >
              <option value="all">Todas</option>
              {companies.data?.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
            Proyecto
            <select
              value={projectFilter === "all" ? "all" : String(projectFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setProjectFilter(value === "all" ? "all" : Number(value));
              }}
              className="h-9 min-w-[9rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)]"
              disabled={projects.isLoading}
            >
              <option value="all">Todos</option>
              {projects.data?.map((project) => (
                <option key={project.id} value={project.id}>
                  {project.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
            Persona
            <select
              value={personFilter === "all" ? "all" : String(personFilter)}
              onChange={(event) => {
                const value = event.target.value;
                setPersonFilter(value === "all" ? "all" : Number(value));
              }}
              className="h-9 min-w-[9rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)]"
              disabled={people.isLoading}
            >
              <option value="all">Todas</option>
              {people.data?.map((person) => (
                <option key={person.id} value={person.id}>
                  {person.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
            Estado
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value)}
              className="h-9 min-w-[9rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)]"
            >
              <option value="all">Todos</option>
              {showFilter !== "notes" &&
                taskStatuses.data?.map((status) => (
                  <option key={`task-${status.id}`} value={`task-${status.id}`}>
                    {status.name}
                  </option>
                ))}
              {showFilter !== "tasks" &&
                noteStatuses.data?.map((status) => (
                  <option key={`note-${status.id}`} value={`note-${status.id}`}>
                    {status.name}
                  </option>
                ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-xs font-medium text-[var(--muted)]">
            Categoría
            <select
              value={categoryFilter}
              onChange={(event) => setCategoryFilter(event.target.value)}
              className="h-9 min-w-[9rem] rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)]"
            >
              <option value="all">Todas</option>
              {showFilter !== "notes" &&
                taskCategories.data?.map((category) => (
                  <option
                    key={`task-${category.id}`}
                    value={`task-${category.id}`}
                  >
                    {category.name}
                  </option>
                ))}
              {showFilter !== "tasks" &&
                noteCategories.data?.map((category) => (
                  <option
                    key={`note-${category.id}`}
                    value={`note-${category.id}`}
                  >
                    {category.name}
                  </option>
                ))}
            </select>
          </label>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <CreatePanel
            open={showTaskForm}
            onOpen={() => {
              setShowTaskForm(true);
              if (showFilter === "notes") setShowFilter("all");
              setCompanyFilter("all");
              setProjectFilter("all");
              setPersonFilter("all");
              setStatusFilter("all");
              setCategoryFilter("all");
            }}
            onClose={() => setShowTaskForm(false)}
            label="Nueva tarea"
            modalSize="lg"
          >
            <TaskItemForm
              key={`board-task-${workDate}`}
              workDate={workDate}
              onClose={() => setShowTaskForm(false)}
            />
          </CreatePanel>
          <CreatePanel
            open={showNoteForm}
            onOpen={() => {
              setShowNoteForm(true);
              if (showFilter === "tasks") setShowFilter("all");
              setCompanyFilter("all");
              setProjectFilter("all");
              setPersonFilter("all");
              setStatusFilter("all");
              setCategoryFilter("all");
            }}
            onClose={() => setShowNoteForm(false)}
            label="Nueva nota"
            modalSize="lg"
          >
            <NoteForm
              key={`board-note-${workDate}`}
              workDate={workDate}
              onClose={() => setShowNoteForm(false)}
            />
          </CreatePanel>
        </div>
      </section>

      <div className="flex flex-wrap items-center gap-1 rounded-lg border border-[var(--border)] bg-[var(--surface)] p-1 w-fit">
        {(
          [
            ["all", "Todo"],
            ["tasks", "Solo tareas"],
            ["notes", "Solo notas"],
          ] as const
        ).map(([value, label]) => (
          <button
            key={value}
            type="button"
            onClick={() => {
              setShowFilter(value);
              setStatusFilter("all");
              setCategoryFilter("all");
            }}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm transition-colors",
              showFilter === value
                ? "bg-[var(--accent-soft)] text-[var(--accent)]"
                : "text-[var(--muted)] hover:text-[var(--ink)]"
            )}
          >
            {label}
          </button>
        ))}
      </div>

      {loading && <p className="text-sm text-[var(--muted)]">Cargando tablero…</p>}
      {error && (
        <p className="text-sm text-[var(--danger)]">{String(error)}</p>
      )}

      {!loading && !error && (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCorners}
          onDragStart={onDragStart}
          onDragEnd={onDragEnd}
          onDragCancel={() => {
            setActiveId(null);
            setActiveDragItem(null);
          }}
        >
          <div className="flex w-full gap-4 overflow-x-auto pb-3">
            {columns.map((column) => (
              <BoardColumn
                key={column.id}
                column={column}
                items={itemsByColumn[column.id] ?? []}
                workDate={workDate}
                onAddTimeTask={time.openForTask}
                onAddTimeNote={time.openForNote}
                cardLayout={cardLayout}
              />
            ))}
          </div>
          <DragOverlay dropAnimation={null}>
            {activeItem?.kind === "task" ? (
              <TaskCard
                task={activeItem.task}
                workDate={workDate}
                className="w-64 scale-95 shadow-lg"
              />
            ) : activeItem?.kind === "note" ? (
              <NoteCard
                note={activeItem.note}
                workDate={workDate}
                className="w-64 scale-95 shadow-lg"
              />
            ) : null}
          </DragOverlay>
        </DndContext>
      )}

      <TimeEntryOverlay
        workDate={workDate}
        target={time.target}
        onClose={time.close}
      />
    </div>
  );
}
