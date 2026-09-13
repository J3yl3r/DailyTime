import { apiClient } from "@/lib/api/client";
import type { ScrapeSchedule } from "@/types/api";
import type { ScrapeScheduleInput } from "@/schemas/scrape-schedule.schema";

export function getScrapeSchedule() {
  return apiClient.get<ScrapeSchedule>("/api/scrape-schedule");
}

export function updateScrapeSchedule(body: ScrapeScheduleInput) {
  return apiClient.put<ScrapeSchedule>("/api/scrape-schedule", {
    enabled: body.enabled,
    times: body.times,
    days: body.days,
  });
}
