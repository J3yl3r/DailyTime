"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { AlertTriangle } from "lucide-react";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";

export type ConfirmOptions = {
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Visual tone for the confirm button. Defaults to danger (delete-like actions). */
  tone?: "danger" | "default";
};

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn | null>(null);

type PendingConfirm = ConfirmOptions & {
  resolve: (value: boolean) => void;
};

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<PendingConfirm | null>(null);
  const pendingRef = useRef<PendingConfirm | null>(null);

  const close = useCallback((result: boolean) => {
    const current = pendingRef.current;
    pendingRef.current = null;
    setPending(null);
    current?.resolve(result);
  }, []);

  const confirm = useCallback<ConfirmFn>((options) => {
    return new Promise<boolean>((resolve) => {
      // If another confirm is open, reject the previous one as cancelled.
      if (pendingRef.current) {
        pendingRef.current.resolve(false);
      }
      const next = { ...options, resolve };
      pendingRef.current = next;
      setPending(next);
    });
  }, []);

  const value = useMemo(() => confirm, [confirm]);
  const tone = pending?.tone ?? "danger";

  return (
    <ConfirmContext.Provider value={value}>
      {children}
      <FormModal
        open={pending != null}
        onOpenChange={(open) => {
          if (!open) close(false);
        }}
        title={pending?.title ?? "Confirmar"}
        size="xs"
        footer={
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => close(false)}
              className="rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--ink)] hover:bg-[var(--surface)]"
            >
              {pending?.cancelLabel ?? "Cancelar"}
            </button>
            <button
              type="button"
              autoFocus
              onClick={() => close(true)}
              className={cn(
                "rounded-md px-4 py-2 text-sm font-medium text-white",
                tone === "danger"
                  ? "bg-[var(--danger)] hover:opacity-90"
                  : "bg-[var(--accent)] hover:bg-[var(--accent-strong)]",
              )}
            >
              {pending?.confirmLabel ?? "Confirmar"}
            </button>
          </div>
        }
      >
        <div className="flex items-start gap-3">
          <div
            className={cn(
              "mt-0.5 flex size-10 shrink-0 items-center justify-center rounded-full",
              tone === "danger"
                ? "bg-red-50 text-[var(--danger)]"
                : "bg-[var(--accent-soft)] text-[var(--accent)]",
            )}
          >
            <AlertTriangle className="size-5" aria-hidden />
          </div>
          <p className="text-sm leading-relaxed text-[var(--muted)]">
            {pending?.description ??
              "Esta acción no se puede deshacer. ¿Deseas continuar?"}
          </p>
        </div>
      </FormModal>
    </ConfirmContext.Provider>
  );
}

export function useConfirm() {
  const confirm = useContext(ConfirmContext);
  if (!confirm) {
    throw new Error("useConfirm debe usarse dentro de ConfirmProvider.");
  }
  return confirm;
}
