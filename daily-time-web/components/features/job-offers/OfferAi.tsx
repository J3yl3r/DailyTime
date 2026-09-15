"use client";

import { Check, CircleHelp, Minus, Sparkles, TriangleAlert, X } from "lucide-react";
import { toast } from "sonner";
import type { JobOffer, OfferAiAnalysis } from "@/types/api";
import { DataListBadge } from "@/components/ui/DataList";
import { useJobOfferMutations } from "@/hooks/mutations/use-job-offer-mutations";

export const AI_VERDICTS: Record<string, { label: string; short: string; color: string }> = {
  apply: { label: "Recomienda postular", short: "IA: postular", color: "#16A34A" },
  maybe: { label: "Dudosa", short: "IA: dudosa", color: "#D97706" },
  skip: { label: "No encaja", short: "IA: no encaja", color: "#DC2626" },
};

const SENIORITY_LABELS: Record<string, string> = {
  junior: "Junior",
  "semi-senior": "Semi senior",
  senior: "Senior",
  lead: "Líder",
};

const ENGLISH_LABELS: Record<string, string> = {
  none: "No lo pide",
  basic: "Básico",
  intermediate: "Intermedio",
  advanced: "Avanzado",
};

const MODALITY_LABELS: Record<string, string> = {
  remote: "Remoto",
  hybrid: "Híbrido",
  onsite: "Presencial",
};

const PERIOD_LABELS: Record<string, string> = {
  month: "mes",
  year: "año",
  hour: "hora",
};

/** Igual que el puntaje de la API: un "no encaja" sin requisito incumplido ni restricción cuenta como dudosa. */
function verdictOf(analysis: OfferAiAnalysis) {
  const backedSkip =
    analysis.missingMustHaves.length > 0 ||
    analysis.mandatoryRequirements.some((r) => r.met === "no") ||
    analysis.locationRestriction.trim() !== "";
  const key = analysis.verdict === "skip" && !backedSkip ? "maybe" : analysis.verdict;
  return AI_VERDICTS[key] ?? AI_VERDICTS.maybe;
}

/** Insignia compacta para la lista, con el resumen como tooltip. */
export function OfferAiBadge({ offer }: { offer: JobOffer }) {
  if (!offer.aiAnalysis) return null;
  const verdict = verdictOf(offer.aiAnalysis);
  return (
    <span title={offer.aiAnalysis.summary || verdict.label} className="inline-flex">
      <DataListBadge color={verdict.color}>{verdict.short}</DataListBadge>
    </span>
  );
}

function RequirementIcon({ met }: { met: string }) {
  if (met === "yes") return <Check className="size-3.5 shrink-0 text-green-600" aria-label="Cumple" />;
  if (met === "no") return <X className="size-3.5 shrink-0 text-[var(--danger)]" aria-label="No cumple" />;
  if (met === "partial") return <Minus className="size-3.5 shrink-0 text-amber-600" aria-label="Parcial" />;
  return <CircleHelp className="size-3.5 shrink-0 text-[var(--muted)]" aria-label="Sin datos" />;
}

function formatSalary(salary: OfferAiAnalysis["salary"]) {
  if (!salary.min && !salary.max) return null;
  const format = (value: number) => value.toLocaleString("es-CO");
  const range =
    salary.min && salary.max && salary.min !== salary.max
      ? `${format(salary.min)} – ${format(salary.max)}`
      : format(salary.max || salary.min);
  const period = PERIOD_LABELS[salary.period];
  return [range, salary.currency, period ? `/ ${period}` : null].filter(Boolean).join(" ");
}

/** Sección "Análisis con IA" del detalle de una oferta. */
export function OfferAiPanel({
  offer,
  onUpdated,
}: {
  offer: JobOffer;
  onUpdated: (offer: JobOffer) => void;
}) {
  const { analyzeWithAi } = useJobOfferMutations();
  const analysis = offer.aiAnalysis;

  const analyze = () =>
    analyzeWithAi.mutate(offer.id, {
      onSuccess: (updated) => {
        onUpdated(updated);
        toast.success("Oferta analizada con IA");
      },
      onError: (error) => toast.error(error.message),
    });

  const facts: [string, string | null][] = analysis
    ? [
        ["Seniority", SENIORITY_LABELS[analysis.seniority] ?? null],
        ["Experiencia pedida", analysis.requiredYears ? `${analysis.requiredYears} años` : null],
        ["Inglés", ENGLISH_LABELS[analysis.englishLevel] ?? null],
        ["Modalidad", MODALITY_LABELS[analysis.workModality] ?? null],
        ["Salario", formatSalary(analysis.salary)],
        ["Restricción", analysis.locationRestriction || null],
      ]
    : [];

  return (
    <section className="flex flex-col gap-3 rounded-lg border border-[var(--border)] p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="inline-flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
          <Sparkles className="size-3.5" />
          Análisis con IA
        </p>
        <button
          type="button"
          onClick={analyze}
          disabled={analyzeWithAi.isPending}
          className="inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-1.5 text-xs text-[var(--ink)] hover:bg-[var(--surface-muted)] disabled:opacity-50"
        >
          <Sparkles className="size-3.5" />
          {analyzeWithAi.isPending ? "Analizando…" : analysis ? "Reanalizar" : "Analizar con IA"}
        </button>
      </div>

      {offer.aiError ? (
        <p className="flex items-start gap-1.5 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
          <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
          Último intento falló: {offer.aiError}
        </p>
      ) : null}

      {!analysis ? (
        <p className="text-sm text-[var(--muted)]">
          Aún no tiene análisis. Se hace solo para las ofertas activas; puedes pedirlo ahora.
        </p>
      ) : (
        <>
          <div className="flex flex-wrap items-start gap-2">
            <DataListBadge color={verdictOf(analysis).color}>{verdictOf(analysis).label}</DataListBadge>
            {analysis.summary ? <p className="text-sm text-[var(--ink)]">{analysis.summary}</p> : null}
          </div>

          {analysis.mandatoryRequirements.length ? (
            <div>
              <p className="mb-1 text-xs font-medium text-[var(--muted)]">Requisitos obligatorios</p>
              <ul className="flex flex-col gap-1">
                {analysis.mandatoryRequirements.map((item) => (
                  <li key={item.requirement} className="flex items-start gap-1.5 text-sm text-[var(--ink)]">
                    <span className="mt-0.5">
                      <RequirementIcon met={item.met} />
                    </span>
                    {item.requirement}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          {analysis.missingMustHaves.length ? (
            <p className="text-sm text-[var(--ink)]">
              <span className="font-medium text-[var(--danger)]">Te falta: </span>
              {analysis.missingMustHaves.join(", ")}
            </p>
          ) : null}

          {facts.some(([, value]) => value) ? (
            <dl className="grid gap-x-4 gap-y-1 text-sm sm:grid-cols-2">
              {facts
                .filter(([, value]) => value)
                .map(([label, value]) => (
                  <div key={label} className="flex gap-1.5">
                    <dt className="text-[var(--muted)]">{label}:</dt>
                    <dd className="text-[var(--ink)]">{value}</dd>
                  </div>
                ))}
            </dl>
          ) : null}

          {analysis.redFlags.length ? (
            <ul className="flex flex-col gap-1">
              {analysis.redFlags.map((flag) => (
                <li key={flag} className="flex items-start gap-1.5 text-xs text-amber-800">
                  <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
                  {flag}
                </li>
              ))}
            </ul>
          ) : null}

          <p className="text-[11px] text-[var(--muted)]">
            {offer.aiModel ?? "Gemini"}
            {offer.aiAnalyzedAt ? ` · ${new Date(offer.aiAnalyzedAt).toLocaleString("es-CO")}` : ""} · Revisa
            siempre la oferta original: la IA puede equivocarse.
          </p>
        </>
      )}
    </section>
  );
}
