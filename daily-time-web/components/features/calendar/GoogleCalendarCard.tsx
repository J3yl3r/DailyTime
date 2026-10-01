"use client";

import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CalendarSync,
  Check,
  Link2,
  Loader2,
  RefreshCw,
  Unlink,
} from "lucide-react";
import { toast } from "sonner";
import { ApiClientError, type GoogleCalendarStatus } from "@/types/api";
import { getGoogleAuthUrl, type GoogleCalendarSettingsInput } from "@/lib/api/google-calendar";
import {
  useGoogleCalendars,
  useGoogleCalendarStatus,
} from "@/hooks/queries/use-google-calendar";
import { useGoogleCalendarMutations } from "@/hooks/mutations/use-google-calendar-mutations";
import { googleCalendarKeys } from "@/lib/query/keys";
import { cn } from "@/lib/utils/cn";

/** Cuánto se espera a que el usuario termine el consentimiento en la ventana de Google. */
const CONNECT_POLL_MS = 2_000;
const CONNECT_TIMEOUT_MS = 3 * 60_000;

function toInput(status: GoogleCalendarStatus): GoogleCalendarSettingsInput {
  return {
    syncEnabled: status.syncEnabled,
    syncTimedTasks: status.syncTimedTasks,
    syncAllDayTasks: status.syncAllDayTasks,
    syncTimedNotes: status.syncTimedNotes,
    calendarId: status.calendarId,
    timeZoneId: status.timeZoneId,
    pastDays: status.pastDays,
    futureDays: status.futureDays,
  };
}

function formatMoment(iso: string | null) {
  if (!iso) return "nunca";
  const date = new Date(iso);
  const today = new Date();
  const time = date.toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" });
  if (date.toDateString() === today.toDateString()) return `hoy ${time}`;
  return `${date.toLocaleDateString("es-CO", { day: "numeric", month: "short" })} ${time}`;
}

export function GoogleCalendarCard() {
  const queryClient = useQueryClient();
  const { data: status, isLoading, error } = useGoogleCalendarStatus();
  const { disconnect, updateSettings, syncNow } = useGoogleCalendarMutations();
  const { data: calendars } = useGoogleCalendars(Boolean(status?.connected));
  const [connecting, setConnecting] = useState(false);
  const pollRef = useRef<number | null>(null);

  const connected = Boolean(status?.connected);

  // Deja de esperar en cuanto la cuenta aparece conectada.
  useEffect(() => {
    if (connected && pollRef.current !== null) {
      window.clearInterval(pollRef.current);
      pollRef.current = null;
      setConnecting(false);
      toast.success("Cuenta de Google conectada");
    }
  }, [connected]);

  useEffect(
    () => () => {
      if (pollRef.current !== null) window.clearInterval(pollRef.current);
    },
    [],
  );

  const startConnect = async () => {
    try {
      setConnecting(true);
      const { authUrl } = await getGoogleAuthUrl();
      const popup = window.open(authUrl, "dailytime-google", "width=520,height=680");
      if (!popup) {
        setConnecting(false);
        toast.error("El navegador bloqueó la ventana de Google", {
          description: "Permite las ventanas emergentes para este sitio y vuelve a intentarlo.",
        });
        return;
      }

      const startedAt = Date.now();
      pollRef.current = window.setInterval(() => {
        if (Date.now() - startedAt > CONNECT_TIMEOUT_MS) {
          if (pollRef.current !== null) window.clearInterval(pollRef.current);
          pollRef.current = null;
          setConnecting(false);
          return;
        }
        queryClient.invalidateQueries({ queryKey: googleCalendarKeys.status() });
      }, CONNECT_POLL_MS);
    } catch (error) {
      setConnecting(false);
      toast.error(error instanceof Error ? error.message : "No se pudo iniciar la conexión");
    }
  };

  const save = (patch: Partial<GoogleCalendarSettingsInput>) => {
    if (!status) return;
    updateSettings.mutate(
      { ...toInput(status), ...patch },
      {
        onSuccess: () => toast.success("Ajustes guardados"),
        onError: (error) => toast.error(error.message),
      },
    );
  };

  const runSync = () =>
    syncNow.mutate(undefined, {
      onSuccess: (result) => {
        if (result.message) {
          toast.error(result.message);
          return;
        }
        toast.success("Sincronizado con Google", {
          description:
            `${result.pushed} enviados · ${result.pulled} traídos` +
            (result.deleted ? ` · ${result.deleted} borrados` : ""),
        });
      },
      onError: (error) => toast.error(error.message),
    });

  if (isLoading) {
    return (
      <p className="inline-flex items-center gap-2 text-sm text-[var(--muted)]">
        <Loader2 className="size-4 animate-spin" />
        Consultando el estado de Google…
      </p>
    );
  }

  if (!status) {
    return (
      <p className="flex items-start gap-2 rounded-lg border border-[var(--border)] bg-[var(--surface-muted)] p-3 text-sm text-[var(--muted)]">
        <AlertTriangle className="mt-0.5 size-4 shrink-0" />
        {/* Un error del cliente HTTP (red, respuesta sin JSON) no dice nada útil aquí. */}
        {error instanceof ApiClientError
          ? error.message
          : "No se pudo consultar el estado de Google. Revisa que la API esté corriendo y actualizada."}
      </p>
    );
  }

  if (!status.configured) {
    return (
      <div className="flex flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface-muted)] p-4">
        <p className="inline-flex items-center gap-2 text-sm font-medium text-[var(--ink)]">
          <AlertTriangle className="size-4" />
          Faltan las credenciales de Google
        </p>
        <p className="text-xs leading-relaxed text-[var(--muted)]">
          Crea un cliente OAuth de tipo <strong>Aplicación web</strong> en Google Cloud, con esta
          URI de redirección autorizada:
        </p>
        <code className="rounded-md bg-[var(--surface)] px-3 py-2 text-xs break-all text-[var(--ink)]">
          {status.redirectUri}
        </code>
        <p className="text-xs leading-relaxed text-[var(--muted)]">
          Luego guarda el id y el secreto en user secrets de la API (no en appsettings):
        </p>
        <code className="rounded-md bg-[var(--surface)] px-3 py-2 text-xs break-all text-[var(--ink)]">
          dotnet user-secrets set &quot;Google:ClientId&quot; &quot;…&quot; --project dailyTimeApi
        </code>
      </div>
    );
  }

  if (!status.connected) {
    return (
      <div className="flex flex-col gap-3">
        <p className="text-sm text-[var(--muted)]">
          Conecta tu cuenta para que tus tareas y notas con hora aparezcan en Google Calendar, y
          los eventos que crees allí entren aquí como tareas.
        </p>
        <button
          type="button"
          onClick={startConnect}
          disabled={connecting}
          className="inline-flex w-fit items-center gap-2 rounded-md border border-[var(--accent)] bg-[var(--accent-soft)] px-3 py-2 text-sm font-medium text-[var(--ink)] hover:brightness-105 disabled:opacity-60"
        >
          {connecting ? <Loader2 className="size-4 animate-spin" /> : <Link2 className="size-4" />}
          {connecting ? "Esperando a Google…" : "Conectar con Google"}
        </button>
        {connecting ? (
          <p className="text-xs text-[var(--muted)]">
            Termina el consentimiento en la ventana de Google. Esta tarjeta se actualiza sola.
          </p>
        ) : null}
      </div>
    );
  }

  const busy = updateSettings.isPending || syncNow.isPending || disconnect.isPending;
  const failed = status.lastSyncStatus === "error";

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="inline-flex items-center gap-2 text-sm font-medium text-[var(--ink)]">
            <Check className="size-4 text-[var(--success,#22C55E)]" />
            {status.email}
          </p>
          <p className="mt-1 text-xs text-[var(--muted)]">
            {status.isRunning || status.lastSyncStatus === "running" ? (
              <span className="inline-flex items-center gap-1">
                <Loader2 className="size-3 animate-spin" />
                sincronizando…
              </span>
            ) : (
              <>
                Última sincronización: {formatMoment(status.lastSyncAt)} ·{" "}
                {status.lastPushedCount} enviados · {status.lastPulledCount} traídos · se repite
                cada {status.syncIntervalMinutes} min
              </>
            )}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={runSync}
            disabled={busy}
            className="inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-2 text-sm hover:bg-[var(--surface-muted)] disabled:opacity-60"
          >
            <RefreshCw className={cn("size-4", syncNow.isPending && "animate-spin")} />
            Sincronizar ahora
          </button>
          <button
            type="button"
            onClick={() => {
              if (!window.confirm("¿Desconectar la cuenta de Google? Los eventos ya creados se quedan en tu calendario, pero dejarán de sincronizarse."))
                return;
              disconnect.mutate(undefined, {
                onSuccess: () => toast.success("Cuenta desconectada"),
                onError: (error) => toast.error(error.message),
              });
            }}
            disabled={busy}
            className="inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-2 text-sm text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--danger)] disabled:opacity-60"
          >
            <Unlink className="size-4" />
            Desconectar
          </button>
        </div>
      </div>

      {failed && status.lastSyncMessage ? (
        <p className="flex items-start gap-2 rounded-lg border border-[var(--danger)]/30 bg-red-50 p-3 text-xs text-[var(--danger)]">
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          {status.lastSyncMessage}
        </p>
      ) : null}

      <label
        className={cn(
          "flex cursor-pointer items-center justify-between gap-3 rounded-lg border px-3 py-2 text-sm",
          status.syncEnabled
            ? "border-[var(--accent)] bg-[var(--accent-soft)] text-[var(--ink)]"
            : "border-[var(--border)] text-[var(--muted)]",
        )}
      >
        <span className="inline-flex items-center gap-2">
          <CalendarSync className="size-4" />
          {status.syncEnabled ? "Sincronización activa" : "Sincronización en pausa"}
        </span>
        <input
          type="checkbox"
          className="size-4 accent-[var(--accent)]"
          checked={status.syncEnabled}
          disabled={busy}
          onChange={(e) => save({ syncEnabled: e.target.checked })}
        />
      </label>

      <div className="flex flex-col gap-2">
        <span className="text-xs font-medium text-[var(--muted)]">Qué se envía a Google</span>
        {(
          [
            { key: "syncTimedTasks", label: "Tareas con hora" },
            { key: "syncAllDayTasks", label: "Tareas sin hora (como evento de día completo)" },
            { key: "syncTimedNotes", label: "Notas con hora" },
          ] as const
        ).map((option) => (
          <label
            key={option.key}
            className="flex cursor-pointer items-center justify-between gap-3 rounded-lg border border-[var(--border)] px-3 py-2 text-sm text-[var(--ink)]"
          >
            {option.label}
            <input
              type="checkbox"
              className="size-4 accent-[var(--accent)]"
              checked={status[option.key]}
              disabled={busy}
              onChange={(e) => save({ [option.key]: e.target.checked })}
            />
          </label>
        ))}
        <p className="text-xs text-[var(--muted)]">
          Al desmarcar una opción, lo ya sincronizado se queda en Google tal como está; solo deja
          de actualizarse.
        </p>
      </div>

      <div className="flex flex-col gap-2">
        <span className="text-xs font-medium text-[var(--muted)]">Calendario destino</span>
        <select
          value={status.calendarId ?? "primary"}
          disabled={busy || !calendars?.length}
          onChange={(e) => {
            if (
              !window.confirm(
                "Cambiar de calendario vuelve a crear los eventos en el nuevo. Los del calendario anterior se quedan allí. ¿Continuar?",
              )
            )
              return;
            save({ calendarId: e.target.value });
          }}
          className="rounded-md border border-[var(--border)] bg-white px-2 py-2 text-sm outline-none focus:border-[var(--accent)]"
        >
          {calendars?.length ? (
            calendars.map((calendar) => (
              <option key={calendar.id} value={calendar.id}>
                {calendar.name}
                {calendar.isPrimary ? " (principal)" : ""}
              </option>
            ))
          ) : (
            <option value={status.calendarId ?? "primary"}>
              {status.calendarId ?? "primary"}
            </option>
          )}
        </select>
      </div>

      <div className="flex flex-wrap gap-3">
        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Días hacia atrás
          <input
            type="number"
            min={0}
            max={1825}
            defaultValue={status.pastDays}
            disabled={busy}
            onBlur={(e) => {
              const value = Number(e.target.value);
              if (Number.isFinite(value) && value !== status.pastDays) save({ pastDays: value });
            }}
            className="w-28 rounded-md border border-[var(--border)] bg-white px-2 py-1.5 text-sm text-[var(--ink)] outline-none focus:border-[var(--accent)]"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Días hacia adelante
          <input
            type="number"
            min={0}
            max={1825}
            defaultValue={status.futureDays}
            disabled={busy}
            onBlur={(e) => {
              const value = Number(e.target.value);
              if (Number.isFinite(value) && value !== status.futureDays)
                save({ futureDays: value });
            }}
            className="w-28 rounded-md border border-[var(--border)] bg-white px-2 py-1.5 text-sm text-[var(--ink)] outline-none focus:border-[var(--accent)]"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium text-[var(--muted)]">
          Zona horaria
          <input
            type="text"
            defaultValue={status.timeZoneId}
            disabled={busy}
            onBlur={(e) => {
              const value = e.target.value.trim();
              if (value && value !== status.timeZoneId) save({ timeZoneId: value });
            }}
            className="w-48 rounded-md border border-[var(--border)] bg-white px-2 py-1.5 text-sm text-[var(--ink)] outline-none focus:border-[var(--accent)]"
          />
        </label>
      </div>

      <p className="text-xs leading-relaxed text-[var(--muted)]">
        Si editas lo mismo en los dos lados entre dos pasadas, gana el cambio más reciente. Los
        eventos que crees en Google entran aquí como tareas con el estado y la categoría por
        defecto.
      </p>
    </div>
  );
}
