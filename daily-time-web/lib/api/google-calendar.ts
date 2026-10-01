import { apiClient } from "@/lib/api/client";
import type {
  GoogleCalendarListItem,
  GoogleCalendarStatus,
  GoogleSyncRun,
} from "@/types/api";

export type GoogleCalendarSettingsInput = {
  syncEnabled: boolean;
  syncTimedTasks: boolean;
  syncAllDayTasks: boolean;
  syncTimedNotes: boolean;
  calendarId?: string | null;
  timeZoneId?: string | null;
  pastDays: number;
  futureDays: number;
};

export function getGoogleCalendarStatus() {
  return apiClient.get<GoogleCalendarStatus>("/api/google-calendar");
}

/** URL de consentimiento; la web la abre en una ventana aparte. */
export function getGoogleAuthUrl() {
  return apiClient.get<{ authUrl: string }>("/api/google-calendar/auth-url");
}

export function disconnectGoogleCalendar() {
  return apiClient.post<GoogleCalendarStatus>("/api/google-calendar/disconnect", {});
}

export function updateGoogleCalendarSettings(body: GoogleCalendarSettingsInput) {
  return apiClient.put<GoogleCalendarStatus>("/api/google-calendar/settings", body);
}

export function syncGoogleCalendarNow() {
  return apiClient.post<GoogleSyncRun>("/api/google-calendar/sync", {});
}

export function getGoogleCalendars() {
  return apiClient.get<GoogleCalendarListItem[]>("/api/google-calendar/calendars");
}
