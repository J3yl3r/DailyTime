"use client";

import { useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, X } from "lucide-react";
import { toast } from "sonner";
import type { OfferTriageSettings, RescoreJobOffersResult } from "@/types/api";
import { FormModal, FormModalSection } from "@/components/shared/form-modal";
import { useOfferAiStatus } from "@/hooks/queries/use-offer-ai-status";
import { useOfferTriageSettings } from "@/hooks/queries/use-offer-triage-settings";
import { useJobOfferMutations } from "@/hooks/mutations/use-job-offer-mutations";
import { useConfirm } from "@/providers/confirm-provider";
import { offerTriageSchema, type OfferTriageInput } from "@/schemas/offer-triage.schema";
import { cn } from "@/lib/utils/cn";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 text-sm font-normal text-[var(--ink)] outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

type RuleName =
  | "discardOnsiteAbroad"
  | "discardResidencyAbroad"
  | "discardOutOfProfile"
  | "discardDuplicates"
  | "discardOnsiteLocal";

const RULES: { name: RuleName; label: string; hint: string }[] = [
  {
    name: "discardOnsiteAbroad",
    label: "Presencial o híbrido fuera de tu país",
    hint: "Tu país sale de la ubicación de tu perfil.",
  },
  {
    name: "discardResidencyAbroad",
    label: "Exige residir en otro país",
    hint: "«Mexico only», «residentes en España» o un país que no aceptas en el título.",
  },
  {
    name: "discardOutOfProfile",
    label: "Fuera de tu perfil",
    hint: "Sin ninguno de tus stacks y con un título que no es de un rol técnico.",
  },
  {
    name: "discardDuplicates",
    label: "Duplicadas en otro portal",
    hint: "Mismo título y empresa; se conserva la primera capturada.",
  },
  {
    name: "discardOnsiteLocal",
    label: "Presencial en tu país",
    hint: "Déjala activa si solo buscas remoto o híbrido.",
  },
];

export function OfferTriageRulesModal({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const settings = useOfferTriageSettings(open);

  return (
    <FormModal
      open={open}
      onOpenChange={onOpenChange}
      title="Reglas de prioridad"
      description="Qué se descarta automáticamente y cómo se ordenan tus ofertas."
      size="md"
    >
      {settings.isLoading ? (
        <p className="text-sm text-[var(--muted)]">Cargando reglas…</p>
      ) : settings.error ? (
        <p className="text-sm text-[var(--danger)]">{String(settings.error)}</p>
      ) : settings.data ? (
        <RulesForm
          key={settings.dataUpdatedAt}
          initial={settings.data}
          onDone={() => onOpenChange(false)}
        />
      ) : null}
    </FormModal>
  );
}

function RulesForm({
  initial,
  onDone,
}: {
  initial: OfferTriageSettings;
  onDone: () => void;
}) {
  const { previewTriage, saveTriageSettings } = useJobOfferMutations();
  const confirm = useConfirm();
  const [preview, setPreview] = useState<RescoreJobOffersResult | null>(null);
  const form = useForm<OfferTriageInput>({
    resolver: zodResolver(offerTriageSchema),
    defaultValues: initial,
  });
  const autoDiscard = useWatch({ control: form.control, name: "autoDiscardEnabled" });
  const errors = form.formState.errors;
  const busy = previewTriage.isPending || saveTriageSettings.isPending;
  const clearPreview = () => setPreview(null);

  const runPreview = form.handleSubmit(async (values) => {
    try {
      setPreview(await previewTriage.mutateAsync(values));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudo calcular la vista previa");
    }
  });

  const save = form.handleSubmit(async (values) => {
    try {
      const result = await previewTriage.mutateAsync(values);
      setPreview(result);
      if (result.newlyDiscarded + result.restored > 0) {
        const ok = await confirm({
          title: "Aplicar reglas",
          description: `Se descartarán ${result.newlyDiscarded} oferta(s) y se restaurarán ${result.restored}. Las descartadas se recuperan desde el filtro de estado «Descartada».`,
          confirmLabel: "Aplicar",
        });
        if (!ok) return;
      }
      const saved = await saveTriageSettings.mutateAsync(values);
      toast.success("Reglas aplicadas", {
        description: `Descartadas: ${saved.result.newlyDiscarded} · Restauradas: ${saved.result.restored} · Activas: ${saved.result.activeAfter}`,
      });
      onDone();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudieron guardar las reglas");
    }
  });

  return (
    <form onSubmit={save} onChange={clearPreview} className="flex flex-col gap-6">
      <FormModalSection title="Descarte automático">
        <label className="inline-flex items-center gap-2 text-sm font-medium text-[var(--ink)]">
          <input
            type="checkbox"
            {...form.register("autoDiscardEnabled")}
            className="size-4 accent-[var(--accent)]"
          />
          Descartar automáticamente las ofertas que cumplan alguna regla
        </label>

        <div className={cn("grid gap-3 sm:grid-cols-2", !autoDiscard && "pointer-events-none opacity-50")}>
          {RULES.map((rule) => (
            <label key={rule.name} className="flex items-start gap-2 text-sm text-[var(--ink)]">
              <input
                type="checkbox"
                {...form.register(rule.name)}
                className="mt-0.5 size-4 shrink-0 accent-[var(--accent)]"
              />
              <span>
                {rule.label}
                <span className="block text-xs text-[var(--muted)]">{rule.hint}</span>
              </span>
            </label>
          ))}

          <label className="flex flex-col gap-1 text-sm text-[var(--ink)]">
            Antigüedad máxima (días)
            <input
              type="number"
              min={0}
              max={365}
              {...form.register("maxAgeDays", { valueAsNumber: true })}
              className={cn(inputClass, "w-28")}
            />
            <span className="text-xs text-[var(--muted)]">0 = sin límite.</span>
            {errors.maxAgeDays ? (
              <span className="text-xs text-[var(--danger)]">{errors.maxAgeDays.message}</span>
            ) : null}
          </label>
        </div>
      </FormModalSection>

      <FormModalSection title="Listas">
        <ListField
          label="Palabras excluidas en el título"
          hint="Solo se buscan en el título. Una tecnología (p. ej. «java») no descarta si el título menciona uno de tus stacks."
        >
          <Controller
            control={form.control}
            name="excludedTitleKeywords"
            render={({ field }) => (
              <TagListInput
                value={field.value}
                onChange={(value) => {
                  field.onChange(value);
                  clearPreview();
                }}
                placeholder="practicante, sap, php…"
              />
            )}
          />
        </ListField>

        <ListField label="Empresas bloqueadas" hint="Coincidencia por palabra completa en el nombre de la empresa.">
          <Controller
            control={form.control}
            name="blockedCompanies"
            render={({ field }) => (
              <TagListInput
                value={field.value}
                onChange={(value) => {
                  field.onChange(value);
                  clearPreview();
                }}
                placeholder="Nombre de la empresa"
              />
            )}
          />
        </ListField>

        <ListField
          label="Países aceptados"
          hint="Vacío = los países preferidos de tu perfil. Tu país siempre cuenta."
        >
          <Controller
            control={form.control}
            name="allowedCountries"
            render={({ field }) => (
              <TagListInput
                value={field.value}
                onChange={(value) => {
                  field.onChange(value);
                  clearPreview();
                }}
                placeholder="Colombia, México…"
              />
            )}
          />
        </ListField>
      </FormModalSection>

      <FormModalSection title="Puntaje">
        <label className="inline-flex items-center gap-2 text-sm text-[var(--ink)]">
          <input
            type="checkbox"
            {...form.register("penalizeEnglishGap")}
            className="size-4 accent-[var(--accent)]"
          />
          Restar puntos si piden inglés avanzado y tu nivel no lo es
        </label>

        <div className="flex flex-wrap gap-4">
          <label className="flex flex-col gap-1 text-sm text-[var(--ink)]">
            Mínimo para A
            <input
              type="number"
              min={1}
              max={100}
              {...form.register("tierAMin", { valueAsNumber: true })}
              className={cn(inputClass, "w-24")}
            />
            {errors.tierAMin ? (
              <span className="text-xs text-[var(--danger)]">{errors.tierAMin.message}</span>
            ) : null}
          </label>
          <label className="flex flex-col gap-1 text-sm text-[var(--ink)]">
            Mínimo para B
            <input
              type="number"
              min={0}
              max={99}
              {...form.register("tierBMin", { valueAsNumber: true })}
              className={cn(inputClass, "w-24")}
            />
            {errors.tierBMin ? (
              <span className="text-xs text-[var(--danger)]">{errors.tierBMin.message}</span>
            ) : null}
          </label>
        </div>

        <p className="text-xs text-[var(--muted)]">
          Factores sobre 100: tu stack 40 · rol técnico 10 · modalidad 15 · país 10 · seniority 15 ·
          recencia 10. Restan: inglés avanzado (−15) y un stack principal distinto al tuyo (−10).
          Por debajo del mínimo de B la prioridad es C.
        </p>
      </FormModalSection>

      <FormModalSection title="Análisis con IA (Gemini)">
        <AiStatusLine />

        <label className="inline-flex items-center gap-2 text-sm text-[var(--ink)]">
          <input
            type="checkbox"
            {...form.register("aiEnabled")}
            className="size-4 accent-[var(--accent)]"
          />
          Analizar automáticamente todas las ofertas activas que entren
        </label>

        <label className="flex flex-col gap-1 text-sm text-[var(--ink)]">
          Modelo
          <input {...form.register("aiModel")} className={cn(inputClass, "w-60 font-mono text-xs")} />
          {errors.aiModel ? (
            <span className="text-xs text-[var(--danger)]">{errors.aiModel.message}</span>
          ) : null}
        </label>

        <p className="text-xs text-[var(--muted)]">
          Se analizan primero las que entraron más recientemente. No hay tope propio: si Google agota la
          cuota gratuita, el análisis se pausa y continúa solo cuando se renueva. A Gemini solo se envía la
          oferta y, de tu perfil, país, países aceptados, modalidad preferida, stacks, años de experiencia y
          nivel de inglés; nunca nombre, correo, teléfono ni salario. El análisis ajusta el puntaje entre +10
          y −20, pero nunca descarta ni cambia estados.
        </p>
      </FormModalSection>

      {preview ? <PreviewPanel result={preview} /> : null}

      <div className="flex flex-col-reverse gap-2 border-t border-[var(--border)] pt-4 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onDone}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--muted)] hover:bg-[var(--surface-muted)]"
        >
          Cancelar
        </button>
        <button
          type="button"
          onClick={() => void runPreview()}
          disabled={busy}
          className="inline-flex items-center justify-center gap-1.5 rounded-md border border-[var(--border)] px-4 py-2 text-sm text-[var(--ink)] hover:bg-[var(--surface-muted)] disabled:opacity-50"
        >
          <Eye className="size-4" />
          {previewTriage.isPending && !saveTriageSettings.isPending ? "Calculando…" : "Vista previa"}
        </button>
        <button
          type="submit"
          disabled={busy}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
        >
          {saveTriageSettings.isPending ? "Aplicando…" : "Guardar y recalcular"}
        </button>
      </div>
    </form>
  );
}

function AiStatusLine() {
  const status = useOfferAiStatus();
  const { requestAiAnalysis } = useJobOfferMutations();

  if (status.isLoading) {
    return <p className="text-xs text-[var(--muted)]">Consultando el estado del análisis…</p>;
  }
  if (!status.data) {
    return (
      <p className="text-xs text-[var(--danger)]">
        No se pudo consultar el estado del análisis{status.error ? `: ${String(status.error)}` : "."}
      </p>
    );
  }

  const s = status.data;
  if (!s.configured) {
    return (
      <div className="rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
        Falta la clave de Gemini. Créala gratis en aistudio.google.com/apikey y guárdala desde la carpeta
        del repo con:
        <code className="mt-1 block break-all font-mono">
          dotnet user-secrets set &quot;Gemini:ApiKey&quot; &quot;TU_CLAVE&quot; --project dailyTimeApi
        </code>
        Luego reinicia los servicios.
      </div>
    );
  }

  const resetsAt = new Date(s.resetsAt).toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" });
  const details = [
    s.isRunning ? "Analizando…" : null,
    `Hoy: ${s.usedToday} analizadas`,
    `Pendientes: ${s.pending}`,
    `La cuota de Google se renueva a las ${resetsAt}`,
    s.pausedUntil ? `En pausa: ${s.pauseReason ?? "cuota agotada"}` : null,
    s.lastError && !s.pausedUntil ? `Último error: ${s.lastError}` : null,
  ].filter(Boolean);

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 rounded-md bg-[var(--surface-muted)] px-3 py-2 text-xs text-[var(--ink)]">
      <span>{details.join(" · ")}</span>
      <button
        type="button"
        onClick={() =>
          requestAiAnalysis.mutate(undefined, {
            onSuccess: () => toast.success("Análisis de pendientes en marcha"),
            onError: (error) => toast.error(error.message),
          })
        }
        disabled={requestAiAnalysis.isPending || s.isRunning || !s.enabled || s.pending === 0 || s.pausedUntil != null}
        className="rounded-md border border-[var(--border)] bg-white px-2.5 py-1 text-xs text-[var(--ink)] hover:bg-[var(--surface-muted)] disabled:opacity-50"
      >
        Analizar pendientes ahora
      </button>
    </div>
  );
}

function ListField({
  label,
  hint,
  children,
}: {
  label: string;
  hint: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-sm font-medium text-[var(--ink)]">{label}</span>
      {children}
      <span className="text-xs text-[var(--muted)]">{hint}</span>
    </div>
  );
}

function TagListInput({
  value,
  onChange,
  placeholder,
}: {
  value: string[];
  onChange: (value: string[]) => void;
  placeholder?: string;
}) {
  const [draft, setDraft] = useState("");

  const add = (raw: string) => {
    const items = raw
      .split(/[,\n]/)
      .map((item) => item.trim())
      .filter(Boolean);
    if (!items.length) return;
    const next = [...value];
    for (const item of items) {
      if (!next.some((existing) => existing.toLowerCase() === item.toLowerCase())) next.push(item);
    }
    onChange(next);
    setDraft("");
  };

  return (
    <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-[var(--border)] bg-white px-2 py-1.5 focus-within:border-[var(--accent)] focus-within:ring-2 focus-within:ring-[var(--accent-soft)]">
      {value.map((item) => (
        <span
          key={item}
          className="inline-flex items-center gap-1 rounded-full bg-[var(--surface-muted)] py-0.5 pl-2.5 pr-1 text-xs text-[var(--ink)]"
        >
          {item}
          <button
            type="button"
            aria-label={`Quitar ${item}`}
            onClick={() => onChange(value.filter((existing) => existing !== item))}
            className="rounded-full p-0.5 text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
          >
            <X className="size-3" />
          </button>
        </span>
      ))}
      <input
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === ",") {
            event.preventDefault();
            add(draft);
          } else if (event.key === "Backspace" && !draft && value.length) {
            onChange(value.slice(0, -1));
          }
        }}
        onBlur={() => add(draft)}
        placeholder={value.length ? "Agregar…" : placeholder}
        className="min-w-[8rem] flex-1 bg-transparent px-1 py-0.5 text-sm outline-none"
      />
    </div>
  );
}

function PreviewPanel({ result }: { result: RescoreJobOffersResult }) {
  return (
    <section className="rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] p-4 text-sm">
      <h3 className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
        Vista previa (sin guardar)
      </h3>
      <p className="mt-2 text-[var(--ink)]">
        {result.evaluated} ofertas evaluadas · quedarían activas {result.activeAfter} (A {result.tierA} · B{" "}
        {result.tierB} · C {result.tierC})
      </p>
      <p className="mt-1 text-[var(--ink)]">
        Se descartarían <strong>{result.newlyDiscarded}</strong> · se restaurarían{" "}
        <strong>{result.restored}</strong>
        {result.protectedByUser
          ? ` · ${result.protectedByUser} no se tocan porque su estado lo decidiste tú`
          : ""}
      </p>

      {result.discardReasons.length ? (
        <ul className="mt-3 flex flex-col gap-1">
          {result.discardReasons.slice(0, 8).map((reason) => (
            <li key={reason.reason} className="flex justify-between gap-3">
              <span className="text-[var(--ink)]">{reason.reason}</span>
              <span className="shrink-0 font-medium text-[var(--ink)]">{reason.count}</span>
            </li>
          ))}
        </ul>
      ) : null}

      {result.samples.length ? (
        <details className="mt-3">
          <summary className="cursor-pointer text-xs text-[var(--muted)]">
            Ejemplos de ofertas que se descartarían
          </summary>
          <ul className="mt-2 flex flex-col gap-1.5">
            {result.samples.map((sample) => (
              <li key={sample.id} className="text-xs text-[var(--ink)]">
                <span className="font-medium">{sample.title}</span>
                {sample.company ? ` · ${sample.company}` : ""} · {sample.portalName}
                <span className="block text-[var(--muted)]">{sample.reason}</span>
              </li>
            ))}
          </ul>
        </details>
      ) : null}
    </section>
  );
}
