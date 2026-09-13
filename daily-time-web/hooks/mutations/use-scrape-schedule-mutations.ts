"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { updateScrapeSchedule } from "@/lib/api/scrape-schedule";
import { reloadWorkerSchedule } from "@/lib/api/worker";
import { scrapeScheduleKeys } from "@/lib/query/keys";
import type { ScrapeScheduleInput } from "@/schemas/scrape-schedule.schema";

export function useScrapeScheduleMutations() {
  const queryClient = useQueryClient();

  const update = useMutation({
    mutationFn: async (body: ScrapeScheduleInput) => {
      const schedule = await updateScrapeSchedule(body);
      // Si el worker está apagado, leerá el horario al arrancar: no es un error.
      const workerReloaded = await reloadWorkerSchedule().then(
        () => true,
        () => false,
      );
      return { schedule, workerReloaded };
    },
    onSuccess: ({ schedule }) => {
      queryClient.setQueryData(scrapeScheduleKeys.current(), schedule);
    },
  });

  return { update };
}
