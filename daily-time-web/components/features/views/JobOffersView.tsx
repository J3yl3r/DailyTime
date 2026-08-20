"use client";

import { useMemo, useState } from "react";
import { ExternalLink, Eye, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { JobOffer, JobOfferFilters } from "@/types/api";
import { useJobOffers } from "@/hooks/queries/use-job-offers";
import { useJobOfferMeta } from "@/hooks/queries/use-job-offer-meta";
import { useJobOfferMutations } from "@/hooks/mutations/use-job-offer-mutations";
import { useJobPortals } from "@/hooks/queries/use-job-portals";
import { useConfirm } from "@/providers/confirm-provider";
import {
  DataList,
  DataListAction,
  DataListBadge,
} from "@/components/ui/DataList";
import { FormModal } from "@/components/shared/form-modal";
import { DetailField, VacancyBody } from "@/components/features/career/vacancy-detail";
import { todayApiDate } from "@/lib/utils/date";

const STATUS_LABELS: Record<string, string> = {
  new: "Nueva",
  seen: "Vista",
  discarded: "Descartada",
  applied: "Postulada",
};

const STATUS_COLORS: Record<string, string> = {
  new: "#3B82F6",
  seen: "#64748B",
  discarded: "#EF4444",
  applied: "#22C55E",
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
  const [status, setStatus] = useState("new");
  const [search, setSearch] = useState("");
  const [country, setCountry] = useState("");
  const [language, setLanguage] = useState("");
  const [workModality, setWorkModality] = useState("");
  const [contractType, setContractType] = useState("");
  const [techStack, setTechStack] = useState("");
  const [capturedFrom, setCapturedFrom] = useState(todayApiDate);
  const [capturedTo, setCapturedTo] = useState(todayApiDate);
  const [postedFrom, setPostedFrom] = useState("");
  const [postedTo, setPostedTo] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [viewing, setViewing] = useState<JobOffer | null>(null);

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

  const markAsSeenIfNew = (item: JobOffer) => {
    if (item.status !== "new") return;
    mutations.updateStatus.mutate({ id: item.id, status: "seen" });
  };

  const openView = (item: JobOffer) => {
    markAsSeenIfNew(item);
    setViewing(item.status === "new" ? { ...item, status: "seen" } : item);
  };

  const openExternal = (item: JobOffer) => {
    if (!item.url) return;
    markAsSeenIfNew(item);
    window.open(item.url, "_blank", "noopener,noreferrer");
  };

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
    const today = todayApiDate();
    setPortalId("all");
    setStatus("new");
    setSearch("");
    setCountry("");
    setLanguage("");
    setWorkModality("");
    setContractType("");
    setTechStack("");
    setCapturedFrom(today);
    setCapturedTo(today);
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
        <DataList
          items={items}
          getKey={(item) => item.id}
          emptyMessage="No hay ofertas con estos filtros."
          selected={(item) => selectedIds.has(item.id)}
          onToggleSelect={(item) => toggleSelect(item.id)}
          selectAriaLabel={(item) => `Seleccionar ${item.title}`}
          header={
            items.length > 0 ? (
              <label className="inline-flex items-center gap-2 text-sm text-[var(--muted)]">
                <input
                  type="checkbox"
                  checked={allVisibleSelected}
                  onChange={toggleSelectAll}
                />
                Seleccionar todas ({items.length})
              </label>
            ) : null
          }
          title={(item) => item.title}
          badge={(item) => (
            <DataListBadge color={STATUS_COLORS[item.status] ?? "#64748B"}>
              {STATUS_LABELS[item.status] ?? item.status}
            </DataListBadge>
          )}
          meta={(item) =>
            [
              item.company,
              item.location,
              item.portalName,
              item.country,
              item.language ? formatLanguage(item.language) : null,
              item.techStack,
              item.workModality,
              item.contractType,
              `Capturada ${new Date(item.capturedAt).toLocaleDateString()}`,
            ]
              .filter(Boolean)
              .join(" · ")
          }
          actions={(item) => (
            <>
              <DataListAction onClick={() => openView(item)}>
                <Eye className="size-3.5" /> Ver
              </DataListAction>
              {item.url ? (
                <DataListAction onClick={() => openExternal(item)}>
                  <ExternalLink className="size-3.5" /> Abrir
                </DataListAction>
              ) : null}
              {item.status === "new" ? (
                <DataListAction onClick={() => setOfferStatus(item, "seen")}>
                  Marcar vista
                </DataListAction>
              ) : null}
              <DataListAction onClick={() => setOfferStatus(item, "applied")}>
                Postulada
              </DataListAction>
              <DataListAction onClick={() => setOfferStatus(item, "discarded")}>
                Descartar
              </DataListAction>
              <DataListAction danger onClick={() => remove(item)}>
                <Trash2 className="size-3.5" />
              </DataListAction>
            </>
          )}
        />
      )}

      <FormModal
        open={viewing != null}
        onOpenChange={(open) => !open && setViewing(null)}
        title={viewing?.title ?? "Detalle de la vacante"}
        description={
          viewing
            ? [viewing.company, viewing.location, viewing.portalName]
                .filter(Boolean)
                .join(" · ")
            : undefined
        }
        size="lg"
      >
        {viewing ? (
          <div className="flex flex-col gap-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <DetailField label="Estado" value={STATUS_LABELS[viewing.status] ?? viewing.status} />
              <DetailField label="Portal" value={viewing.portalName} />
              <DetailField label="Empresa" value={viewing.company} />
              <DetailField label="Ubicación" value={viewing.location} />
              <DetailField label="País" value={viewing.country} />
              <DetailField
                label="Idioma"
                value={viewing.language ? formatLanguage(viewing.language) : null}
              />
              <DetailField label="Modalidad" value={viewing.workModality} />
              <DetailField label="Contrato" value={viewing.contractType} />
              <DetailField label="Stack" value={viewing.techStack} />
              <DetailField
                label="Publicada"
                value={
                  viewing.postedAt
                    ? new Date(viewing.postedAt).toLocaleDateString()
                    : null
                }
              />
              <DetailField
                label="Capturada"
                value={new Date(viewing.capturedAt).toLocaleString()}
              />
            </div>
            {viewing.url ? (
              <button
                type="button"
                onClick={() => openExternal(viewing)}
                className="inline-flex w-fit items-center gap-1 text-sm text-[var(--accent)] hover:underline"
              >
                <ExternalLink className="size-3.5" />
                Abrir vacante original
              </button>
            ) : null}
            <div>
              <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                Descripción
              </p>
              <VacancyBody text={viewing.description || viewing.descriptionSnippet} />
            </div>
          </div>
        ) : null}
      </FormModal>
    </div>
  );
}
