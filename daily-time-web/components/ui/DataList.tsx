"use client";

import {
  type ButtonHTMLAttributes,
  type ReactNode,
  type MouseEvent,
} from "react";
import {
  DndContext,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical } from "lucide-react";
import { cn } from "@/lib/utils/cn";

type DataListProps<T> = {
  items: T[];
  getKey: (item: T) => string | number;
  emptyMessage?: string;
  className?: string;
  /** Toolbar above the list (e.g. select-all). */
  header?: ReactNode;
  /** Sticky bar when some rows are selected. */
  selectionBar?: ReactNode;
  selected?: boolean | ((item: T) => boolean);
  onToggleSelect?: (item: T) => void;
  selectAriaLabel?: (item: T) => string;
  title: (item: T) => ReactNode;
  badge?: (item: T) => ReactNode;
  meta?: (item: T) => ReactNode;
  description?: (item: T) => ReactNode;
  actions?: (item: T) => ReactNode;
  /** Enables Drag and Drop reordering */
  isSortable?: boolean;
  /** Callback fired when items are reordered via Drag and Drop */
  onReorder?: (
    newItems: T[],
    movedItem: T,
    oldIndex: number,
    newIndex: number
  ) => void;
};

type RowProps<T> = {
  item: T;
  itemKey: string | number;
  checked: boolean;
  onToggleSelect?: (item: T) => void;
  selectAriaLabel?: (item: T) => string;
  title: (item: T) => ReactNode;
  badge?: (item: T) => ReactNode;
  meta?: (item: T) => ReactNode;
  description?: (item: T) => ReactNode;
  actions?: (item: T) => ReactNode;
  isSortable?: boolean;
};

function DataListRow<T>({
  item,
  checked,
  onToggleSelect,
  selectAriaLabel,
  title,
  badge,
  meta,
  description,
  actions,
  isSortable,
}: RowProps<T>) {
  const handleRowClick = (event: MouseEvent<HTMLLIElement>) => {
    if (!onToggleSelect) return;
    const target = event.target as HTMLElement | null;
    if (
      target?.closest(
        "button, a, input, select, textarea, [data-prevent-row-click]"
      )
    ) {
      return;
    }
    onToggleSelect(item);
  };

  return (
    <li
      onClick={handleRowClick}
      className={cn(
        "px-3 py-2.5 transition-colors sm:px-4",
        onToggleSelect && "cursor-pointer hover:bg-[var(--surface-muted)]/50",
        checked &&
          "bg-[var(--accent-soft)]/40 ring-1 ring-inset ring-[var(--accent)]/30"
      )}
    >
      <div className="flex flex-wrap items-start justify-between gap-2 sm:gap-3">
        <div className="flex min-w-0 flex-1 items-start gap-2.5">
          {isSortable ? (
            <span
              data-prevent-row-click
              className="mt-1 flex size-5 cursor-grab items-center justify-center text-[var(--muted)] hover:text-[var(--ink)] active:cursor-grabbing"
              title="Arrastrar para ordenar"
            >
              <GripVertical className="size-4" />
            </span>
          ) : null}

          {onToggleSelect ? (
            <input
              type="checkbox"
              className="mt-1 size-4 shrink-0 cursor-pointer accent-[var(--accent)]"
              checked={checked}
              onChange={() => onToggleSelect(item)}
              onClick={(e) => e.stopPropagation()}
              aria-label={selectAriaLabel?.(item) ?? "Seleccionar"}
            />
          ) : null}

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <div className="min-w-0 font-medium text-[var(--ink)]">
                {title(item)}
              </div>
              {badge?.(item)}
            </div>
            {meta ? (
              <p className="mt-0.5 text-sm text-[var(--muted)]">{meta(item)}</p>
            ) : null}
            {description ? (
              <div className="mt-1 text-sm text-[var(--muted)]">
                {description(item)}
              </div>
            ) : null}
          </div>
        </div>

        {actions ? (
          <div
            data-prevent-row-click
            className="flex shrink-0 flex-wrap items-center gap-0.5"
          >
            {actions(item)}
          </div>
        ) : null}
      </div>
    </li>
  );
}

function SortableDataListRow<T>(props: RowProps<T>) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: String(props.itemKey),
    data: { item: props.item },
  });

  const style = {
    transform: CSS.Translate.toString(transform),
    transition,
  };

  const handleRowClick = (event: MouseEvent<HTMLLIElement>) => {
    if (!props.onToggleSelect) return;
    const target = event.target as HTMLElement | null;
    if (
      target?.closest(
        "button, a, input, select, textarea, [data-prevent-row-click]"
      )
    ) {
      return;
    }
    props.onToggleSelect(props.item);
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      onClick={handleRowClick}
      className={cn(
        "px-3 py-2.5 transition-colors sm:px-4",
        props.onToggleSelect &&
          "cursor-pointer hover:bg-[var(--surface-muted)]/50",
        props.checked &&
          "bg-[var(--accent-soft)]/40 ring-1 ring-inset ring-[var(--accent)]/30",
        isDragging &&
          "z-20 opacity-60 shadow-lg ring-2 ring-[var(--accent)] bg-[var(--surface)]"
      )}
    >
      <div className="flex flex-wrap items-start justify-between gap-2 sm:gap-3">
        <div className="flex min-w-0 flex-1 items-start gap-2.5">
          <button
            type="button"
            data-prevent-row-click
            className="mt-0.5 -ml-1 flex size-6 cursor-grab items-center justify-center rounded text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)] active:cursor-grabbing touch-none"
            title="Arrastrar para ordenar"
            aria-label="Arrastrar para ordenar"
            {...attributes}
            {...listeners}
          >
            <GripVertical className="size-4" />
          </button>

          {props.onToggleSelect ? (
            <input
              type="checkbox"
              className="mt-1 size-4 shrink-0 cursor-pointer accent-[var(--accent)]"
              checked={props.checked}
              onChange={() => props.onToggleSelect?.(props.item)}
              onClick={(e) => e.stopPropagation()}
              aria-label={props.selectAriaLabel?.(props.item) ?? "Seleccionar"}
            />
          ) : null}

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <div className="min-w-0 font-medium text-[var(--ink)]">
                {props.title(props.item)}
              </div>
              {props.badge?.(props.item)}
            </div>
            {props.meta ? (
              <p className="mt-0.5 text-sm text-[var(--muted)]">
                {props.meta(props.item)}
              </p>
            ) : null}
            {props.description ? (
              <div className="mt-1 text-sm text-[var(--muted)]">
                {props.description(props.item)}
              </div>
            ) : null}
          </div>
        </div>

        {props.actions ? (
          <div
            data-prevent-row-click
            className="flex shrink-0 flex-wrap items-center gap-0.5"
          >
            {props.actions(props.item)}
          </div>
        ) : null}
      </div>
    </li>
  );
}

/**
 * Lista plana reutilizable con soporte para selección por clic en tarjeta
 * y reordenamiento Drag and Drop opcional con persistencia.
 */
export function DataList<T>({
  items,
  getKey,
  emptyMessage = "Sin elementos.",
  className,
  header,
  selectionBar,
  selected,
  onToggleSelect,
  selectAriaLabel,
  title,
  badge,
  meta,
  description,
  actions,
  isSortable = false,
  onReorder,
}: DataListProps<T>) {
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    })
  );

  if (items.length === 0) {
    return (
      <p className="rounded-lg border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
        {emptyMessage}
      </p>
    );
  }

  const isSelected = (item: T) =>
    typeof selected === "function" ? selected(item) : Boolean(selected);

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = items.findIndex(
      (item) => String(getKey(item)) === String(active.id)
    );
    const newIndex = items.findIndex(
      (item) => String(getKey(item)) === String(over.id)
    );

    if (oldIndex !== -1 && newIndex !== -1) {
      const reordered = arrayMove(items, oldIndex, newIndex);
      onReorder?.(reordered, items[oldIndex], oldIndex, newIndex);
    }
  };

  const itemKeys = items.map((item) => String(getKey(item)));

  const content = (
    <ul className="divide-y divide-[var(--border)] overflow-hidden rounded-lg border border-[var(--border)] bg-[var(--surface)]">
      {items.map((item) => {
        const key = getKey(item);
        const checked = isSelected(item);

        if (isSortable) {
          return (
            <SortableDataListRow
              key={key}
              item={item}
              itemKey={key}
              checked={checked}
              onToggleSelect={onToggleSelect}
              selectAriaLabel={selectAriaLabel}
              title={title}
              badge={badge}
              meta={meta}
              description={description}
              actions={actions}
              isSortable
            />
          );
        }

        return (
          <DataListRow
            key={key}
            item={item}
            itemKey={key}
            checked={checked}
            onToggleSelect={onToggleSelect}
            selectAriaLabel={selectAriaLabel}
            title={title}
            badge={badge}
            meta={meta}
            description={description}
            actions={actions}
          />
        );
      })}
    </ul>
  );

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      {header}
      {selectionBar}

      {isSortable ? (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <SortableContext
            items={itemKeys}
            strategy={verticalListSortingStrategy}
          >
            {content}
          </SortableContext>
        </DndContext>
      ) : (
        content
      )}
    </div>
  );
}

export function DataListAction({
  children,
  className,
  danger,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { danger?: boolean }) {
  return (
    <button
      type="button"
      className={cn(
        "inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] disabled:opacity-50",
        danger && "hover:bg-red-50 hover:text-[var(--danger)]",
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
}

export function DataListLink({
  href,
  children,
  className,
}: {
  href: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className={cn(
        "inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]",
        className
      )}
    >
      {children}
    </a>
  );
}

export function DataListBadge({
  children,
  color,
  className,
}: {
  children: ReactNode;
  color?: string | null;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-white",
        className
      )}
      style={{ backgroundColor: color || "#64748B" }}
    >
      {children}
    </span>
  );
}
