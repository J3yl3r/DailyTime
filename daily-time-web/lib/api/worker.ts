import { env } from "@/config/env";

export type WorkerScrapeResult = {
  portalId: number;
  portalName: string;
  status: string;
  message: string;
  pageTitle?: string | null;
  offers: unknown[];
  savedInserted?: number;
  savedUpdated?: number;
};

async function workerRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${env.workerApiUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!res.ok) {
    let message = `Error HTTP ${res.status}`;
    try {
      const json = (await res.json()) as { message?: string };
      if (json.message) message = json.message;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }

  return (await res.json()) as T;
}

/** Dispara scrape manual de un portal en el worker y persiste ofertas. */
export function scrapePortalNow(portalId: number) {
  return workerRequest<WorkerScrapeResult>(`/api/jobs/scrape/${portalId}`, {
    method: "POST",
  });
}
