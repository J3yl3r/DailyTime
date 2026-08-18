import { z } from "zod";

export const vaultAccountSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(100),
  description: z.string().trim().max(300).optional().or(z.literal("")),
});

export type VaultAccountFormValues = z.input<typeof vaultAccountSchema>;
export type VaultAccountInput = z.output<typeof vaultAccountSchema>;

export const vaultServiceSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio").max(150),
  url: z.string().trim().max(500).optional().or(z.literal("")),
  notes: z.string().trim().max(500).optional().or(z.literal("")),
  isActive: z.boolean().default(true),
});

export type VaultServiceFormValues = z.input<typeof vaultServiceSchema>;
export type VaultServiceInput = z.output<typeof vaultServiceSchema>;

export const vaultPasswordSchema = z.object({
  serviceId: z.coerce.number().int().positive("Selecciona un servicio"),
  username: z.string().trim().min(1, "El usuario es obligatorio").max(200),
  password: z.string().min(1, "La contraseña es obligatoria").max(500),
  url: z.string().trim().max(500).optional().or(z.literal("")),
  notes: z.string().trim().max(1000).optional().or(z.literal("")),
  tags: z.string().trim().max(500).optional().or(z.literal("")),
});

export type VaultPasswordFormValues = z.input<typeof vaultPasswordSchema>;
export type VaultPasswordInput = z.output<typeof vaultPasswordSchema>;
