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

export class WorkerRequestAbortedError extends Error {
  constructor() {
    super("Captura detenida.");
    this.name = "WorkerRequestAbortedError";
  }
}

async function workerRequest<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${env.workerApiUrl}${path}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
    });
  } catch (error) {
    if (
      (error instanceof DOMException && error.name === "AbortError") ||
      (error instanceof Error && error.name === "AbortError")
    ) {
      throw new WorkerRequestAbortedError();
    }
    throw error;
  }

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
export function scrapePortalNow(portalId: number, signal?: AbortSignal) {
  return workerRequest<WorkerScrapeResult>(`/api/jobs/scrape/${portalId}`, {
    method: "POST",
    signal,
  });
}

export function stopScrapes() {
  return workerRequest<{ cancelled: boolean; message: string }>("/api/jobs/scrape/stop", {
    method: "POST",
  });
}

export function openChromeDebug() {
  return workerRequest<{ started: boolean; message: string }>("/api/chrome/debug", {
    method: "POST",
  });
}
