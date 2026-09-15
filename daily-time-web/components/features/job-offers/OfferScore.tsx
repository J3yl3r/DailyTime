"use client";

import type { JobOffer } from "@/types/api";
import { DataListBadge } from "@/components/ui/DataList";

export const TIER_COLORS: Record<string, string> = {
  A: "#16A34A",
  B: "#D97706",
  C: "#64748B",
};

export const TIER_LABELS: Record<string, string> = {
  A: "Prioridad A",
  B: "Prioridad B",
  C: "Prioridad C",
  none: "Sin puntaje",
};

function formatPoints(points: number, maxPoints: number) {
  if (maxPoints === 0) return String(points);
  return `${points}/${maxPoints}`;
}

/** Insignia "A · 82" con el desglose del puntaje como tooltip. */
export function OfferScoreBadge({ offer }: { offer: JobOffer }) {
  if (offer.priorityScore == null || !offer.priorityTier) return null;

  const summary = offer.scoreFactors
    .map((factor) => `${factor.label}: ${formatPoints(factor.points, factor.maxPoints)} · ${factor.detail}`)
    .join("\n");

  return (
    <span title={summary} className="inline-flex">
      <DataListBadge color={TIER_COLORS[offer.priorityTier]}>
        {offer.priorityTier} · {offer.priorityScore}
      </DataListBadge>
    </span>
  );
}

export function OfferScoreBreakdown({ offer }: { offer: JobOffer }) {
  if (!offer.scoreFactors.length) {
    return (
      <p className="text-sm text-[var(--muted)]">
        Esta oferta aún no tiene puntaje. Usa «Recalcular» para evaluarla.
      </p>
    );
  }

  return (
    <ul className="flex flex-col gap-2">
      {offer.scoreFactors.map((factor) => {
        const penalty = factor.maxPoints === 0;
        return (
          <li
            key={factor.key}
            className="flex flex-col gap-0.5 text-sm sm:grid sm:grid-cols-[7.5rem_3.5rem_1fr] sm:items-baseline sm:gap-3"
          >
            <span className="text-[var(--muted)]">{factor.label}</span>
            <span
              className={
                penalty ? "font-medium text-[var(--danger)]" : "font-medium text-[var(--ink)]"
              }
            >
              {formatPoints(factor.points, factor.maxPoints)}
            </span>
            <span className="text-[var(--ink)]">{factor.detail}</span>
          </li>
        );
      })}
    </ul>
  );
}
