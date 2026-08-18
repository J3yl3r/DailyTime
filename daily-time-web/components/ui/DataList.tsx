"use client";

import type { ButtonHTMLAttributes, ReactNode } from "react";
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
};

/**
 * Lista plana reutilizable (filas con divisores, sin cards).
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
}: DataListProps<T>) {
  if (items.length === 0) {
    return (
      <p className="rounded-lg border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
        {emptyMessage}
      </p>
    );
  }

  const isSelected = (item: T) =>
    typeof selected === "function" ? selected(item) : Boolean(selected);

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      {header}
      {selectionBar}

      <ul className="divide-y divide-[var(--border)] overflow-hidden rounded-lg border border-[var(--border)] bg-[var(--surface)]">
        {items.map((item) => {
          const key = getKey(item);
          const checked = isSelected(item);
          return (
            <li
              key={key}
              className={cn(
                "px-3 py-2.5 transition-colors sm:px-4",
                checked && "bg-[var(--accent-soft)]/40",
              )}
            >
              <div className="flex flex-wrap items-start justify-between gap-2 sm:gap-3">
                <div className="flex min-w-0 flex-1 items-start gap-2.5">
                  {onToggleSelect ? (
                    <input
                      type="checkbox"
                      className="mt-1 size-4 shrink-0 accent-[var(--accent)]"
                      checked={checked}
                      onChange={() => onToggleSelect(item)}
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
                  <div className="flex shrink-0 flex-wrap items-center gap-0.5">
                    {actions(item)}
                  </div>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>
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
        className,
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
        className,
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
        className,
      )}
      style={{ backgroundColor: color || "#64748B" }}
    >
      {children}
    </span>
  );
}
