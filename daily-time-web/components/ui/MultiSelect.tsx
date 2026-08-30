"use client";

import { useState, useRef, useEffect, useMemo } from "react";
import { ChevronDown, Check, X, Search } from "lucide-react";
import { cn } from "@/lib/utils/cn";

export type MultiSelectOption = {
  value: string | number;
  label: string;
  color?: string | null;
};

type MultiSelectProps = {
  label?: string;
  placeholder?: string;
  options: MultiSelectOption[];
  value: (string | number)[];
  onChange: (value: any[]) => void;
  className?: string;
  allLabel?: string;
};

export function MultiSelect({
  label,
  placeholder = "Todos",
  options,
  value,
  onChange,
  className,
  allLabel = "Todos",
}: MultiSelectProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);

  // Close when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setOpen(false);
      }
    }
    if (open) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [open]);

  // Filter options by internal search query
  const filteredOptions = useMemo(() => {
    if (!search.trim()) return options;
    const term = search.toLowerCase().trim();
    return options.filter((opt) => opt.label.toLowerCase().includes(term));
  }, [options, search]);

  const selectedSet = useMemo(() => new Set(value), [value]);

  const toggleOption = (optValue: string | number) => {
    if (selectedSet.has(optValue)) {
      onChange(value.filter((v) => v !== optValue));
    } else {
      onChange([...value, optValue]);
    }
  };

  const selectAll = () => {
    onChange(options.map((o) => o.value));
  };

  const clearAll = () => {
    onChange([]);
  };

  // Button text summary
  const summaryText = useMemo(() => {
    if (value.length === 0) return placeholder;
    if (value.length === options.length && options.length > 1) return allLabel;
    if (value.length <= 2) {
      return options
        .filter((o) => selectedSet.has(o.value))
        .map((o) => o.label)
        .join(", ");
    }
    return `${value.length} seleccionados`;
  }, [value, options, selectedSet, placeholder, allLabel]);

  return (
    <div
      ref={containerRef}
      className={cn("relative flex flex-col gap-1 text-xs font-medium text-[var(--muted)]", className)}
    >
      {label && <span>{label}</span>}
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className={cn(
          "flex h-9 w-full items-center justify-between gap-2 rounded-md border border-[var(--border)] bg-white px-3 py-2 text-left text-sm font-normal text-[var(--ink)] transition-colors hover:bg-[var(--surface-muted)]/50 focus:border-[var(--accent)] focus:outline-none focus:ring-1 focus:ring-[var(--accent)]",
          value.length > 0 && "border-[var(--accent)]/60 bg-[var(--accent-soft)]/20 font-medium"
        )}
        aria-expanded={open}
        aria-haspopup="listbox"
      >
        <span className="truncate">{summaryText}</span>
        <div className="flex shrink-0 items-center gap-1">
          {value.length > 0 ? (
            <span className="flex size-4 items-center justify-center rounded-full bg-[var(--accent)] text-[10px] font-bold text-white">
              {value.length}
            </span>
          ) : null}
          <ChevronDown
            className={cn(
              "size-4 text-[var(--muted)] transition-transform duration-200",
              open && "rotate-180 text-[var(--ink)]"
            )}
          />
        </div>
      </button>

      {open && (
        <div className="absolute top-[calc(100%+4px)] left-0 z-30 flex max-h-72 w-full min-w-48 flex-col rounded-lg border border-[var(--border)] bg-[var(--surface)] p-2 shadow-xl">
          {options.length > 5 && (
            <div className="relative mb-2 shrink-0">
              <Search className="absolute top-2.5 left-2.5 size-3.5 text-[var(--muted)]" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Buscar opción…"
                className="h-8 w-full rounded-md border border-[var(--border)] bg-white pr-2 pl-8 text-xs text-[var(--ink)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none"
                autoFocus
              />
            </div>
          )}

          <div className="mb-2 flex items-center justify-between border-b border-[var(--border)] pb-1.5 text-[11px]">
            <button
              type="button"
              onClick={selectAll}
              className="text-[var(--accent)] hover:underline"
            >
              Seleccionar todos
            </button>
            <button
              type="button"
              onClick={clearAll}
              className="text-[var(--muted)] hover:text-[var(--danger)]"
            >
              Limpiar
            </button>
          </div>

          <ul className="flex-1 overflow-y-auto overflow-x-hidden">
            {filteredOptions.length === 0 ? (
              <li className="py-3 text-center text-xs text-[var(--muted)]">
                No hay coincidencias
              </li>
            ) : (
              filteredOptions.map((opt) => {
                const checked = selectedSet.has(opt.value);
                return (
                  <li key={String(opt.value)}>
                    <button
                      type="button"
                      onClick={() => toggleOption(opt.value)}
                      className={cn(
                        "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-xs text-[var(--ink)] transition-colors hover:bg-[var(--surface-muted)]",
                        checked && "bg-[var(--accent-soft)]/50 font-medium text-[var(--accent-dark,var(--ink))]"
                      )}
                    >
                      <div
                        className={cn(
                          "flex size-3.5 shrink-0 items-center justify-center rounded border border-[var(--border)] transition-colors",
                          checked
                            ? "border-[var(--accent)] bg-[var(--accent)] text-white"
                            : "bg-white"
                        )}
                      >
                        {checked && <Check className="size-2.5 stroke-[3]" />}
                      </div>

                      {opt.color ? (
                        <span
                          className="size-2 shrink-0 rounded-full"
                          style={{ backgroundColor: opt.color }}
                        />
                      ) : null}

                      <span className="truncate">{opt.label}</span>
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        </div>
      )}
    </div>
  );
}

export function FilterPillsBar({
  pills,
  onClearAll,
}: {
  pills: { id: string; label: string; onRemove: () => void }[];
  onClearAll: () => void;
}) {
  if (pills.length === 0) return null;

  return (
    <div className="flex flex-wrap items-center gap-1.5 pt-1">
      <span className="text-xs font-medium text-[var(--muted)]">Filtros activos:</span>
      {pills.map((pill) => (
        <span
          key={pill.id}
          className="inline-flex items-center gap-1 rounded-md border border-[var(--border)] bg-white px-2 py-0.5 text-xs text-[var(--ink)] shadow-xs"
        >
          <span>{pill.label}</span>
          <button
            type="button"
            onClick={pill.onRemove}
            className="rounded p-0.5 text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--danger)]"
            title="Quitar filtro"
          >
            <X className="size-3" />
          </button>
        </span>
      ))}
      <button
        type="button"
        onClick={onClearAll}
        className="rounded px-2 py-0.5 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
      >
        Limpiar todos
      </button>
    </div>
  );
}
