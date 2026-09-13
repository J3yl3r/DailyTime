"use client";

import { useQuery } from "@tanstack/react-query";
import { getScrapeSchedule } from "@/lib/api/scrape-schedule";
import { scrapeScheduleKeys } from "@/lib/query/keys";

export function useScrapeSchedule() {
  return useQuery({
    queryKey: scrapeScheduleKeys.current(),
    queryFn: getScrapeSchedule,
    // Sin sondeo constante: mientras hay captura programada en curso se refresca cada 15 s;
    // si no, una sola vez poco después de la próxima hora para mostrar que empezó.
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data) return false;
      if (data.lastSlotStatus === "running") return 15_000;
      if (!data.nextSlotAt) return false;
      const untilNext = new Date(data.nextSlotAt).getTime() - Date.now() + 5_000;
      return Math.min(Math.max(untilNext, 15_000), 2_000_000_000);
    },
  });
}
