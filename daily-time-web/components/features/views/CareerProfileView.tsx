"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  Copy,
  ExternalLink,
  FileText,
  Globe,
  Languages,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Plus,
  Trash2,
  Wallet,
} from "lucide-react";
import { toast } from "sonner";
import { FormModal } from "@/components/shared/form-modal";
import { useCareerProfile } from "@/hooks/queries/use-career-profile";
import { useCareerProfileMutations } from "@/hooks/mutations/use-career-profile-mutations";
import { cn } from "@/lib/utils/cn";
import type { CareerProfile, CareerProfileInput } from "@/types/api";

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

type ProfileDraft = {
  fullName: string;
  headline: string;
  location: string;
  timezone: string;
  availability: string;
  preferredModality: string;
  email: string;
  phone: string;
  salaryMin: string;
  salaryMax: string;
  salaryCurrency: string;
  salaryPeriod: string;
  salaryNotes: string;
  summary: string;
  links: { label: string; url: string }[];
  languages: { name: string; level: string }[];
  preferredCountries: string;
  preferredStacks: string;
  strengths: string;
  education: { title: string; place: string; year: string }[];
  certifications: { title: string; issuer: string; year: string }[];
  coverLetters: {
    name: string;
    language: string;
    stack: string;
    body: string;
    isActive: boolean;
  }[];
};

function blank(value: string | null | undefined) {
  return value ?? "";
}

function lines(text: string) {
  return text
    .split("\n")
    .map((item) => item.trim())
    .filter(Boolean);
}

function toDraft(profile: CareerProfile): ProfileDraft {
  return {
    fullName: blank(profile.fullName),
    headline: blank(profile.headline),
    location: blank(profile.location),
    timezone: blank(profile.timezone),
    availability: blank(profile.availability),
    preferredModality: blank(profile.preferredModality),
    email: blank(profile.email),
    phone: blank(profile.phone),
    salaryMin: profile.salary?.min != null ? String(profile.salary.min) : "",
    salaryMax: profile.salary?.max != null ? String(profile.salary.max) : "",
    salaryCurrency: blank(profile.salary?.currency),
    salaryPeriod: blank(profile.salary?.period),
    salaryNotes: blank(profile.salary?.notes),
    summary: blank(profile.summary),
    links: profile.links.length
      ? profile.links.map((item) => ({ label: item.label, url: item.url }))
      : [{ label: "", url: "" }],
    languages: profile.languages.length
      ? profile.languages.map((item) => ({ name: item.name, level: item.level ?? "" }))
      : [{ name: "", level: "" }],
    preferredCountries: profile.preferredCountries.join("\n"),
    preferredStacks: profile.preferredStacks.join("\n"),
    strengths: profile.strengths.join("\n"),
    education: profile.education.length
      ? profile.education.map((item) => ({
          title: item.title,
          place: item.place ?? "",
          year: item.year ?? "",
        }))
      : [{ title: "", place: "", year: "" }],
    certifications: profile.certifications.length
      ? profile.certifications.map((item) => ({
          title: item.title,
          issuer: item.issuer ?? "",
          year: item.year ?? "",
        }))
      : [{ title: "", issuer: "", year: "" }],
    coverLetters: profile.coverLetters.length
      ? profile.coverLetters.map((item) => ({
          name: item.name,
          language: item.language || "es",
          stack: item.stack ?? "",
          body: item.body,
          isActive: item.isActive,
        }))
      : [{ name: "", language: "es", stack: "", body: "", isActive: true }],
  };
}

function toInput(draft: ProfileDraft): CareerProfileInput {
  const min = draft.salaryMin.trim() === "" ? null : Number(draft.salaryMin);
  const max = draft.salaryMax.trim() === "" ? null : Number(draft.salaryMax);
  return {
    fullName: draft.fullName.trim(),
    headline: draft.headline.trim() || null,
    location: draft.location.trim() || null,
    timezone: draft.timezone.trim() || null,
    availability: draft.availability.trim() || null,
    preferredModality: draft.preferredModality.trim() || null,
    email: draft.email.trim() || null,
    phone: draft.phone.trim() || null,
    salary: {
      min: Number.isFinite(min) ? min : null,
      max: Number.isFinite(max) ? max : null,
      currency: draft.salaryCurrency.trim() || null,
      period: draft.salaryPeriod.trim() || null,
      notes: draft.salaryNotes.trim() || null,
    },
    summary: draft.summary.trim() || null,
    links: draft.links
      .map((item) => ({ label: item.label.trim(), url: item.url.trim() }))
      .filter((item) => item.label && item.url),
    languages: draft.languages
      .map((item) => ({ name: item.name.trim(), level: item.level.trim() || null }))
      .filter((item) => item.name),
    preferredCountries: lines(draft.preferredCountries),
    preferredStacks: lines(draft.preferredStacks),
    strengths: lines(draft.strengths),
    education: draft.education
      .map((item) => ({
        title: item.title.trim(),
        place: item.place.trim() || null,
        year: item.year.trim() || null,
      }))
      .filter((item) => item.title),
    certifications: draft.certifications
      .map((item) => ({
        title: item.title.trim(),
        issuer: item.issuer.trim() || null,
        year: item.year.trim() || null,
      }))
      .filter((item) => item.title),
    coverLetters: draft.coverLetters
      .map((item) => ({
        name: item.name.trim(),
        language: item.language.trim() || "es",
        stack: item.stack.trim() || null,
        body: item.body.trim(),
        isActive: item.isActive,
      }))
      .filter((item) => item.name && item.body),
  };
}

function initials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

function formatSalary(s: CareerProfile["salary"] | undefined) {
  if (!s || (s.min == null && s.max == null)) return "Sin definir";
  const fmt = new Intl.NumberFormat("es-CO");
  const currency = s.currency || "USD";
  const period = s.period || "mes";
  if (s.min != null && s.max != null) {
    return `${currency} ${fmt.format(s.min)}–${fmt.format(s.max)} / ${period}`;
  }
  if (s.min != null) return `${currency} ${fmt.format(s.min)}+ / ${period}`;
  return `${currency} hasta ${fmt.format(s.max!)} / ${period}`;
}

export function CareerProfileView() {
  const query = useCareerProfile();
  const mutations = useCareerProfileMutations();
  const profile = query.data;
  const [editing, setEditing] = useState(false);
  const [letterId, setLetterId] = useState<number | null>(null);

  useEffect(() => {
    if (!profile?.coverLetters.length) {
      setLetterId(null);
      return;
    }
    setLetterId((current) =>
      current != null && profile.coverLetters.some((item) => item.id === current)
        ? current
        : profile.coverLetters[0].id,
    );
  }, [profile]);

  const letter = useMemo(
    () => profile?.coverLetters.find((item) => item.id === letterId) ?? profile?.coverLetters[0],
    [profile, letterId],
  );

  const copyLetter = async () => {
    if (!letter) return;
    try {
      await navigator.clipboard.writeText(letter.body);
      toast.success("Carta copiada");
    } catch {
      toast.error("No se pudo copiar");
    }
  };

  const save = async (draft: ProfileDraft) => {
    const body = toInput(draft);
    if (!body.fullName) {
      toast.error("El nombre es obligatorio");
      return;
    }
    try {
      await mutations.upsert.mutateAsync(body);
      setEditing(false);
      toast.success("Perfil guardado");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  };

  if (query.isLoading) {
    return <p className="text-sm text-[var(--muted)]">Cargando perfil…</p>;
  }
  if (query.error) {
    return (
      <p className="text-sm text-[var(--danger)]">
        {query.error instanceof Error ? query.error.message : "No se pudo cargar el perfil."}
      </p>
    );
  }

  const empty = !profile || profile.id === 0;
  const name = profile?.fullName || "Tu nombre";
  const letters = profile?.coverLetters ?? [];

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Ficha reutilizable para postulaciones: datos de contacto, preferencias y cartas
          base. Usa {"{cargo}"} y {"{empresa}"} al personalizar cada envío.
        </p>
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="inline-flex items-center gap-1.5 rounded-md bg-[var(--accent)] px-3 py-2 text-sm font-medium text-white"
        >
          <Pencil className="size-3.5" />
          Editar
        </button>
      </div>

      {empty && !profile?.fullName ? (
        <p className="rounded-xl border border-dashed border-[var(--border)] bg-[var(--surface)] px-5 py-8 text-center text-sm text-[var(--muted)]">
          Aún no hay un perfil guardado. Pulsa Editar para cargar tus datos de postulación.
        </p>
      ) : null}

      <section className="overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-[var(--shadow-card)]">
        <div className="flex flex-col gap-4 px-5 py-5 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <div className="flex size-14 shrink-0 items-center justify-center rounded-full bg-[var(--accent-soft)] text-lg font-semibold text-[var(--accent-strong)]">
              {initials(name) || "?"}
            </div>
            <div className="min-w-0">
              <h2 className="text-xl font-semibold text-[var(--ink)]">{name}</h2>
              <p className="text-sm text-[var(--muted)]">{profile?.headline || "Sin titular"}</p>
              <p className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-[var(--muted)]">
                {profile?.location || profile?.timezone ? (
                  <span className="inline-flex items-center gap-1">
                    <MapPin className="size-3.5" />
                    {[profile?.location, profile?.timezone].filter(Boolean).join(" · ")}
                  </span>
                ) : null}
                {profile?.availability ? <span>{profile.availability}</span> : null}
                {profile?.preferredModality ? <span>{profile.preferredModality}</span> : null}
              </p>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {(profile?.links ?? []).map((link) => (
              <a
                key={`${link.label}-${link.url}`}
                href={link.url}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1 rounded-md border border-[var(--border)] bg-white px-2.5 py-1.5 text-xs text-[var(--muted)] hover:border-[var(--accent)] hover:text-[var(--accent)]"
              >
                <ExternalLink className="size-3.5" />
                {link.label}
              </a>
            ))}
          </div>
        </div>
        {(profile?.email || profile?.phone) && (
          <div className="flex flex-wrap gap-x-5 gap-y-2 border-t border-[var(--border)] bg-[var(--surface-muted)]/60 px-5 py-3 text-sm">
            {profile.email ? (
              <a
                href={`mailto:${profile.email}`}
                className="inline-flex items-center gap-1.5 text-[var(--ink)] hover:text-[var(--accent)]"
              >
                <Mail className="size-3.5 text-[var(--muted)]" />
                {profile.email}
              </a>
            ) : null}
            {profile.phone ? (
              <span className="inline-flex items-center gap-1.5">
                <Phone className="size-3.5 text-[var(--muted)]" />
                {profile.phone}
              </span>
            ) : null}
          </div>
        )}
      </section>

      <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <InfoTile
          icon={<Wallet className="size-4" />}
          label="Expectativa salarial"
          value={formatSalary(profile?.salary)}
          hint={profile?.salary?.notes ?? undefined}
        />
        <InfoTile
          icon={<Globe className="size-4" />}
          label="Países"
          value={profile?.preferredCountries.join(" · ") || "Sin definir"}
        />
        <InfoTile
          icon={<Languages className="size-4" />}
          label="Idiomas"
          value={
            profile?.languages
              .map((item) => (item.level ? `${item.name} (${item.level})` : item.name))
              .join(" · ") || "Sin definir"
          }
        />
        <InfoTile
          icon={<FileText className="size-4" />}
          label="Plantillas de carta"
          value={`${letters.length} variante${letters.length === 1 ? "" : "s"}`}
        />
      </section>

      <section className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-4 shadow-[var(--shadow-card)]">
        <h3 className="text-sm font-semibold text-[var(--ink)]">Resumen profesional</h3>
        <p className="mt-2 text-sm leading-relaxed text-[var(--muted)]">
          {profile?.summary || "Aún no hay un resumen."}
        </p>
        <div className="mt-3 flex flex-wrap gap-1.5">
          {(profile?.preferredStacks ?? []).map((stack) => (
            <span
              key={stack}
              className="rounded-md bg-[var(--accent-soft)] px-2 py-0.5 text-[11px] font-medium text-[var(--accent-strong)]"
            >
              {stack}
            </span>
          ))}
        </div>
      </section>

      <section className="grid gap-3 lg:grid-cols-[220px_1fr]">
        <div className="rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 shadow-[var(--shadow-card)]">
          <p className="mb-2 px-1 text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
            Cartas
          </p>
          <div className="flex flex-col gap-1">
            {letters.length === 0 ? (
              <p className="px-2 py-3 text-xs text-[var(--muted)]">No hay plantillas todavía.</p>
            ) : (
              letters.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => setLetterId(item.id)}
                  className={cn(
                    "rounded-lg px-3 py-2 text-left text-sm transition-colors",
                    item.id === letter?.id
                      ? "bg-[var(--accent-soft)] font-medium text-[var(--accent-strong)]"
                      : "text-[var(--ink)] hover:bg-[var(--surface-muted)]",
                  )}
                >
                  {item.name}
                  <span className="mt-0.5 block text-[11px] font-normal text-[var(--muted)]">
                    {[item.stack, item.language.toUpperCase()].filter(Boolean).join(" · ")}
                  </span>
                </button>
              ))
            )}
          </div>
        </div>

        <article className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-4 shadow-[var(--shadow-card)]">
          <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
            <div>
              <h3 className="font-semibold text-[var(--ink)]">{letter?.name || "Carta"}</h3>
              <p className="text-xs text-[var(--muted)]">
                Usa {"{cargo}"} y {"{empresa}"} al personalizar cada postulación.
              </p>
            </div>
            <button
              type="button"
              onClick={() => void copyLetter()}
              disabled={!letter}
              className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)] disabled:opacity-40"
            >
              <Copy className="size-3.5" /> Copiar
            </button>
          </div>
          <pre className="max-h-[28rem] overflow-auto whitespace-pre-wrap rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] px-4 py-3 text-sm leading-relaxed text-[var(--ink)]">
            {letter?.body || "Aún no hay una carta guardada."}
          </pre>
        </article>
      </section>

      <section className="grid gap-3 md:grid-cols-2">
        <article className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-4 shadow-[var(--shadow-card)]">
          <h3 className="text-sm font-semibold text-[var(--ink)]">Fortalezas</h3>
          <ul className="mt-2 flex flex-col gap-1.5 pl-4 text-sm text-[var(--muted)]">
            {(profile?.strengths ?? []).map((item) => (
              <li key={item} className="list-disc">
                {item}
              </li>
            ))}
            {!profile?.strengths.length ? <li className="list-none pl-0">Sin definir.</li> : null}
          </ul>
        </article>
        <article className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-4 shadow-[var(--shadow-card)]">
          <h3 className="text-sm font-semibold text-[var(--ink)]">Formación</h3>
          <ul className="mt-3 flex flex-col gap-3">
            {(profile?.education ?? []).map((item) => (
              <li key={`${item.title}-${item.place}`}>
                <p className="text-sm font-medium text-[var(--ink)]">{item.title}</p>
                <p className="text-xs text-[var(--muted)]">
                  {[item.place, item.year && item.year !== "—" ? item.year : null]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              </li>
            ))}
            {(profile?.certifications ?? []).map((item) => (
              <li key={`${item.title}-${item.issuer}`}>
                <p className="text-sm font-medium text-[var(--ink)]">{item.title}</p>
                <p className="text-xs text-[var(--muted)]">
                  {[item.issuer, item.year && item.year !== "—" ? item.year : null]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              </li>
            ))}
            {!profile?.education.length && !profile?.certifications.length ? (
              <li className="text-sm text-[var(--muted)]">Sin definir.</li>
            ) : null}
          </ul>
        </article>
      </section>

      <FormModal
        open={editing}
        onOpenChange={(open) => !open && setEditing(false)}
        title="Editar perfil de postulación"
        description="Estos datos se reutilizan al postular. Las cartas pueden usar {cargo} y {empresa}."
        size="lg"
      >
        {editing && profile ? (
          <ProfileForm
            initial={toDraft(profile)}
            onClose={() => setEditing(false)}
            onSubmit={save}
            pending={mutations.upsert.isPending}
          />
        ) : null}
      </FormModal>
    </div>
  );
}

function ProfileForm({
  initial,
  onSubmit,
  onClose,
  pending,
}: {
  initial: ProfileDraft;
  onSubmit: (draft: ProfileDraft) => void;
  onClose: () => void;
  pending?: boolean;
}) {
  const [draft, setDraft] = useState<ProfileDraft>(initial);

  return (
    <form
      className="flex flex-col gap-5"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(draft);
      }}
    >
      <section className="grid gap-3 sm:grid-cols-2">
        <Field label="Nombre completo">
          <input
            value={draft.fullName}
            onChange={(e) => setDraft((c) => ({ ...c, fullName: e.target.value }))}
            className={inputClass}
            autoFocus
          />
        </Field>
        <Field label="Titular">
          <input
            value={draft.headline}
            onChange={(e) => setDraft((c) => ({ ...c, headline: e.target.value }))}
            className={inputClass}
            placeholder="Desarrollador .NET y React"
          />
        </Field>
        <Field label="Ubicación">
          <input
            value={draft.location}
            onChange={(e) => setDraft((c) => ({ ...c, location: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Zona horaria">
          <input
            value={draft.timezone}
            onChange={(e) => setDraft((c) => ({ ...c, timezone: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Disponibilidad">
          <input
            value={draft.availability}
            onChange={(e) => setDraft((c) => ({ ...c, availability: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Modalidad">
          <input
            value={draft.preferredModality}
            onChange={(e) => setDraft((c) => ({ ...c, preferredModality: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Correo">
          <input
            type="email"
            value={draft.email}
            onChange={(e) => setDraft((c) => ({ ...c, email: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Teléfono">
          <input
            value={draft.phone}
            onChange={(e) => setDraft((c) => ({ ...c, phone: e.target.value }))}
            className={inputClass}
          />
        </Field>
      </section>

      <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Field label="Salario mín.">
          <input
            type="number"
            value={draft.salaryMin}
            onChange={(e) => setDraft((c) => ({ ...c, salaryMin: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Salario máx.">
          <input
            type="number"
            value={draft.salaryMax}
            onChange={(e) => setDraft((c) => ({ ...c, salaryMax: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Moneda">
          <input
            value={draft.salaryCurrency}
            onChange={(e) => setDraft((c) => ({ ...c, salaryCurrency: e.target.value }))}
            className={inputClass}
          />
        </Field>
        <Field label="Periodo">
          <input
            value={draft.salaryPeriod}
            onChange={(e) => setDraft((c) => ({ ...c, salaryPeriod: e.target.value }))}
            className={inputClass}
            placeholder="mes"
          />
        </Field>
      </section>
      <Field label="Notas de salario">
        <input
          value={draft.salaryNotes}
          onChange={(e) => setDraft((c) => ({ ...c, salaryNotes: e.target.value }))}
          className={inputClass}
        />
      </Field>
      <Field label="Resumen profesional">
        <textarea
          value={draft.summary}
          onChange={(e) => setDraft((c) => ({ ...c, summary: e.target.value }))}
          rows={4}
          className={inputClass}
        />
      </Field>

      <RepeatList
        title="Enlaces"
        items={draft.links}
        onAdd={() => setDraft((c) => ({ ...c, links: [...c.links, { label: "", url: "" }] }))}
        onRemove={(index) =>
          setDraft((c) => ({ ...c, links: c.links.filter((_, i) => i !== index) }))
        }
        renderItem={(item, index) => (
          <div className="grid gap-2 sm:grid-cols-2">
            <input
              value={item.label}
              placeholder="LinkedIn"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  links: c.links.map((row, i) =>
                    i === index ? { ...row, label: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.url}
              placeholder="https://…"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  links: c.links.map((row, i) =>
                    i === index ? { ...row, url: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
          </div>
        )}
      />

      <RepeatList
        title="Idiomas"
        items={draft.languages}
        onAdd={() => setDraft((c) => ({ ...c, languages: [...c.languages, { name: "", level: "" }] }))}
        onRemove={(index) =>
          setDraft((c) => ({ ...c, languages: c.languages.filter((_, i) => i !== index) }))
        }
        renderItem={(item, index) => (
          <div className="grid gap-2 sm:grid-cols-2">
            <input
              value={item.name}
              placeholder="Español"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  languages: c.languages.map((row, i) =>
                    i === index ? { ...row, name: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.level}
              placeholder="Nativo"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  languages: c.languages.map((row, i) =>
                    i === index ? { ...row, level: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
          </div>
        )}
      />

      <div className="grid gap-3 md:grid-cols-3">
        <Field label="Países (uno por línea)">
          <textarea
            value={draft.preferredCountries}
            onChange={(e) => setDraft((c) => ({ ...c, preferredCountries: e.target.value }))}
            rows={4}
            className={inputClass}
          />
        </Field>
        <Field label="Stacks (uno por línea)">
          <textarea
            value={draft.preferredStacks}
            onChange={(e) => setDraft((c) => ({ ...c, preferredStacks: e.target.value }))}
            rows={4}
            className={inputClass}
          />
        </Field>
        <Field label="Fortalezas (una por línea)">
          <textarea
            value={draft.strengths}
            onChange={(e) => setDraft((c) => ({ ...c, strengths: e.target.value }))}
            rows={4}
            className={inputClass}
          />
        </Field>
      </div>

      <RepeatList
        title="Formación"
        items={draft.education}
        onAdd={() =>
          setDraft((c) => ({ ...c, education: [...c.education, { title: "", place: "", year: "" }] }))
        }
        onRemove={(index) =>
          setDraft((c) => ({ ...c, education: c.education.filter((_, i) => i !== index) }))
        }
        renderItem={(item, index) => (
          <div className="grid gap-2 sm:grid-cols-3">
            <input
              value={item.title}
              placeholder="Título"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  education: c.education.map((row, i) =>
                    i === index ? { ...row, title: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.place}
              placeholder="Institución"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  education: c.education.map((row, i) =>
                    i === index ? { ...row, place: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.year}
              placeholder="Año"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  education: c.education.map((row, i) =>
                    i === index ? { ...row, year: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
          </div>
        )}
      />

      <RepeatList
        title="Certificaciones"
        items={draft.certifications}
        onAdd={() =>
          setDraft((c) => ({
            ...c,
            certifications: [...c.certifications, { title: "", issuer: "", year: "" }],
          }))
        }
        onRemove={(index) =>
          setDraft((c) => ({
            ...c,
            certifications: c.certifications.filter((_, i) => i !== index),
          }))
        }
        renderItem={(item, index) => (
          <div className="grid gap-2 sm:grid-cols-3">
            <input
              value={item.title}
              placeholder="Certificación"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  certifications: c.certifications.map((row, i) =>
                    i === index ? { ...row, title: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.issuer}
              placeholder="Emisor"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  certifications: c.certifications.map((row, i) =>
                    i === index ? { ...row, issuer: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
            <input
              value={item.year}
              placeholder="Año"
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  certifications: c.certifications.map((row, i) =>
                    i === index ? { ...row, year: e.target.value } : row,
                  ),
                }))
              }
              className={inputClass}
            />
          </div>
        )}
      />

      <RepeatList
        title="Cartas de presentación"
        items={draft.coverLetters}
        onAdd={() =>
          setDraft((c) => ({
            ...c,
            coverLetters: [
              ...c.coverLetters,
              { name: "", language: "es", stack: "", body: "", isActive: true },
            ],
          }))
        }
        onRemove={(index) =>
          setDraft((c) => ({
            ...c,
            coverLetters: c.coverLetters.filter((_, i) => i !== index),
          }))
        }
        renderItem={(item, index) => (
          <div className="flex flex-col gap-2">
            <div className="grid gap-2 sm:grid-cols-3">
              <input
                value={item.name}
                placeholder="Nombre de la plantilla"
                onChange={(e) =>
                  setDraft((c) => ({
                    ...c,
                    coverLetters: c.coverLetters.map((row, i) =>
                      i === index ? { ...row, name: e.target.value } : row,
                    ),
                  }))
                }
                className={inputClass}
              />
              <input
                value={item.language}
                placeholder="es"
                onChange={(e) =>
                  setDraft((c) => ({
                    ...c,
                    coverLetters: c.coverLetters.map((row, i) =>
                      i === index ? { ...row, language: e.target.value } : row,
                    ),
                  }))
                }
                className={inputClass}
              />
              <input
                value={item.stack}
                placeholder="Stack"
                onChange={(e) =>
                  setDraft((c) => ({
                    ...c,
                    coverLetters: c.coverLetters.map((row, i) =>
                      i === index ? { ...row, stack: e.target.value } : row,
                    ),
                  }))
                }
                className={inputClass}
              />
            </div>
            <textarea
              value={item.body}
              onChange={(e) =>
                setDraft((c) => ({
                  ...c,
                  coverLetters: c.coverLetters.map((row, i) =>
                    i === index ? { ...row, body: e.target.value } : row,
                  ),
                }))
              }
              rows={8}
              className={inputClass}
              placeholder="Hola, me presento para {cargo} en {empresa}…"
            />
          </div>
        )}
      />

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
          {pending ? "Guardando…" : "Guardar perfil"}
        </button>
      </div>
    </form>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5 text-sm font-medium">
      {label}
      {children}
    </label>
  );
}

function RepeatList<T>({
  title,
  items,
  onAdd,
  onRemove,
  renderItem,
}: {
  title: string;
  items: T[];
  onAdd: () => void;
  onRemove: (index: number) => void;
  renderItem: (item: T, index: number) => ReactNode;
}) {
  return (
    <section className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">{title}</h3>
        <button
          type="button"
          onClick={onAdd}
          className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--accent)] hover:bg-[var(--accent-soft)]"
        >
          <Plus className="size-3.5" /> Agregar
        </button>
      </div>
      <div className="flex flex-col gap-3">
        {items.map((item, index) => (
          <div
            key={index}
            className="flex items-start gap-2 rounded-lg border border-[var(--border)] p-3"
          >
            <div className="min-w-0 flex-1">{renderItem(item, index)}</div>
            <button
              type="button"
              onClick={() => onRemove(index)}
              className="mt-1 rounded-md p-1 text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--danger)]"
              aria-label="Quitar"
            >
              <Trash2 className="size-3.5" />
            </button>
          </div>
        ))}
      </div>
    </section>
  );
}

function InfoTile({
  icon,
  label,
  value,
  hint,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3 shadow-[var(--shadow-card)]">
      <p className="inline-flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wide text-[var(--muted)]">
        {icon}
        {label}
      </p>
      <p className="mt-1 text-sm font-medium text-[var(--ink)]">{value}</p>
      {hint ? <p className="mt-1 text-xs text-[var(--muted)]">{hint}</p> : null}
    </div>
  );
}
