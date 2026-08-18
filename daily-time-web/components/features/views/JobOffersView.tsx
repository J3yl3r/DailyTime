"use client";

import { useMemo, useState } from "react";
import { ExternalLink, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { JobOffer, JobOfferFilters } from "@/types/api";
import { useJobOffers } from "@/hooks/queries/use-job-offers";
import { useJobOfferMeta } from "@/hooks/queries/use-job-offer-meta";
import { useJobOfferMutations } from "@/hooks/mutations/use-job-offer-mutations";
import { useJobPortals } from "@/hooks/queries/use-job-portals";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";

const STATUS_LABELS: Record<string, string> = {
  new: "Nueva",
  seen: "Vista",
  discarded: "Descartada",
  applied: "Postulada",
};

const LANGUAGE_LABELS: Record<string, string> = {
  es: "Español",
  en: "Inglés",
  pt: "Portugués",
  fr: "Francés",
};

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 text-sm text-[var(--ink)]";

function formatLanguage(code: string) {
  return LANGUAGE_LABELS[code] ?? code;
}

export function JobOffersView() {
  const portals = useJobPortals();
  const meta = useJobOfferMeta();
  const confirm = useConfirm();
  const [portalId, setPortalId] = useState<number | "all">("all");
  const [status, setStatus] = useState<string>("all");
  const [search, setSearch] = useState("");
  const [country, setCountry] = useState("");
  const [language, setLanguage] = useState("");
  const [workModality, setWorkModality] = useState("");
  const [contractType, setContractType] = useState("");
  const [techStack, setTechStack] = useState("");
  const [capturedFrom, setCapturedFrom] = useState("");
  const [capturedTo, setCapturedTo] = useState("");
  const [postedFrom, setPostedFrom] = useState("");
  const [postedTo, setPostedTo] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());

  const filters = useMemo<JobOfferFilters>(() => {
    const f: JobOfferFilters = {};
    if (portalId !== "all") f.portalId = portalId;
    if (status !== "all") f.status = status;
    if (search.trim()) f.search = search.trim();
    if (country) f.country = country;
    if (language) f.language = language;
    if (workModality) f.workModality = workModality;
    if (contractType) f.contractType = contractType;
    if (techStack) f.techStack = techStack;
    if (capturedFrom) f.capturedFrom = capturedFrom;
    if (capturedTo) f.capturedTo = capturedTo;
    if (postedFrom) f.postedFrom = postedFrom;
    if (postedTo) f.postedTo = postedTo;
    return f;
  }, [
    portalId,
    status,
    search,
    country,
    language,
    workModality,
    contractType,
    techStack,
    capturedFrom,
    capturedTo,
    postedFrom,
    postedTo,
  ]);

  const query = useJobOffers(filters);
  const mutations = useJobOfferMutations();
  const items = query.data ?? [];
  const portalOptions = useMemo(() => portals.data ?? [], [portals.data]);
  const countries = meta.data?.countries ?? [];
  const languages = meta.data?.languages ?? [];
  const workModalities = meta.data?.workModalities ?? [];
  const contractTypes = meta.data?.contractTypes ?? [];
  const techStacks = meta.data?.techStacks ?? [];

  const allVisibleSelected =
    items.length > 0 && items.every((item) => selectedIds.has(item.id));
  const someSelected = selectedIds.size > 0;

  const toggleSelect = (id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (allVisibleSelected) {
      setSelectedIds(new Set());
      return;
    }
    setSelectedIds(new Set(items.map((item) => item.id)));
  };

  const clearSelection = () => setSelectedIds(new Set());

  const setOfferStatus = async (item: JobOffer, next: string) => {
    if (next === "discarded") {
      const ok = await confirm({
        title: "Descartar oferta",
        description: `¿Estás seguro de descartar “${item.title}”?`,
        confirmLabel: "Descartar",
      });
      if (!ok) return;
    }
    mutations.updateStatus.mutate(
      { id: item.id, status: next },
      {
        onSuccess: () =>
          toast.success(
            next === "applied"
              ? "Marcada como postulada y agregada a Postulaciones"
              : "Estado actualizado",
          ),
        onError: (error) => toast.error(error.message),
      },
    );
  };

  const bulkStatus = async (next: string) => {
    const ids = Array.from(selectedIds);
    if (next === "discarded") {
      const ok = await confirm({
        title: "Descartar ofertas",
        description: `¿Estás seguro de descartar ${ids.length} oferta(s)?`,
        confirmLabel: "Descartar",
      });
      if (!ok) return;
    }
    mutations.bulkUpdateStatus.mutate(
      { ids, status: next },
      {
        onSuccess: (result) => {
          toast.success(
            next === "applied"
              ? `${result.affected} oferta(s) postulada(s) y agregada(s) a Postulaciones`
              : `${result.affected} oferta(s) actualizada(s)`,
          );
          clearSelection();
        },
        onError: (error) => toast.error(error.message),
      },
    );
  };

  const remove = async (item: JobOffer) => {
    const ok = await confirm({
      title: "Eliminar oferta",
      description: `¿Estás seguro de eliminar “${item.title}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Oferta eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  const bulkRemove = async () => {
    const ids = Array.from(selectedIds);
    const ok = await confirm({
      title: "Eliminar ofertas",
      description: `¿Estás seguro de eliminar ${ids.length} oferta(s)? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    mutations.bulkRemove.mutate(ids, {
      onSuccess: (result) => {
        toast.success(`${result.affected} oferta(s) eliminada(s)`);
        clearSelection();
      },
      onError: (error) => toast.error(error.message),
    });
  };

  const clearFilters = () => {
    setPortalId("all");
    setStatus("all");
    setSearch("");
    setCountry("");
    setLanguage("");
    setWorkModality("");
    setContractType("");
    setTechStack("");
    setCapturedFrom("");
    setCapturedTo("");
    setPostedFrom("");
    setPostedTo("");
    clearSelection();
  };

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <p className="text-sm text-[var(--muted)]">
        Filtra por cargo, país, idioma, stack (.NET, React…), modalidad, contrato y fechas.
        Selecciona varias ofertas para marcarlas, descartarlas o eliminarlas en bloque.
      </p>

      <div className="grid gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[var(--shadow-card)] sm:grid-cols-2 lg:grid-cols-3">
        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)] sm:col-span-2 lg:col-span-3">
          Cargo / búsqueda
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Ej. desarrollador, .net, backend…"
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Portal
          <select
            value={portalId === "all" ? "all" : String(portalId)}
            onChange={(e) =>
              setPortalId(e.target.value === "all" ? "all" : Number(e.target.value))
            }
            className={inputClass}
          >
            <option value="all">Todos</option>
            {portalOptions.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Estado
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className={inputClass}
          >
            <option value="all">Todos</option>
            <option value="new">Nuevas</option>
            <option value="seen">Vistas</option>
            <option value="discarded">Descartadas</option>
            <option value="applied">Postuladas</option>
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          País
          <select
            value={country}
            onChange={(e) => setCountry(e.target.value)}
            className={inputClass}
          >
            <option value="">Todos</option>
            {countries.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Idioma
          <select
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
            className={inputClass}
          >
            <option value="">Todos</option>
            {languages.map((lang) => (
              <option key={lang} value={lang}>
                {formatLanguage(lang)}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Modalidad
          <select
            value={workModality}
            onChange={(e) => setWorkModality(e.target.value)}
            className={inputClass}
          >
            <option value="">Todas</option>
            {workModalities.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Tipo de contrato
          <select
            value={contractType}
            onChange={(e) => setContractType(e.target.value)}
            className={inputClass}
          >
            <option value="">Todos</option>
            {contractTypes.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Lenguaje / stack
          <select
            value={techStack}
            onChange={(e) => setTechStack(e.target.value)}
            className={inputClass}
          >
            <option value="">Todos</option>
            {techStacks.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Capturada desde
          <input
            type="date"
            value={capturedFrom}
            onChange={(e) => setCapturedFrom(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Capturada hasta
          <input
            type="date"
            value={capturedTo}
            onChange={(e) => setCapturedTo(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Publicada desde
          <input
            type="date"
            value={postedFrom}
            onChange={(e) => setPostedFrom(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Publicada hasta
          <input
            type="date"
            value={postedTo}
            onChange={(e) => setPostedTo(e.target.value)}
            className={inputClass}
          />
        </label>

        <div className="flex flex-wrap items-end gap-2 sm:col-span-2 lg:col-span-3">
          <button
            type="button"
            onClick={clearFilters}
            className="rounded-md px-3 py-1.5 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
          >
            Limpiar filtros
          </button>
        </div>
      </div>

      {someSelected ? (
        <div className="sticky top-2 z-10 flex flex-wrap items-center gap-2 rounded-xl border border-[var(--accent)] bg-[var(--accent-soft)] px-4 py-3">
          <span className="text-sm font-medium">
            {selectedIds.size} seleccionada(s)
          </span>
          <button
            type="button"
            onClick={() => bulkStatus("seen")}
            disabled={mutations.bulkUpdateStatus.isPending}
            className="rounded-md bg-white px-3 py-1.5 text-xs hover:bg-[var(--surface-muted)]"
          >
            Marcar vistas
          </button>
          <button
            type="button"
            onClick={() => bulkStatus("applied")}
            disabled={mutations.bulkUpdateStatus.isPending}
            className="rounded-md bg-white px-3 py-1.5 text-xs hover:bg-[var(--surface-muted)]"
          >
            Postuladas
          </button>
          <button
            type="button"
            onClick={() => bulkStatus("discarded")}
            disabled={mutations.bulkUpdateStatus.isPending}
            className="rounded-md bg-white px-3 py-1.5 text-xs hover:bg-[var(--surface-muted)]"
          >
            Descartar
          </button>
          <button
            type="button"
            onClick={bulkRemove}
            disabled={mutations.bulkRemove.isPending}
            className="rounded-md bg-white px-3 py-1.5 text-xs text-[var(--danger)] hover:bg-red-50"
          >
            Eliminar
          </button>
          <button
            type="button"
            onClick={clearSelection}
            className="ml-auto text-xs text-[var(--muted)] hover:underline"
          >
            Cancelar selección
          </button>
        </div>
      ) : null}

      {query.isLoading && <p className="text-sm text-[var(--muted)]">Cargando…</p>}
      {query.error && (
        <p className="text-sm text-[var(--danger)]">{String(query.error)}</p>
      )}

      {!query.isLoading && !query.error && (
        !items.length ? (
          <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-6 text-sm text-[var(--muted)]">
            No hay ofertas con estos filtros.
          </p>
        ) : (
          <div className="flex flex-col gap-2">
            <label className="flex items-center gap-2 px-1 text-sm text-[var(--muted)]">
              <input
                type="checkbox"
                checked={allVisibleSelected}
                onChange={toggleSelectAll}
                className="size-4 accent-[var(--accent)]"
              />
              Seleccionar todas ({items.length})
            </label>

            {items.map((item) => {
              const selected = selectedIds.has(item.id);
              return (
                <article
                  key={item.id}
                  className={cn(
                    "rounded-xl border bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]",
                    selected
                      ? "border-[var(--accent)] ring-1 ring-[var(--accent-soft)]"
                      : "border-[var(--border)]",
                  )}
                >
                  <div className="flex flex-wrap items-start gap-3">
                    <input
                      type="checkbox"
                      checked={selected}
                      onChange={() => toggleSelect(item.id)}
                      className="mt-1 size-4 shrink-0 accent-[var(--accent)]"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">{item.title}</p>
                      <p className="text-sm text-[var(--muted)]">
                        {[item.company, item.location, item.portalName]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                      {(item.country ||
                        item.language ||
                        item.techStack ||
                        item.workModality ||
                        item.contractType) && (
                        <p className="text-xs text-[var(--muted)]">
                          {[
                            item.country,
                            item.language ? formatLanguage(item.language) : null,
                            item.techStack,
                            item.workModality,
                            item.contractType,
                          ]
                            .filter(Boolean)
                            .join(" · ")}
                        </p>
                      )}
                      {item.descriptionSnippet ? (
                        <p className="mt-1 line-clamp-2 text-sm text-[var(--muted)]">
                          {item.descriptionSnippet}
                        </p>
                      ) : null}
                      <p className="mt-1 text-xs text-[var(--muted)]">
                        {STATUS_LABELS[item.status] ?? item.status}
                        {" · "}
                        Capturada {new Date(item.capturedAt).toLocaleString()}
                        {item.postedAt
                          ? ` · Publicada ${new Date(item.postedAt).toLocaleDateString()}`
                          : ""}
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-1">
                      {item.url ? (
                        <a
                          href={item.url}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--accent)] hover:bg-[var(--surface-muted)]"
                        >
                          <ExternalLink className="size-3.5" /> Abrir
                        </a>
                      ) : null}
                      {item.status === "new" ? (
                        <button
                          type="button"
                          onClick={() => setOfferStatus(item, "seen")}
                          className="rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                        >
                          Marcar vista
                        </button>
                      ) : null}
                      <button
                        type="button"
                        onClick={() => setOfferStatus(item, "applied")}
                        className="rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                      >
                        Postulada
                      </button>
                      <button
                        type="button"
                        onClick={() => setOfferStatus(item, "discarded")}
                        className="rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                      >
                        Descartar
                      </button>
                      <button
                        type="button"
                        onClick={() => remove(item)}
                        className={cn(
                          "inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs",
                          "text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]",
                        )}
                      >
                        <Trash2 className="size-3.5" />
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )
      )}
    </div>
  );
}
