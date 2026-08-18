import { env } from "@/config/env";
import type { ApiResponse } from "@/types/api";
import { ApiClientError } from "@/types/api";

type RequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown;
};

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...rest } = options;

  const res = await fetch(`${env.apiUrl}${path}`, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  const json = (await res.json()) as ApiResponse<T>;

  if (!res.ok || !json.success) {
    throw new ApiClientError(
      json.message ?? `Error HTTP ${res.status}`,
      res.status,
      json.errors
    );
  }

  return json.data as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body: unknown) => request<T>(path, { method: "POST", body }),
  put: <T>(path: string, body: unknown) => request<T>(path, { method: "PUT", body }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};