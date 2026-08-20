import type { ReactNode } from "react";

export function DetailField({
  label,
  value,
}: {
  label: string;
  value?: ReactNode;
}) {
  if (value == null || value === "") return null;
  return (
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
        {label}
      </p>
      <div className="mt-0.5 text-sm text-[var(--ink)]">{value}</div>
    </div>
  );
}

export function VacancyBody({ text }: { text?: string | null }) {
  if (!text?.trim()) {
    return <p className="text-sm text-[var(--muted)]">No hay descripción guardada.</p>;
  }
  return (
    <pre className="max-h-[28rem] overflow-auto whitespace-pre-wrap rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] px-4 py-3 text-sm leading-relaxed text-[var(--ink)]">
      {text}
    </pre>
  );
}
