"use client";

import { useQuery } from "@tanstack/react-query";
import {
  getGoogleCalendarStatus,
  getGoogleCalendars,
} from "@/lib/api/google-calendar";
import { googleCalendarKeys } from "@/lib/query/keys";

export function useGoogleCalendarStatus(enabled = true) {
  return useQuery({
    queryKey: googleCalendarKeys.status(),
    queryFn: getGoogleCalendarStatus,
    enabled,
    // Mientras hay una pasada en curso se refresca seguido; si no, basta con mirar de
    // vez en cuando por si el ciclo automático trajo algo.
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data?.connected) return false;
      if (data.isRunning || data.lastSyncStatus === "running") return 3_000;
      return Math.max(data.syncIntervalMinutes, 1) * 60_000;
    },
  });
}

export function useGoogleCalendars(enabled: boolean) {
  return useQuery({
    queryKey: googleCalendarKeys.calendars(),
    queryFn: getGoogleCalendars,
    enabled,
    staleTime: 5 * 60_000,
  });
}
