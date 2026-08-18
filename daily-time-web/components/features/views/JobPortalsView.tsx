"use client";

import { useState } from "react";
import { History, Pencil, Play, Trash2, Layers, Timer } from "lucide-react";
import { toast } from "sonner";
import type { JobPortal, JobPortalScrapeLog } from "@/types/api";
import { useJobPortals } from "@/hooks/queries/use-job-portals";
import { useJobPortalMutations } from "@/hooks/mutations/use-job-portal-mutations";
import { getJobPortalScrapeLogs } from "@/lib/api/job-portals";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { useConfirm } from "@/providers/confirm-provider";
import { useScrapeScheduler, MIN_INTERVAL_MINUTES, MAX_INTERVAL_MINUTES } from "@/providers/scrape-scheduler-provider";
import { useAppNotifications } from "@/providers/app-notifications-provider";
import { cn } from "@/lib/utils/cn";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

/** Keywords de búsqueda y filtro de stack (.NET + React/Next) para todos los países. */
const SEARCH_KEYWORDS_DOTNET_JSON = `[
    ".net",
    "desarrollador",
    "backend",
    "fullstack",
    "react",
    "next.js",
    "frontend",
    "front end",
    "desarrollador react",
    "desarrollador frontend"
  ]`;

const REQUIRE_CONTENT_KEYWORDS = `[
    ".net", "dotnet", "asp.net", "aspnet", "c#", "csharp", ".net core", "net core", "entity framework", "ef core",
    "react", "next.js", "nextjs", "next js", "react.js", "reactjs"
  ]`;

const CONFIG_EXAMPLE_ELEMPLEO = `{
  "searchUrlTemplate": "https://www.elempleo.com/co/ofertas-empleo/trabajo-{keyword}",
  "searchKeywords": ${SEARCH_KEYWORDS_DOTNET_JSON},
  "requireContentKeywords": ${REQUIRE_CONTENT_KEYWORDS},
  "listSelectors": [".result-item"],
  "titleSelectors": ["a.js-offer-title", "h2 a"],
  "linkSelectors": ["a.js-offer-title", "h2 a"],
  "companySelectors": [".js-offer-company"],
  "locationSelectors": [".js-offer-city"],
  "dateSelectors": [".js-offer-date"],
  "modalitySelectors": [".js-offer-workday", ".info-contract"],
  "contractSelectors": [".js-offer-contract", ".info-contract"],
  "maxAgeDays": 3,
  "dropIfDateUnknown": true,
  "defaultCountry": "Colombia",
  "defaultLanguage": "es",
  "waitForSelector": ".result-item",
  "clickBeforeExtract": ["a.js-filter-SortOrderBy[data-sort='20']"],
  "waitAfterClickMs": 1500,
  "maxItems": 100,
  "scrollTimes": 4
}`;

const CONFIG_EXAMPLE_INDEED = `{
  "searchUrlTemplate": "https://{countryCode}.indeed.com/jobs?q={query}&fromage=1&sort=date",
  "searchKeywords": ${SEARCH_KEYWORDS_DOTNET_JSON},
  "requireContentKeywords": ${REQUIRE_CONTENT_KEYWORDS},
  "searchCountries": [
    { "code": "co", "name": "Colombia" },
    { "code": "mx", "name": "México" },
    { "code": "es", "name": "España" }
  ],
  "remoteOrHybridOnlyCountryCodes": ["mx", "es"],
  "listSelectors": ["div.job_seen_beacon", "li.css-1ac2h1w", "div.resultContent"],
  "titleSelectors": ["a.jcs-JobTitle", "h2.jobTitle a", "h2.jobTitle span"],
  "linkSelectors": ["a.jcs-JobTitle", "h2.jobTitle a", "a[data-jk]"],
  "companySelectors": ["[data-testid='company-name']", "span.companyName"],
  "locationSelectors": ["[data-testid='text-location']", "div.companyLocation"],
  "dateSelectors": ["[data-testid='timing-attribute']", "span.date"],
  "modalitySelectors": ["[data-testid='attribute_snippet_testid']"],
  "contractSelectors": ["[data-testid='attribute_snippet_testid']"],
  "maxAgeHours": 24,
  "dropIfDateUnknown": true,
  "defaultLanguage": "es",
  "defaultCountry": "Colombia",
  "waitForSelector": "div.job_seen_beacon",
  "maxItems": 100,
  "scrollTimes": 6
}`;

const CONFIG_EXAMPLE_LINKEDIN = `{
  "searchUrlTemplate": "https://www.linkedin.com/jobs/search?keywords={query}&location={country}&f_TPR=r43200&sortBy=DD",
  "searchKeywords": ${SEARCH_KEYWORDS_DOTNET_JSON},
  "requireContentKeywords": ${REQUIRE_CONTENT_KEYWORDS},
  "searchCountries": [
    { "code": "co", "name": "Colombia" },
    { "code": "mx", "name": "México" },
    { "code": "es", "name": "España" }
  ],
  "remoteOrHybridOnlyCountryCodes": ["mx", "es"],
  "listSelectors": ["div.base-card", "li.jobs-search-results__list-item", "div.job-card-container"],
  "titleSelectors": ["h3.base-search-card__title", "a.job-card-list__title--link"],
  "linkSelectors": ["a.base-card__full-link", "a[href*='/jobs/view/']"],
  "companySelectors": ["h4.base-search-card__subtitle", ".artdeco-entity-lockup__subtitle"],
  "locationSelectors": [".job-search-card__location", ".job-card-container__metadata-item"],
  "dateSelectors": ["time", ".job-search-card__listdate"],
  "modalitySelectors": [".job-card-container__metadata-item--workplace-type"],
  "contractSelectors": [".job-card-container__metadata-item--employment-type"],
  "maxAgeHours": 12,
  "dropIfDateUnknown": true,
  "defaultLanguage": "es",
  "defaultCountry": "Colombia",
  "waitForSelector": "div.base-card, li.jobs-search-results__list-item",
  "maxItems": 100,
  "scrollTimes": 8
}`;

const CONFIG_EXAMPLE_COMPUTRABAJO = `{
  "searchUrlTemplate": "https://{host}/trabajo-de-{keyword}?pubdate=1",
  "searchKeywords": ${SEARCH_KEYWORDS_DOTNET_JSON},
  "requireContentKeywords": ${REQUIRE_CONTENT_KEYWORDS},
  "searchCountries": [
    { "code": "co", "name": "Colombia", "host": "co.computrabajo.com" },
    { "code": "mx", "name": "México", "host": "mx.computrabajo.com" }
  ],
  "remoteOrHybridOnlyCountryCodes": ["mx"],
  "listSelectors": ["article.box_offer", "div.box_offer"],
  "titleSelectors": ["a.js-o-link", "h2 a"],
  "linkSelectors": ["a.js-o-link", "h2 a", "a[href*='/ofertas-de-trabajo/']"],
  "companySelectors": ["a.fc_base[href*='/empresas/']", "p.fs16 a"],
  "locationSelectors": ["p.fs13.fc_base", ".fs13"],
  "dateSelectors": ["p.fs13.fc_base.mt15", "span.fs13", "p.fs13"],
  "modalitySelectors": ["p.fs13.fc_base", ".fs13", "span.tag"],
  "contractSelectors": ["p.fs13.fc_base", ".fs13"],
  "maxAgeHours": 24,
  "dropIfDateUnknown": true,
  "defaultLanguage": "es",
  "defaultCountry": "Colombia",
  "waitForSelector": "article.box_offer, div.box_offer",
  "maxItems": 100,
  "scrollTimes": 6
}`;

const CONFIG_EXAMPLE = CONFIG_EXAMPLE_INDEED;

type PortalFormValues = {
  name: string;
  url: string;
  loginUrl: string;
  notes: string;
  scrapeConfig: string;
  isActive: boolean;
};

function PortalForm({
  initial,
  onSubmit,
  onClose,
  submitLabel,
  pending,
}: {
  initial?: Partial<JobPortal>;
  onSubmit: (values: PortalFormValues) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [url, setUrl] = useState(initial?.url ?? "");
  const [loginUrl, setLoginUrl] = useState(initial?.loginUrl ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");
  const [scrapeConfig, setScrapeConfig] = useState(
    initial?.scrapeConfig ?? CONFIG_EXAMPLE,
  );
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!name.trim() || !url.trim()) {
          toast.error("Nombre y URL son obligatorios");
          return;
        }
        if (scrapeConfig.trim()) {
          try {
            JSON.parse(scrapeConfig);
          } catch {
            toast.error("ScrapeConfig debe ser JSON válido");
            return;
          }
        }
        onSubmit({
          name: name.trim(),
          url: url.trim(),
          loginUrl: loginUrl.trim(),
          notes: notes.trim(),
          scrapeConfig: scrapeConfig.trim(),
          isActive,
        });
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Nombre
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Indeed, LinkedIn, elempleo…"
          className={inputClass}
          autoFocus
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        URL de búsqueda
        <input
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder="https://..."
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        URL de login (opcional)
        <input
          value={loginUrl}
          onChange={(e) => setLoginUrl(e.target.value)}
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Notas
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        ScrapeConfig (JSON por portal)
        <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
          <button
            type="button"
            onClick={() => setScrapeConfig(CONFIG_EXAMPLE_INDEED)}
            className="rounded-md border border-[var(--border)] px-2 py-1 text-xs font-normal hover:bg-[var(--surface-muted)]"
          >
            Ejemplo Indeed (CO + MX + ES)
          </button>
          <button
            type="button"
            onClick={() => setScrapeConfig(CONFIG_EXAMPLE_LINKEDIN)}
            className="rounded-md border border-[var(--border)] px-2 py-1 text-xs font-normal hover:bg-[var(--surface-muted)]"
          >
            Ejemplo LinkedIn
          </button>
          <button
            type="button"
            onClick={() => setScrapeConfig(CONFIG_EXAMPLE_ELEMPLEO)}
            className="rounded-md border border-[var(--border)] px-2 py-1 text-xs font-normal hover:bg-[var(--surface-muted)]"
          >
            Ejemplo elempleo
          </button>
          <button
            type="button"
            onClick={() => setScrapeConfig(CONFIG_EXAMPLE_COMPUTRABAJO)}
            className="rounded-md border border-[var(--border)] px-2 py-1 text-xs font-normal hover:bg-[var(--surface-muted)] sm:basis-full sm:self-start"
          >
            Ejemplo Computrabajo (CO + MX)
          </button>
        </div>
        <textarea
          value={scrapeConfig}
          onChange={(e) => setScrapeConfig(e.target.value)}
          rows={10}
          className={`${inputClass} font-mono text-xs`}
        />
        <span className="text-xs font-normal text-[var(--muted)]">
          Usa searchCountries + {"{countryCode}"} para Indeed multi-país. Si el
          sitio cambia el HTML, solo actualizas este JSON.
        </span>
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
        Activo
      </label>
      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-4 py-2 text-sm"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={pending}
          className="rounded-md bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {pending ? "Guardando…" : submitLabel}
        </button>
      </div>
    </form>
  );
}

export function JobPortalsView() {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<JobPortal | null>(null);
  const [logsPortal, setLogsPortal] = useState<JobPortal | null>(null);
  const [logs, setLogs] = useState<JobPortalScrapeLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const query = useJobPortals();
  const mutations = useJobPortalMutations();
  const confirm = useConfirm();
  const scrape = useScrapeScheduler();
  const { notify } = useAppNotifications();
  const items = [...(query.data ?? [])].sort((a, b) => a.id - b.id);
  const busy = scrape.isRunningAll || scrape.currentPortalId != null;

  const openLogs = async (item: JobPortal) => {
    setLogsPortal(item);
    setLogsLoading(true);
    try {
      const data = await getJobPortalScrapeLogs(item.id, 40);
      setLogs(data);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudieron cargar los logs");
      setLogs([]);
    } finally {
      setLogsLoading(false);
    }
  };

  const create = async (values: PortalFormValues) => {
    try {
      await mutations.create.mutateAsync(values);
      setCreating(false);
      toast.success("Portal creado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al crear");
    }
  };

  const update = async (values: PortalFormValues) => {
    if (!editing) return;
    try {
      await mutations.update.mutateAsync({ id: editing.id, body: values });
      setEditing(null);
      toast.success("Portal actualizado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al actualizar");
    }
  };

  const remove = async (item: JobPortal) => {
    const ok = await confirm({
      title: "Eliminar portal",
      description: `¿Estás seguro de eliminar el portal “${item.name}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Portal eliminado"),
      onError: (error) => toast.error(error.message),
    });
  };

  const capture = async (item: JobPortal) => {
    const result = await scrape.runOne(item.id, item.name);
    if (!result) return;
    notify({
      title: `${item.name}: ${result.status}`,
      body: result.message || "Captura finalizada",
      tone: result.status === "error" || result.status === "blocked" ? "error" : "success",
    });
  };

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Captura individual o todos en secuencia. La automática corre cada 10 minutos
          mientras esta pestaña esté abierta (notificaciones en la campana).
        </p>
        <CreatePanel
          open={creating}
          onOpen={() => setCreating(true)}
          onClose={() => setCreating(false)}
          label="Nuevo portal"
        >
          <PortalForm
            onClose={() => setCreating(false)}
            onSubmit={create}
            submitLabel="Crear"
            pending={mutations.create.isPending}
          />
        </CreatePanel>
      </div>

      <div className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[var(--shadow-card)]">
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => void scrape.runAllSequential({ source: "manual" })}
            disabled={busy || !items.some((p) => p.isActive)}
            className="inline-flex items-center gap-1.5 rounded-md bg-[var(--accent)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--accent-strong)] disabled:opacity-50"
          >
            <Layers className="size-4" />
            {scrape.isRunningAll ? "Capturando todos…" : "Capturar todos (secuencia)"}
          </button>
          <label
            className={cn(
              "inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--border)] px-3 py-2 text-sm",
              scrape.autoEnabled
                ? "border-[var(--accent)] bg-[var(--accent-soft)] text-[var(--ink)]"
                : "text-[var(--muted)]",
            )}
          >
            <Timer className="size-4" />
            <span>Ejecución automática</span>
            <input
              type="checkbox"
              className="size-4 accent-[var(--accent)]"
              checked={scrape.autoEnabled}
              onChange={(e) => scrape.setAutoEnabled(e.target.checked)}
            />
          </label>
          <label className="inline-flex items-center gap-2 rounded-md border border-[var(--border)] px-3 py-2 text-sm text-[var(--ink)]">
            <span className="text-[var(--muted)]">Cada</span>
            <input
              type="number"
              min={MIN_INTERVAL_MINUTES}
              max={MAX_INTERVAL_MINUTES}
              step={5}
              value={scrape.intervalMinutes}
              onChange={(e) => scrape.setIntervalMinutes(Number(e.target.value))}
              className="w-16 rounded-md border border-[var(--border)] bg-white px-2 py-1 text-sm outline-none focus:border-[var(--accent)]"
            />
            <span className="text-[var(--muted)]">min</span>
          </label>
        </div>
        <p className="text-xs text-[var(--muted)]">
          Intervalo permitido: {MIN_INTERVAL_MINUTES}–{MAX_INTERVAL_MINUTES} min (recomendado 60).
          {scrape.autoEnabled
            ? ` Automática activa.${scrape.nextAutoAt ? ` Próxima: ${new Date(scrape.nextAutoAt).toLocaleTimeString()}.` : ""}${scrape.lastAutoAt ? ` Última: ${new Date(scrape.lastAutoAt).toLocaleString()}.` : ""}`
            : " Automática desactivada. Puedes capturar uno por uno o todos en secuencia."}
          {scrape.currentPortalId
            ? ` · Ejecutando portal #${scrape.currentPortalId}…`
            : ""}
        </p>
      </div>

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && (
        <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>
      )}
      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            Aún no hay portales. Agrega Indeed, LinkedIn o elempleo con su
            ScrapeConfig.
          </p>
        ) : (
          <div className="flex flex-col gap-2">
            {items.map((item) => (
              <article
                key={item.id}
                className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]"
              >
                <div className="min-w-0">
                  <p className="font-medium">{item.name}</p>
                  <p className="truncate text-sm text-[var(--muted)]">{item.url}</p>
                  <p className="mt-1 text-xs text-[var(--muted)]">
                    {!item.isActive ? "Inactivo · " : ""}
                    {item.lastRunStatus
                      ? `Última captura: ${item.lastRunStatus}`
                      : "Sin capturas aún"}
                    {item.lastRunAt
                      ? ` · ${new Date(item.lastRunAt).toLocaleString()}`
                      : ""}
                  </p>
                </div>
                <div className="flex flex-wrap justify-start gap-1">
                  <button
                    type="button"
                    onClick={() => void capture(item)}
                    disabled={!item.isActive || busy}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] disabled:opacity-50"
                  >
                    <Play className="size-3.5" />
                    {scrape.currentPortalId === item.id ? "Capturando…" : "Capturar"}
                  </button>
                  <button
                    type="button"
                    onClick={() => void openLogs(item)}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                  >
                    <History className="size-3.5" /> Logs
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditing(item)}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                  >
                    <Pencil className="size-3.5" /> Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => remove(item)}
                    className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
                  >
                    <Trash2 className="size-3.5" /> Eliminar
                  </button>
                </div>
              </article>
            ))}
          </div>
        )
      )}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => !open && setEditing(null)}
        title="Editar portal"
        size="sm"
      >
        {editing ? (
          <PortalForm
            initial={editing}
            onClose={() => setEditing(null)}
            onSubmit={update}
            submitLabel="Actualizar"
            pending={mutations.update.isPending}
          />
        ) : null}
      </FormModal>

      <FormModal
        open={logsPortal != null}
        onOpenChange={(open) => {
          if (!open) {
            setLogsPortal(null);
            setLogs([]);
          }
        }}
        title={logsPortal ? `Logs · ${logsPortal.name}` : "Logs"}
        description="Historial de capturas del scraper para este portal."
        size="md"
      >
        {logsLoading ? (
          <p className="text-sm text-[var(--muted)]">Cargando logs…</p>
        ) : !logs.length ? (
          <p className="text-sm text-[var(--muted)]">
            Aún no hay logs. Ejecuta una captura para generar historial.
          </p>
        ) : (
          <ul className="flex flex-col gap-2">
            {logs.map((log) => (
              <li
                key={log.id}
                className="rounded-lg border border-[var(--border)] px-3 py-2 text-sm"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-medium text-[var(--ink)]">{log.status}</span>
                  <span className="text-xs text-[var(--muted)]">
                    {new Date(log.startedAt).toLocaleString()}
                    {log.finishedAt
                      ? ` → ${new Date(log.finishedAt).toLocaleString()}`
                      : " (en curso)"}
                  </span>
                </div>
                <p className="mt-1 text-xs text-[var(--muted)]">
                  Ofertas: {log.offerCount}
                  {log.savedInserted || log.savedUpdated
                    ? ` · Guardadas: +${log.savedInserted} / ~${log.savedUpdated}`
                    : ""}
                </p>
                {log.message ? (
                  <p className="mt-1 whitespace-pre-wrap text-xs text-[var(--ink)]">
                    {log.message}
                  </p>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </FormModal>
    </div>
  );
}
