"use client";

import { useEffect, useRef, useState } from "react";
import { usePathname } from "next/navigation";
import { Bell, Menu } from "lucide-react";
import { useSidebar } from "@/providers/sidebar-provider";
import { useAppNotifications } from "@/providers/app-notifications-provider";
import { cn } from "@/lib/utils/cn";

const TITLES: Record<string, string> = {
  "/workspace": "Informe de tiempo",
  "/board": "Tablero",
  "/calendar": "Calendario",
  "/vault": "Credenciales",
  "/vault/services": "Servicios de bóveda",
  "/career/profile": "Perfil de postulación",
  "/experiences": "Experiencias",
  "/applications": "Postulaciones",
  "/career/portals": "Portales de ofertas",
  "/career/offers": "Ofertas capturadas",
  "/career/catalogs": "Datos reutilizables",
  "/statuses": "Estados",
  "/categories": "Categorías",
  "/people": "Personas",
  "/projects": "Proyectos",
  "/companies": "Empresas",
};

function titleForPath(pathname: string) {
  return TITLES[pathname] ?? "DailyTime";
}

export function Topbar() {
  const pathname = usePathname();
  const { open, toggle } = useSidebar();
  const { items, unreadCount, markAllRead, clear } = useAppNotifications();
  const [panelOpen, setPanelOpen] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!panelOpen) return;
    const onPointer = (event: MouseEvent) => {
      if (!panelRef.current?.contains(event.target as Node)) {
        setPanelOpen(false);
      }
    };
    document.addEventListener("mousedown", onPointer);
    return () => document.removeEventListener("mousedown", onPointer);
  }, [panelOpen]);

  return (
    <header className="sticky top-0 z-20 flex flex-wrap items-center gap-3 border-b border-[var(--border)] bg-[var(--surface)]/90 px-4 py-3 backdrop-blur-md">
      <div className="flex min-w-0 flex-1 items-center gap-3">
        <button
          type="button"
          onClick={toggle}
          className="rounded-md p-2 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
          aria-label={open ? "Ocultar menú" : "Mostrar menú"}
          title={open ? "Ocultar menú" : "Mostrar menú"}
        >
          <Menu className="size-5" />
        </button>
        <h1 className="truncate font-[family-name:var(--font-fraunces)] text-lg font-semibold text-[var(--ink)] md:text-xl">
          {titleForPath(pathname)}
        </h1>
      </div>

      <div className="relative" ref={panelRef}>
        <button
          type="button"
          onClick={() => {
            setPanelOpen((v) => !v);
            markAllRead();
          }}
          className="relative rounded-md p-2 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
          aria-label="Notificaciones"
          title="Notificaciones"
        >
          <Bell className="size-5" />
          {unreadCount > 0 ? (
            <span className="absolute right-1 top-1 flex size-4 items-center justify-center rounded-full bg-[var(--danger)] text-[10px] font-semibold text-white">
              {unreadCount > 9 ? "9+" : unreadCount}
            </span>
          ) : null}
        </button>

        {panelOpen ? (
          <div className="absolute right-0 top-full z-30 mt-2 w-[min(22rem,calc(100vw-2rem))] overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-xl">
            <div className="flex items-center justify-between border-b border-[var(--border)] px-3 py-2">
              <p className="text-sm font-medium text-[var(--ink)]">Notificaciones</p>
              <button
                type="button"
                onClick={clear}
                className="text-xs text-[var(--muted)] hover:text-[var(--ink)]"
              >
                Limpiar
              </button>
            </div>
            <ul className="max-h-80 overflow-y-auto">
              {!items.length ? (
                <li className="px-3 py-6 text-center text-sm text-[var(--muted)]">
                  Sin notificaciones aún.
                </li>
              ) : (
                items.map((item) => (
                  <li
                    key={item.id}
                    className={cn(
                      "border-b border-[var(--border)] px-3 py-2 last:border-b-0",
                      !item.read && "bg-[var(--surface-muted)]/50",
                    )}
                  >
                    <p
                      className={cn(
                        "text-sm font-medium",
                        item.tone === "error" && "text-[var(--danger)]",
                        item.tone === "success" && "text-[var(--accent)]",
                        item.tone === "info" && "text-[var(--ink)]",
                      )}
                    >
                      {item.title}
                    </p>
                    {item.body ? (
                      <p className="mt-0.5 text-xs text-[var(--muted)]">{item.body}</p>
                    ) : null}
                    <p className="mt-1 text-[10px] text-[var(--muted)]">
                      {new Date(item.createdAt).toLocaleString()}
                    </p>
                  </li>
                ))
              )}
            </ul>
          </div>
        ) : null}
      </div>
    </header>
  );
}
