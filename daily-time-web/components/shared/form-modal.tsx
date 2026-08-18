"use client";

import {
  useEffect,
  useId,
  useRef,
  type MouseEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";
import { cn } from "@/lib/utils/cn";

export type FormModalSize = "xs" | "sm" | "md" | "lg";

const SIZE_CLASSES: Record<FormModalSize, string> = {
  xs: "max-w-md",
  sm: "max-w-xl",
  md: "max-w-3xl",
  lg: "max-w-5xl",
};

type FormModalProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  size?: FormModalSize;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
};

export function FormModal({
  open,
  onOpenChange,
  title,
  description,
  size = "md",
  children,
  footer,
  className,
}: FormModalProps) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const previouslyFocused = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) return;

    previouslyFocused.current = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const focusTimer = window.setTimeout(() => {
      const initialFocus = dialogRef.current?.querySelector<HTMLElement>(
        "[autofocus], input:not([type='hidden']), textarea, select, button",
      );
      initialFocus?.focus();
    });

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onOpenChange(false);
        return;
      }

      if (event.key !== "Tab" || !dialogRef.current) return;

      const focusable = Array.from(
        dialogRef.current.querySelectorAll<HTMLElement>(
          "button:not([disabled]), input:not([disabled]):not([type='hidden']), textarea:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex='-1'])",
        ),
      );
      if (!focusable.length) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      window.clearTimeout(focusTimer);
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", handleKeyDown);
      previouslyFocused.current?.focus();
    };
  }, [open, onOpenChange]);

  if (!open || typeof document === "undefined") return null;

  const preventBackdropDismiss = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      event.preventDefault();
    }
  };

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-[var(--ink)]/45 p-0 backdrop-blur-[2px] sm:items-center sm:p-4"
      onMouseDown={preventBackdropDismiss}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        className={cn(
          "flex max-h-[92dvh] w-full flex-col overflow-hidden rounded-t-2xl border border-[var(--border)] bg-[var(--surface)] shadow-2xl sm:rounded-2xl",
          SIZE_CLASSES[size],
          className,
        )}
      >
        <header className="flex shrink-0 items-start justify-between gap-4 border-b border-[var(--border)] px-5 py-4 sm:px-6">
          <div className="min-w-0">
            <h2
              id={titleId}
              className="font-[family-name:var(--font-fraunces)] text-xl font-semibold text-[var(--ink)]"
            >
              {title}
            </h2>
            {description ? (
              <p id={descriptionId} className="mt-1 text-sm text-[var(--muted)]">
                {description}
              </p>
            ) : null}
          </div>
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="shrink-0 rounded-lg p-2 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
            aria-label="Cerrar modal"
          >
            <X className="size-5" />
          </button>
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5 sm:px-6">
          {children}
        </div>

        {footer ? (
          <footer className="shrink-0 border-t border-[var(--border)] bg-[var(--surface-muted)]/70 px-5 py-4 sm:px-6">
            {footer}
          </footer>
        ) : null}
      </div>
    </div>,
    document.body,
  );
}

type FormModalSectionProps = {
  title?: string;
  children: ReactNode;
  className?: string;
};

export function FormModalSection({
  title,
  children,
  className,
}: FormModalSectionProps) {
  return (
    <section className={cn("space-y-4", className)}>
      {title ? (
        <h3 className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
          {title}
        </h3>
      ) : null}
      {children}
    </section>
  );
}

type FormModalFooterProps = {
  children: ReactNode;
  className?: string;
};

export function FormModalFooter({ children, className }: FormModalFooterProps) {
  return (
    <div className={cn("flex flex-col-reverse gap-2 sm:flex-row sm:justify-end", className)}>
      {children}
    </div>
  );
}
