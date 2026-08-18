import { apiClient } from "@/lib/api/client";
import type { VaultAccount, VaultPassword, VaultService } from "@/types/api";
import type {
  VaultAccountInput,
  VaultPasswordInput,
  VaultServiceInput,
} from "@/schemas/vault.schema";

export function getVaultAccounts() {
  return apiClient.get<VaultAccount[]>("/api/vault/accounts");
}

export function createVaultAccount(body: VaultAccountInput) {
  return apiClient.post<VaultAccount>("/api/vault/accounts", {
    name: body.name,
    description: body.description || null,
  });
}

export function updateVaultAccount(id: number, body: VaultAccountInput) {
  return apiClient.put<VaultAccount>(`/api/vault/accounts/${id}`, {
    name: body.name,
    description: body.description || null,
  });
}

export function deleteVaultAccount(id: number) {
  return apiClient.delete<object>(`/api/vault/accounts/${id}`);
}

export function getVaultServices(onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<VaultService[]>(`/api/vault/services${query}`);
}

export function createVaultService(body: VaultServiceInput) {
  return apiClient.post<VaultService>("/api/vault/services", {
    name: body.name,
    url: body.url || null,
    notes: body.notes || null,
    isActive: body.isActive,
  });
}

export function updateVaultService(id: number, body: VaultServiceInput) {
  return apiClient.put<VaultService>(`/api/vault/services/${id}`, {
    name: body.name,
    url: body.url || null,
    notes: body.notes || null,
    isActive: body.isActive,
  });
}

export function deleteVaultService(id: number) {
  return apiClient.delete<object>(`/api/vault/services/${id}`);
}

export function getVaultPasswords(accountId: number) {
  return apiClient.get<VaultPassword[]>(
    `/api/vault/accounts/${accountId}/passwords`,
  );
}

export function createVaultPassword(accountId: number, body: VaultPasswordInput) {
  return apiClient.post<VaultPassword>(
    `/api/vault/accounts/${accountId}/passwords`,
    {
      serviceId: body.serviceId,
      username: body.username,
      password: body.password,
      url: body.url || null,
      notes: body.notes || null,
      tags: body.tags || null,
    },
  );
}

export function updateVaultPassword(id: number, body: VaultPasswordInput) {
  return apiClient.put<VaultPassword>(`/api/vault/passwords/${id}`, {
    serviceId: body.serviceId,
    username: body.username,
    password: body.password,
    url: body.url || null,
    notes: body.notes || null,
    tags: body.tags || null,
  });
}

export function deleteVaultPassword(id: number) {
  return apiClient.delete<object>(`/api/vault/passwords/${id}`);
}
