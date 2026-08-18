export type VaultCredentialDraft = {
  serviceId: number | "";
  username: string;
  password: string;
  url: string;
  notes: string;
  tags: string;
};

export function parseVaultTags(value: string | null | undefined): string[] {
  if (!value?.trim()) return [];
  return value
    .split(",")
    .map((tag) => tag.trim().toLowerCase())
    .filter(Boolean);
}
