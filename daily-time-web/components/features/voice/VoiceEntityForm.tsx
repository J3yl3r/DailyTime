"use client";

import { useEffect, useRef } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { VoiceFormFieldPatch, VoiceFormKind } from "@/lib/voice/form-commands";
import { usePersonMutations } from "@/hooks/mutations/use-person-mutations";
import { useProjectMutations } from "@/hooks/mutations/use-project-mutations";
import { useStatusMutations } from "@/hooks/mutations/use-status-mutations";
import { useCategoryMutations } from "@/hooks/mutations/use-category-mutations";
import { useVaultAccountMutations } from "@/hooks/mutations/use-vault-account-mutations";
import { useVaultPasswordMutations } from "@/hooks/mutations/use-vault-password-mutations";
import { useVaultServiceMutations } from "@/hooks/mutations/use-vault-service-mutations";
import { useVaultServices } from "@/hooks/queries/use-vault-services";
import { useCareerCatalogMutations } from "@/hooks/mutations/use-career-catalog-mutations";
import type { CareerCatalogKind } from "@/types/api";

type EntityValues = {
  name: string;
  description: string;
  color: string;
  isFinal: boolean;
  isActive: boolean;
  itemType: "task" | "note";
  serviceName: string;
  username: string;
  password: string;
  url: string;
  notes: string;
  tags: string;
};

type Props = {
  kind: Exclude<VoiceFormKind, "task" | "note" | "work_experience" | "job_application">;
  initialValues?: Partial<EntityValues>;
  accountId?: number | null;
  catalogKind?: CareerCatalogKind | null;
  editingId?: number | null;
  voicePatch?: VoiceFormFieldPatch | null;
  submitRequestId?: number;
  onClose: () => void;
};

const inputClass =
  "rounded-md border border-[var(--border)] bg-white px-3 py-2 text-sm font-normal outline-none focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]";

export function VoiceEntityForm({
  kind,
  initialValues,
  accountId = null,
  catalogKind = null,
  editingId = null,
  voicePatch = null,
  submitRequestId = 0,
  onClose,
}: Props) {
  const personMutations = usePersonMutations();
  const projectMutations = useProjectMutations();
  const statusMutations = useStatusMutations();
  const categoryMutations = useCategoryMutations();
  const vaultAccountMutations = useVaultAccountMutations();
  const vaultPasswordMutations = useVaultPasswordMutations(accountId);
  const vaultServiceMutations = useVaultServiceMutations();
  const vaultServicesQuery = useVaultServices();
  const vaultServices = vaultServicesQuery.data ?? [];
  const careerCatalogMutations = useCareerCatalogMutations(
    catalogKind ?? "companies",
  );

  const form = useForm<EntityValues>({
    defaultValues: {
      name: initialValues?.name ?? "",
      description: initialValues?.description ?? "",
      color: initialValues?.color ?? "#64748B",
      isFinal: initialValues?.isFinal ?? false,
      isActive: initialValues?.isActive ?? true,
      itemType: initialValues?.itemType ?? "task",
      serviceName: initialValues?.serviceName ?? "",
      username: initialValues?.username ?? "",
      password: initialValues?.password ?? "",
      url: initialValues?.url ?? "",
      notes: initialValues?.notes ?? "",
      tags: initialValues?.tags ?? "",
    },
  });

  useEffect(() => {
    if (!voicePatch) return;
    form.setValue(voicePatch.field as keyof EntityValues, voicePatch.value as never, {
      shouldDirty: true,
      shouldValidate: true,
      shouldTouch: true,
    });
  }, [form, voicePatch]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (kind === "person") {
        const body = {
          name: values.name.trim(),
          description: values.description.trim(),
          isActive: values.isActive,
        };
        if (editingId != null) {
          await personMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Persona actualizada");
        } else {
          await personMutations.create.mutateAsync(body);
          toast.success("Persona creada");
        }
      } else if (kind === "project") {
        const body = {
          name: values.name.trim(),
          description: values.description.trim(),
          isActive: values.isActive,
        };
        if (editingId != null) {
          await projectMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Proyecto actualizado");
        } else {
          await projectMutations.create.mutateAsync(body);
          toast.success("Proyecto creado");
        }
      } else if (kind === "status") {
        const body = {
          name: values.name.trim(),
          description: values.description.trim() || values.name.trim(),
          color: values.color,
          isFinal: values.isFinal,
          itemType: values.itemType,
        };
        if (editingId != null) {
          await statusMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Estado actualizado");
        } else {
          await statusMutations.create.mutateAsync(body);
          toast.success("Estado creado");
        }
      } else if (kind === "category") {
        const body = {
          name: values.name.trim(),
          description: values.description.trim() || values.name.trim(),
          itemType: values.itemType,
          isActive: values.isActive,
        };
        if (editingId != null) {
          await categoryMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Categoría actualizada");
        } else {
          await categoryMutations.create.mutateAsync(body);
          toast.success("Categoría creada");
        }
      } else if (kind === "vault_account") {
        const body = {
          name: values.name.trim(),
          description: values.description.trim(),
        };
        if (editingId != null) {
          await vaultAccountMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Cuenta de bóveda actualizada");
        } else {
          await vaultAccountMutations.create.mutateAsync(body);
          toast.success("Cuenta de bóveda creada");
        }
      } else if (kind === "vault_password") {
        if (accountId == null) {
          toast.error("Primero crea una cuenta de bóveda");
          return;
        }
        const serviceName = values.serviceName.trim();
        if (!serviceName) {
          toast.error("El servicio es obligatorio");
          return;
        }
        let service =
          vaultServices.find(
            (item) => item.name.toLowerCase() === serviceName.toLowerCase(),
          ) ?? null;
        if (!service) {
          service = await vaultServiceMutations.create.mutateAsync({
            name: serviceName,
            url: values.url.trim(),
            notes: "",
            isActive: true,
          });
        }
        const body = {
          serviceId: service.id,
          username: values.username.trim(),
          password: values.password,
          url: values.url.trim(),
          notes: values.notes.trim(),
          tags: values.tags.trim(),
        };
        if (editingId != null) {
          await vaultPasswordMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Contraseña actualizada");
        } else {
          await vaultPasswordMutations.create.mutateAsync(body);
          toast.success("Contraseña guardada");
        }
      } else if (kind === "vault_service") {
        const body = {
          name: values.name.trim(),
          url: values.url.trim(),
          notes: values.notes.trim(),
          isActive: values.isActive,
        };
        if (!body.name) {
          toast.error("El nombre es obligatorio");
          return;
        }
        if (editingId != null) {
          await vaultServiceMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Servicio actualizado");
        } else {
          await vaultServiceMutations.create.mutateAsync(body);
          toast.success("Servicio creado");
        }
      } else if (kind === "career_catalog") {
        if (!catalogKind) {
          toast.error("Tipo de catálogo no indicado");
          return;
        }
        const body = {
          name: values.name.trim(),
          description: values.description.trim(),
          color: values.color.trim() || "#64748B",
          sortOrder: 0,
          isActive: values.isActive,
        };
        if (!body.name) {
          toast.error("El nombre es obligatorio");
          return;
        }
        if (editingId != null) {
          await careerCatalogMutations.update.mutateAsync({ id: editingId, body });
          toast.success("Dato reutilizable actualizado");
        } else {
          await careerCatalogMutations.create.mutateAsync(body);
          toast.success("Dato reutilizable creado");
        }
      }
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Error al guardar");
    }
  });

  const submitRef = useRef(onSubmit);
  submitRef.current = onSubmit;

  useEffect(() => {
    if (!submitRequestId) return;
    void submitRef.current();
  }, [submitRequestId]);

  const pending =
    personMutations.create.isPending ||
    personMutations.update.isPending ||
    projectMutations.create.isPending ||
    projectMutations.update.isPending ||
    statusMutations.create.isPending ||
    statusMutations.update.isPending ||
    categoryMutations.create.isPending ||
    categoryMutations.update.isPending ||
    vaultAccountMutations.create.isPending ||
    vaultAccountMutations.update.isPending ||
    vaultPasswordMutations.create.isPending ||
    vaultPasswordMutations.update.isPending ||
    vaultServiceMutations.create.isPending ||
    vaultServiceMutations.update.isPending ||
    careerCatalogMutations.create.isPending ||
    careerCatalogMutations.update.isPending;

  const showType = kind === "status" || kind === "category";
  const showColor = kind === "status" || kind === "career_catalog";
  const showFinal = kind === "status";
  const showActive =
    kind === "person" ||
    kind === "project" ||
    kind === "category" ||
    kind === "career_catalog" ||
    kind === "vault_service";
  const showVaultPassword = kind === "vault_password";
  const showVaultService = kind === "vault_service";
  const showName = !showVaultPassword;
  const showDescription =
    kind !== "vault_password" && kind !== "vault_service";
  const showUrlNotes = showVaultService;

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3">
      {showType ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
          Tipo
          <select {...form.register("itemType")} className={inputClass}>
            <option value="task">Tarea</option>
            <option value="note">Nota</option>
          </select>
        </label>
      ) : null}

      {showName ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
          Nombre
          <Controller
            name="name"
            control={form.control}
            render={({ field }) => (
              <input
                {...field}
                value={field.value ?? ""}
                placeholder="Nombre"
                className={inputClass}
                autoFocus
              />
            )}
          />
        </label>
      ) : null}

      {showVaultPassword ? (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Servicio
            <Controller
              name="serviceName"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  value={field.value ?? ""}
                  placeholder="Gmail laboral"
                  className={inputClass}
                  autoFocus
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Usuario
            <Controller
              name="username"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Contraseña
            <Controller
              name="password"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            URL
            <Controller
              name="url"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Etiquetas
            <Controller
              name="tags"
              control={form.control}
              render={({ field }) => (
                <input
                  {...field}
                  value={field.value ?? ""}
                  placeholder="trabajo, email"
                  className={inputClass}
                />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Notas
            <Controller
              name="notes"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  rows={2}
                  className={inputClass}
                />
              )}
            />
          </label>
        </>
      ) : null}

      {showDescription ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
          Descripción
          <Controller
            name="description"
            control={form.control}
            render={({ field }) => (
              <textarea
                {...field}
                value={field.value ?? ""}
                rows={3}
                className={inputClass}
              />
            )}
          />
        </label>
      ) : null}

      {showUrlNotes ? (
        <>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            URL
            <Controller
              name="url"
              control={form.control}
              render={({ field }) => (
                <input {...field} value={field.value ?? ""} className={inputClass} />
              )}
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
            Notas
            <Controller
              name="notes"
              control={form.control}
              render={({ field }) => (
                <textarea
                  {...field}
                  value={field.value ?? ""}
                  rows={2}
                  className={inputClass}
                />
              )}
            />
          </label>
        </>
      ) : null}

      {showColor ? (
        <label className="flex flex-col gap-1.5 text-sm font-medium text-[var(--ink)]">
          Color
          <div className="flex items-center gap-2">
            <Controller
              name="color"
              control={form.control}
              render={({ field }) => (
                <>
                  <input
                    type="color"
                    value={field.value}
                    onChange={(event) =>
                      field.onChange(event.target.value.toUpperCase())
                    }
                    className="h-10 w-14 cursor-pointer rounded-md border border-[var(--border)] bg-white p-1"
                  />
                  <input
                    value={field.value}
                    onChange={(event) =>
                      field.onChange(event.target.value.toUpperCase())
                    }
                    className={`${inputClass} flex-1 uppercase`}
                  />
                </>
              )}
            />
          </div>
        </label>
      ) : null}

      {showFinal ? (
        <label className="inline-flex items-center gap-2 text-sm">
          <input type="checkbox" {...form.register("isFinal")} className="size-4 accent-[var(--accent)]" />
          Estado final
        </label>
      ) : null}

      {showActive ? (
        <label className="inline-flex items-center gap-2 text-sm">
          <input type="checkbox" {...form.register("isActive")} className="size-4 accent-[var(--accent)]" />
          Activo
        </label>
      ) : null}

      <div className="flex gap-2 border-t border-[var(--border)] pt-3">
        <button
          type="submit"
          disabled={pending}
          className="rounded-md bg-[var(--accent)] px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
        >
          {pending ? "Guardando…" : "Guardar"}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md border border-[var(--border)] px-3 py-1.5 text-sm text-[var(--muted)]"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}
