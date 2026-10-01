"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  disconnectGoogleCalendar,
  syncGoogleCalendarNow,
  updateGoogleCalendarSettings,
  type GoogleCalendarSettingsInput,
} from "@/lib/api/google-calendar";
import { googleCalendarKeys, noteKeys, taskItemKeys } from "@/lib/query/keys";
import type { GoogleCalendarStatus } from "@/types/api";

export function useGoogleCalendarMutations() {
  const queryClient = useQueryClient();

  const setStatus = (status: GoogleCalendarStatus) =>
    queryClient.setQueryData(googleCalendarKeys.status(), status);

  /** Una pasada puede crear, cambiar o borrar tareas y notas: el calendario se vuelve a leer entero. */
  const invalidateItems = () => {
    queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
    queryClient.invalidateQueries({ queryKey: noteKeys.all });
  };

  const disconnect = useMutation({
    mutationFn: disconnectGoogleCalendar,
    onSuccess: (status) => {
      setStatus(status);
      queryClient.removeQueries({ queryKey: googleCalendarKeys.calendars() });
      invalidateItems();
    },
  });

  const updateSettings = useMutation({
    mutationFn: (body: GoogleCalendarSettingsInput) => updateGoogleCalendarSettings(body),
    onSuccess: (status) => {
      setStatus(status);
      queryClient.invalidateQueries({ queryKey: googleCalendarKeys.calendars() });
    },
  });

  const syncNow = useMutation({
    mutationFn: syncGoogleCalendarNow,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: googleCalendarKeys.status() });
      invalidateItems();
    },
  });

  return { disconnect, updateSettings, syncNow };
}
