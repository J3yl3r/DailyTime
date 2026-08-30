"use client";

import { useEffect, useMemo, useState } from "react";
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
import {
  MultiSelect,
  FilterPillsBar,
  type MultiSelectOption,
} from "@/components/ui/MultiSelect";
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
  const [selectedPortalIds, setSelectedPortalIds] = useState<number[]>([]);
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>(["new"]);
  const [search, setSearch] = useState("");
  const [selectedCountries, setSelectedCountries] = useState<string[]>([]);
  const [selectedLanguages, setSelectedLanguages] = useState<string[]>([]);
  const [selectedWorkModalities, setSelectedWorkModalities] = useState<string[]>([]);
  const [selectedContractTypes, setSelectedContractTypes] = useState<string[]>([]);
  const [selectedTechStacks, setSelectedTechStacks] = useState<string[]>([]);
  const [capturedFrom, setCapturedFrom] = useState(todayApiDate);
  const [capturedTo, setCapturedTo] = useState(todayApiDate);
  const [postedFrom, setPostedFrom] = useState("");
  const [postedTo, setPostedTo] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [viewing, setViewing] = useState<JobOffer | null>(null);
  const [localItems, setLocalItems] = useState<JobOffer[] | null>(null);

  const filters = useMemo<JobOfferFilters>(() => {
    const f: JobOfferFilters = {};
    if (selectedPortalIds.length > 0) f.portalIds = selectedPortalIds;
    if (selectedStatuses.length > 0) f.statuses = selectedStatuses;
    if (search.trim()) f.search = search.trim();
    if (selectedCountries.length > 0) f.countries = selectedCountries;
    if (selectedLanguages.length > 0) f.languages = selectedLanguages;
    if (selectedWorkModalities.length > 0) f.workModalities = selectedWorkModalities;
    if (selectedContractTypes.length > 0) f.contractTypes = selectedContractTypes;
    if (selectedTechStacks.length > 0) f.techStacks = selectedTechStacks;
    if (capturedFrom) f.capturedFrom = capturedFrom;
    if (capturedTo) f.capturedTo = capturedTo;
    if (postedFrom) f.postedFrom = postedFrom;
    if (postedTo) f.postedTo = postedTo;
    return f;
  }, [
    selectedPortalIds,
    selectedStatuses,
    search,
    selectedCountries,
    selectedLanguages,
    selectedWorkModalities,
    selectedContractTypes,
    selectedTechStacks,
    capturedFrom,
    capturedTo,
    postedFrom,
    postedTo,
  ]);

  const query = useJobOffers(filters);
  const mutations = useJobOfferMutations();

  useEffect(() => {
    setLocalItems(null);
  }, [query.data]);

  const items = localItems ?? query.data ?? [];
  const portalOptions = useMemo(() => portals.data ?? [], [portals.data]);
  const countries = meta.data?.countries ?? [];
  const languages = meta.data?.languages ?? [];
  const workModalities = meta.data?.workModalities ?? [];
  const contractTypes = meta.data?.contractTypes ?? [];
  const techStacks = meta.data?.techStacks ?? [];

  const handleReorder = (newItems: JobOffer[]) => {
    setLocalItems(newItems);
    const ids = newItems.map((item) => item.id);
    mutations.reorder.mutate(ids, {
      onError: (error) => {
        setLocalItems(null);
        toast.error(
          error instanceof Error ? error.message : "No se pudo guardar el orden"
        );
      },
    });
  };

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

  const portalMultiOptions: MultiSelectOption[] = useMemo(
    () => portalOptions.map((p) => ({ value: p.id, label: p.name })),
    [portalOptions]
  );

  const statusMultiOptions: MultiSelectOption[] = useMemo(
    () =>
      Object.entries(STATUS_LABELS).map(([value, label]) => ({
        value,
        label,
        color: STATUS_COLORS[value],
      })),
    []
  );

  const countryMultiOptions: MultiSelectOption[] = useMemo(
    () => countries.map((c) => ({ value: c, label: c })),
    [countries]
  );

  const languageMultiOptions: MultiSelectOption[] = useMemo(
    () => languages.map((l) => ({ value: l, label: formatLanguage(l) })),
    [languages]
  );

  const modalityMultiOptions: MultiSelectOption[] = useMemo(
    () => workModalities.map((m) => ({ value: m, label: m })),
    [workModalities]
  );

  const contractMultiOptions: MultiSelectOption[] = useMemo(
    () => contractTypes.map((c) => ({ value: c, label: c })),
    [contractTypes]
  );

  const techStackMultiOptions: MultiSelectOption[] = useMemo(
    () => techStacks.map((s) => ({ value: s, label: s })),
    [techStacks]
  );

  const clearFilters = () => {
    const today = todayApiDate();
    setSelectedPortalIds([]);
    setSelectedStatuses(["new"]);
    setSearch("");
    setSelectedCountries([]);
    setSelectedLanguages([]);
    setSelectedWorkModalities([]);
    setSelectedContractTypes([]);
    setSelectedTechStacks([]);
    setCapturedFrom(today);
    setCapturedTo(today);
    setPostedFrom("");
    setPostedTo("");
    clearSelection();
  };

  const activePills = useMemo(() => {
    const pills: { id: string; label: string; onRemove: () => void }[] = [];

    selectedPortalIds.forEach((id) => {
      const p = portalOptions.find((o) => o.id === id);
      if (p) {
        pills.push({
          id: `portal-${id}`,
          label: `Portal: ${p.name}`,
          onRemove: () =>
            setSelectedPortalIds((prev) => prev.filter((x) => x !== id)),
        });
      }
    });

    selectedStatuses.forEach((st) => {
      pills.push({
        id: `status-${st}`,
        label: `Estado: ${STATUS_LABELS[st] ?? st}`,
        onRemove: () =>
          setSelectedStatuses((prev) => prev.filter((x) => x !== st)),
      });
    });

    selectedCountries.forEach((c) => {
      pills.push({
        id: `country-${c}`,
        label: `País: ${c}`,
        onRemove: () =>
          setSelectedCountries((prev) => prev.filter((x) => x !== c)),
      });
    });

    selectedLanguages.forEach((l) => {
      pills.push({
        id: `lang-${l}`,
        label: `Idioma: ${formatLanguage(l)}`,
        onRemove: () =>
          setSelectedLanguages((prev) => prev.filter((x) => x !== l)),
      });
    });

    selectedWorkModalities.forEach((m) => {
      pills.push({
        id: `modality-${m}`,
        label: `Modalidad: ${m}`,
        onRemove: () =>
          setSelectedWorkModalities((prev) => prev.filter((x) => x !== m)),
      });
    });

    selectedContractTypes.forEach((c) => {
      pills.push({
        id: `contract-${c}`,
        label: `Contrato: ${c}`,
        onRemove: () =>
          setSelectedContractTypes((prev) => prev.filter((x) => x !== c)),
      });
    });

    selectedTechStacks.forEach((s) => {
      pills.push({
        id: `stack-${s}`,
        label: `Stack: ${s}`,
        onRemove: () =>
          setSelectedTechStacks((prev) => prev.filter((x) => x !== s)),
      });
    });

    return pills;
  }, [
    selectedPortalIds,
    portalOptions,
    selectedStatuses,
    selectedCountries,
    selectedLanguages,
    selectedWorkModalities,
    selectedContractTypes,
    selectedTechStacks,
  ]);

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <p className="text-sm text-[var(--muted)]">
        Filtra por selección múltiple de cargo, portales, país, idioma, stack (.NET, React…), modalidad, contrato y fechas.
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

        <MultiSelect
          label="Portales"
          placeholder="Todos los portales"
          options={portalMultiOptions}
          value={selectedPortalIds}
          onChange={setSelectedPortalIds}
        />

        <MultiSelect
          label="Estados"
          placeholder="Todos los estados"
          options={statusMultiOptions}
          value={selectedStatuses}
          onChange={setSelectedStatuses}
        />

        <MultiSelect
          label="Países"
          placeholder="Todos los países"
          options={countryMultiOptions}
          value={selectedCountries}
          onChange={setSelectedCountries}
        />

        <MultiSelect
          label="Idiomas"
          placeholder="Todos los idiomas"
          options={languageMultiOptions}
          value={selectedLanguages}
          onChange={setSelectedLanguages}
        />

        <MultiSelect
          label="Modalidad"
          placeholder="Todas las modalidades"
          options={modalityMultiOptions}
          value={selectedWorkModalities}
          onChange={setSelectedWorkModalities}
        />

        <MultiSelect
          label="Tipo de contrato"
          placeholder="Todos los contratos"
          options={contractMultiOptions}
          value={selectedContractTypes}
          onChange={setSelectedContractTypes}
        />

        <MultiSelect
          label="Lenguaje / stack"
          placeholder="Todos los stacks"
          options={techStackMultiOptions}
          value={selectedTechStacks}
          onChange={setSelectedTechStacks}
          className="sm:col-span-2 lg:col-span-3"
        />

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

        <div className="flex flex-wrap items-center justify-between gap-2 sm:col-span-2 lg:col-span-3">
          <button
            type="button"
            onClick={clearFilters}
            className="rounded-md px-3 py-1.5 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
          >
            Limpiar filtros
          </button>
        </div>

        {activePills.length > 0 && (
          <div className="border-t border-[var(--border)] pt-2 sm:col-span-2 lg:col-span-3">
            <FilterPillsBar pills={activePills} onClearAll={clearFilters} />
          </div>
        )}
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
          isSortable
          onReorder={handleReorder}
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
