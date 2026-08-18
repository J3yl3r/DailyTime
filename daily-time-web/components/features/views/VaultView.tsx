"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Copy,
  Eye,
  EyeOff,
  ExternalLink,
  KeyRound,
  Pencil,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import type { VaultAccount, VaultPassword } from "@/types/api";
import type { VaultCredentialDraft } from "@/types/vault";
import { parseVaultTags } from "@/types/vault";
import { useVaultAccounts } from "@/hooks/queries/use-vault-accounts";
import { useVaultPasswords } from "@/hooks/queries/use-vault-passwords";
import { useVaultServices } from "@/hooks/queries/use-vault-services";
import { useVaultAccountMutations } from "@/hooks/mutations/use-vault-account-mutations";
import { useVaultPasswordMutations } from "@/hooks/mutations/use-vault-password-mutations";
import { CreatePanel } from "@/components/ui/CreatePanel";
import { FormModal } from "@/components/shared/form-modal";
import { cn } from "@/lib/utils/cn";
import { useConfirm } from "@/providers/confirm-provider";
import Link from "next/link";
import type { VaultService } from "@/types/api";

const EMPTY_DRAFT: VaultCredentialDraft = {
  serviceId: "",
  username: "",
  password: "",
  url: "",
  notes: "",
  tags: "",
};

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

function AccountForm({
  initialName = "",
  initialDescription = "",
  onSubmit,
  onClose,
  submitLabel,
  pending,
  showCancel = true,
}: {
  initialName?: string;
  initialDescription?: string;
  onSubmit: (values: { name: string; description: string }) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
  showCancel?: boolean;
}) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!name.trim()) {
          toast.error("El nombre de la cuenta es obligatorio");
          return;
        }
        onSubmit({ name: name.trim(), description: description.trim() });
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Nombre de la cuenta
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Mi bóveda personal"
          className={inputClass}
          autoFocus
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Descripción (opcional)
        <textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          rows={3}
          placeholder="Para qué usas esta bóveda"
          className={inputClass}
        />
      </label>
      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
        {showCancel ? (
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-[var(--border)] px-4 py-2 text-sm"
          >
            Cancelar
          </button>
        ) : null}
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

function CredentialForm({
  initial,
  services,
  onSubmit,
  onClose,
  submitLabel,
  pending,
}: {
  initial?: VaultCredentialDraft;
  services: VaultService[];
  onSubmit: (draft: VaultCredentialDraft) => void;
  onClose: () => void;
  submitLabel: string;
  pending?: boolean;
}) {
  const [draft, setDraft] = useState<VaultCredentialDraft>(initial ?? EMPTY_DRAFT);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!draft.serviceId || !draft.username.trim() || !draft.password.trim()) {
          toast.error("Servicio, usuario y contraseña son obligatorios");
          return;
        }
        onSubmit(draft);
      }}
    >
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Servicio
        <select
          value={draft.serviceId}
          onChange={(event) => {
            const serviceId = event.target.value
              ? Number(event.target.value)
              : "";
            const selected = services.find((s) => s.id === serviceId);
            setDraft((current) => ({
              ...current,
              serviceId,
              url: current.url || selected?.url || "",
            }));
          }}
          className={inputClass}
          autoFocus
        >
          <option value="">Selecciona un servicio…</option>
          {services.map((service) => (
            <option key={service.id} value={service.id}>
              {service.name}
            </option>
          ))}
        </select>
        {!services.length ? (
          <span className="text-xs font-normal text-[var(--muted)]">
            Primero crea servicios en{" "}
            <Link href="/vault/services" className="text-[var(--accent)] underline">
              Bóveda → Servicios
            </Link>
            .
          </span>
        ) : null}
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Usuario / correo
        <input
          value={draft.username}
          onChange={(event) =>
            setDraft((current) => ({ ...current, username: event.target.value }))
          }
          placeholder="yo@empresa.com"
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Contraseña
        <input
          type="text"
          value={draft.password}
          onChange={(event) =>
            setDraft((current) => ({ ...current, password: event.target.value }))
          }
          placeholder="••••••••"
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        URL
        <input
          value={draft.url}
          onChange={(event) =>
            setDraft((current) => ({ ...current, url: event.target.value }))
          }
          placeholder="https://..."
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Etiquetas (separadas por coma)
        <input
          value={draft.tags}
          onChange={(event) =>
            setDraft((current) => ({ ...current, tags: event.target.value }))
          }
          placeholder="trabajo, email"
          className={inputClass}
        />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">
        Notas
        <textarea
          value={draft.notes}
          onChange={(event) =>
            setDraft((current) => ({ ...current, notes: event.target.value }))
          }
          rows={3}
          className={inputClass}
        />
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

export function VaultView() {
  const accountsQuery = useVaultAccounts();
  const accounts = accountsQuery.data ?? [];
  const [selectedAccountId, setSelectedAccountId] = useState<number | null>(null);
  const [query, setQuery] = useState("");
  const [tagFilter, setTagFilter] = useState<string | "all">("all");
  const [creatingAccount, setCreatingAccount] = useState(false);
  const [editingAccount, setEditingAccount] = useState<VaultAccount | null>(null);
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<VaultPassword | null>(null);
  const [revealed, setRevealed] = useState<Record<number, boolean>>({});
  const confirm = useConfirm();

  useEffect(() => {
    if (!accounts.length) {
      setSelectedAccountId(null);
      return;
    }
    if (
      selectedAccountId == null ||
      !accounts.some((account) => account.id === selectedAccountId)
    ) {
      setSelectedAccountId(accounts[0].id);
    }
  }, [accounts, selectedAccountId]);

  const passwordsQuery = useVaultPasswords(selectedAccountId);
  const servicesQuery = useVaultServices(true);
  const services = servicesQuery.data ?? [];
  const accountMutations = useVaultAccountMutations();
  const passwordMutations = useVaultPasswordMutations(selectedAccountId);
  const selectedAccount =
    accounts.find((account) => account.id === selectedAccountId) ?? null;
  const items = passwordsQuery.data ?? [];

  const allTags = useMemo(() => {
    const tags = new Set<string>();
    items.forEach((item) => {
      parseVaultTags(item.tags).forEach((tag) => tags.add(tag));
    });
    return Array.from(tags).sort();
  }, [items]);

  const filtered = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return items.filter((item) => {
      const tags = parseVaultTags(item.tags);
      if (tagFilter !== "all" && !tags.includes(tagFilter)) return false;
      if (!needle) return true;
      return (
        item.serviceName.toLowerCase().includes(needle) ||
        item.username.toLowerCase().includes(needle) ||
        (item.notes ?? "").toLowerCase().includes(needle) ||
        tags.some((tag) => tag.includes(needle))
      );
    });
  }, [items, query, tagFilter]);

  const copyText = async (label: string, value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      toast.success(`${label} copiado`);
    } catch {
      toast.error("No se pudo copiar");
    }
  };

  const createAccount = async (values: { name: string; description: string }) => {
    try {
      const created = await accountMutations.create.mutateAsync(values);
      setSelectedAccountId(created.id);
      setCreatingAccount(false);
      toast.success("Cuenta de bóveda creada");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al crear la cuenta");
    }
  };

  const updateAccount = async (values: { name: string; description: string }) => {
    if (!editingAccount) return;
    try {
      await accountMutations.update.mutateAsync({
        id: editingAccount.id,
        body: values,
      });
      setEditingAccount(null);
      toast.success("Cuenta actualizada");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al actualizar");
    }
  };

  const removeAccount = async (account: VaultAccount) => {
    const ok = await confirm({
      title: "Eliminar cuenta",
      description: `¿Estás seguro de eliminar la cuenta “${account.name}” y todas sus contraseñas? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    accountMutations.remove.mutate(account.id, {
      onSuccess: () => toast.success("Cuenta eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  const addPassword = async (draft: VaultCredentialDraft) => {
    if (draft.serviceId === "") {
      toast.error("Selecciona un servicio");
      return;
    }
    try {
      await passwordMutations.create.mutateAsync({
        serviceId: Number(draft.serviceId),
        username: draft.username.trim(),
        password: draft.password,
        url: draft.url.trim(),
        notes: draft.notes.trim(),
        tags: draft.tags.trim(),
      });
      setCreating(false);
      toast.success("Contraseña guardada");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  };

  const updatePassword = async (draft: VaultCredentialDraft) => {
    if (!editing || draft.serviceId === "") return;
    try {
      await passwordMutations.update.mutateAsync({
        id: editing.id,
        body: {
          serviceId: Number(draft.serviceId),
          username: draft.username.trim(),
          password: draft.password,
          url: draft.url.trim(),
          notes: draft.notes.trim(),
          tags: draft.tags.trim(),
        },
      });
      setEditing(null);
      toast.success("Contraseña actualizada");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al actualizar");
    }
  };

  const removePassword = async (item: VaultPassword) => {
    const ok = await confirm({
      title: "Eliminar contraseña",
      description: `¿Estás seguro de eliminar “${item.serviceName}”? Esta acción no se puede deshacer.`,
      confirmLabel: "Eliminar",
    });
    if (!ok) return;
    passwordMutations.remove.mutate(item.id, {
      onSuccess: () => toast.success("Contraseña eliminada"),
      onError: (error) => toast.error(error.message),
    });
  };

  if (accountsQuery.isLoading) {
    return <p className="text-sm text-[var(--muted)]">Cargando bóveda…</p>;
  }

  if (accountsQuery.error) {
    return (
      <p className="text-sm text-[var(--danger)]">
        {String(accountsQuery.error)}
      </p>
    );
  }

  if (!accounts.length) {
    return (
      <div className="mx-auto flex max-w-lg flex-col gap-5">
        <section className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-5 py-6 shadow-[var(--shadow-card)]">
          <div className="mb-4 flex items-start gap-3">
            <span className="rounded-lg bg-[var(--accent-soft)] p-2 text-[var(--accent)]">
              <KeyRound className="size-4" />
            </span>
            <div>
              <h2 className="font-[family-name:var(--font-fraunces)] text-lg font-semibold text-[var(--ink)]">
                Crea tu cuenta de bóveda
              </h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Primero crea una cuenta; después podrás guardar N contraseñas
                dentro de ella.
              </p>
            </div>
          </div>
          <AccountForm
            onClose={() => undefined}
            onSubmit={createAccount}
            submitLabel="Crear cuenta"
            pending={accountMutations.create.isPending}
            showCancel={false}
          />
        </section>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-5">
      <section className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)]/80 px-4 py-3 shadow-[var(--shadow-card)]">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex items-start gap-3">
            <span className="mt-0.5 rounded-lg bg-[var(--accent-soft)] p-2 text-[var(--accent)]">
              <KeyRound className="size-4" />
            </span>
            <div>
              <p className="text-sm font-medium text-[var(--ink)]">
                Bóveda de credenciales
              </p>
              <p className="text-sm text-[var(--muted)]">
                Una cuenta con N contraseñas guardadas en la base de datos.
              </p>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <CreatePanel
              open={creatingAccount}
              onOpen={() => setCreatingAccount(true)}
              onClose={() => setCreatingAccount(false)}
              label="Nueva cuenta"
            >
              <AccountForm
                onClose={() => setCreatingAccount(false)}
                onSubmit={createAccount}
                submitLabel="Crear"
                pending={accountMutations.create.isPending}
              />
            </CreatePanel>
            {selectedAccount && (
              <CreatePanel
                open={creating}
                onOpen={() => setCreating(true)}
                onClose={() => setCreating(false)}
                label="Nueva contraseña"
              >
                <CredentialForm
                  services={services}
                  onClose={() => setCreating(false)}
                  onSubmit={addPassword}
                  submitLabel="Guardar"
                  pending={passwordMutations.create.isPending}
                />
              </CreatePanel>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-end gap-3 border-t border-[var(--border)] pt-3">
          <label className="flex min-w-[14rem] flex-1 flex-col gap-1 text-xs font-medium text-[var(--muted)]">
            Cuenta activa
            <select
              value={selectedAccountId ?? ""}
              onChange={(event) => setSelectedAccountId(Number(event.target.value))}
              className="h-9 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--ink)] outline-none focus:border-[var(--accent)]"
            >
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} ({account.passwordCount})
                </option>
              ))}
            </select>
          </label>
          {selectedAccount && (
            <div className="flex gap-1">
              <button
                type="button"
                onClick={() => setEditingAccount(selectedAccount)}
                className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
              >
                <Pencil className="size-3.5" /> Editar cuenta
              </button>
              <button
                type="button"
                onClick={() => removeAccount(selectedAccount)}
                className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
              >
                <Trash2 className="size-3.5" /> Eliminar cuenta
              </button>
            </div>
          )}
        </div>
        {selectedAccount?.description ? (
          <p className="text-sm text-[var(--muted)]">{selectedAccount.description}</p>
        ) : null}
      </section>

      <div className="flex flex-wrap items-center gap-3">
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Buscar servicio, usuario o nota…"
          className="h-9 min-w-[16rem] flex-1 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm outline-none focus:border-[var(--accent)]"
        />
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setTagFilter("all")}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm",
              tagFilter === "all"
                ? "bg-[var(--accent)] text-white"
                : "border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)]",
            )}
          >
            Todas
          </button>
          {allTags.map((tag) => (
            <button
              key={tag}
              type="button"
              onClick={() => setTagFilter(tag)}
              className={cn(
                "rounded-md px-3 py-1.5 text-sm capitalize",
                tagFilter === tag
                  ? "bg-[var(--accent)] text-white"
                  : "border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)]",
              )}
            >
              {tag}
            </button>
          ))}
        </div>
      </div>

      {passwordsQuery.isLoading && (
        <p className="text-sm text-[var(--muted)]">Cargando contraseñas…</p>
      )}
      {passwordsQuery.error && (
        <p className="text-sm text-[var(--danger)]">{String(passwordsQuery.error)}</p>
      )}
      {!passwordsQuery.isLoading && !passwordsQuery.error && !filtered.length ? (
        <p className="rounded-xl border border-dashed border-[var(--border)] px-4 py-10 text-center text-sm text-[var(--muted)]">
          {items.length
            ? "No hay contraseñas con este filtro."
            : "Esta cuenta aún no tiene contraseñas. Agrega la primera."}
        </p>
      ) : null}
      {!passwordsQuery.isLoading && !passwordsQuery.error && filtered.length > 0 ? (
        <ul className="flex flex-col gap-3">
          {filtered.map((item) => {
            const tags = parseVaultTags(item.tags);
            const visible = revealed[item.id] === true;
            return (
              <li
                key={item.id}
                className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-4 py-4 shadow-[var(--shadow-card)]"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="font-medium text-[var(--ink)]">{item.serviceName}</h2>
                      {tags.map((tag) => (
                        <span
                          key={tag}
                          className="rounded-md bg-[var(--surface-muted)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-[var(--muted)]"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                    {item.notes ? (
                      <p className="mt-1 text-sm text-[var(--muted)]">{item.notes}</p>
                    ) : null}
                  </div>
                  <div className="flex flex-wrap gap-1">
                    {item.url ? (
                      <a
                        href={item.url}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--accent)]"
                      >
                        <ExternalLink className="size-3.5" />
                        Abrir
                      </a>
                    ) : null}
                    <button
                      type="button"
                      onClick={() => setEditing(item)}
                      className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-[var(--surface-muted)]"
                    >
                      <Pencil className="size-3.5" /> Editar
                    </button>
                    <button
                      type="button"
                      onClick={() => removePassword(item)}
                      className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-xs text-[var(--muted)] hover:bg-red-50 hover:text-[var(--danger)]"
                    >
                      <Trash2 className="size-3.5" /> Eliminar
                    </button>
                  </div>
                </div>

                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <div className="rounded-lg bg-[var(--surface-muted)]/70 px-3 py-2">
                    <p className="text-[10px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                      Usuario
                    </p>
                    <div className="mt-1 flex items-center justify-between gap-2">
                      <p className="truncate text-sm text-[var(--ink)]">{item.username}</p>
                      <button
                        type="button"
                        onClick={() => copyText("Usuario", item.username)}
                        className="rounded-md p-1 text-[var(--muted)] hover:bg-[var(--surface)] hover:text-[var(--accent)]"
                        aria-label="Copiar usuario"
                      >
                        <Copy className="size-3.5" />
                      </button>
                    </div>
                  </div>
                  <div className="rounded-lg bg-[var(--surface-muted)]/70 px-3 py-2">
                    <p className="text-[10px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                      Contraseña
                    </p>
                    <div className="mt-1 flex items-center justify-between gap-2">
                      <p className="truncate font-mono text-sm text-[var(--ink)]">
                        {visible ? item.password : "••••••••••••"}
                      </p>
                      <div className="flex items-center gap-1">
                        <button
                          type="button"
                          onClick={() =>
                            setRevealed((current) => ({
                              ...current,
                              [item.id]: !visible,
                            }))
                          }
                          className="rounded-md p-1 text-[var(--muted)] hover:bg-[var(--surface)] hover:text-[var(--accent)]"
                          aria-label={visible ? "Ocultar contraseña" : "Mostrar contraseña"}
                        >
                          {visible ? (
                            <EyeOff className="size-3.5" />
                          ) : (
                            <Eye className="size-3.5" />
                          )}
                        </button>
                        <button
                          type="button"
                          onClick={() => copyText("Contraseña", item.password)}
                          className="rounded-md p-1 text-[var(--muted)] hover:bg-[var(--surface)] hover:text-[var(--accent)]"
                          aria-label="Copiar contraseña"
                        >
                          <Copy className="size-3.5" />
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      ) : null}

      <FormModal
        open={editing != null}
        onOpenChange={(open) => {
          if (!open) setEditing(null);
        }}
        title="Editar contraseña"
        size="xs"
      >
        {editing ? (
          <CredentialForm
            services={services}
            initial={{
              serviceId: editing.serviceId,
              username: editing.username,
              password: editing.password,
              url: editing.url ?? "",
              notes: editing.notes ?? "",
              tags: editing.tags ?? "",
            }}
            onClose={() => setEditing(null)}
            onSubmit={updatePassword}
            submitLabel="Actualizar"
            pending={passwordMutations.update.isPending}
          />
        ) : null}
      </FormModal>

      <FormModal
        open={editingAccount != null}
        onOpenChange={(open) => {
          if (!open) setEditingAccount(null);
        }}
        title="Editar cuenta"
        size="xs"
      >
        {editingAccount ? (
          <AccountForm
            initialName={editingAccount.name}
            initialDescription={editingAccount.description ?? ""}
            onClose={() => setEditingAccount(null)}
            onSubmit={updateAccount}
            submitLabel="Actualizar"
            pending={accountMutations.update.isPending}
          />
        ) : null}
      </FormModal>
    </div>
  );
}
