"use client";

import type { ReactNode } from "react";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils/cn";
import {
  FormModal,
  type FormModalSize,
} from "@/components/shared/form-modal";

type Props = {
  open: boolean;
  onOpen: () => void;
  onClose: () => void;
  label: string;
  description?: string;
  modalSize?: FormModalSize;
  children: ReactNode;
  className?: string;
};

/** Reusable create trigger backed by the shared form modal. */
export function CreatePanel({
  open,
  onOpen,
  onClose,
  label,
  description,
  modalSize = "xs",
  children,
  className,
}: Props) {
  return (
    <div className={cn("flex", className)}>
      <button
        type="button"
        onClick={onOpen}
        className="inline-flex w-fit items-center gap-2 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm font-medium text-[var(--ink)] shadow-sm transition-colors hover:border-[var(--accent)] hover:bg-[var(--accent-soft)] hover:text-[var(--accent)]"
      >
        <Plus className="size-4" />
        {label}
      </button>

      <FormModal
        open={open}
        onOpenChange={(nextOpen) => {
          if (nextOpen) onOpen();
          else onClose();
        }}
        title={label}
        description={description}
        size={modalSize}
      >
        {children}
      </FormModal>
    </div>
  );
}
