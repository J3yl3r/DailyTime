"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import {
  Building2,
  BriefcaseBusiness,
  CalendarDays,
  ChevronDown,
  Clock3,
  Columns3,
  FileUser,
  FolderKanban,
  KeyRound,
  Library,
  Newspaper,
  PanelLeftClose,
  Globe2,
  Server,
  Shapes,
  Tags,
  UserCircle,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils/cn";
import { useSidebar } from "@/providers/sidebar-provider";

type NavItem = {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
};

type NavGroup = {
  id: string;
  label: string;
  items: NavItem[];
};

const NAV_GROUPS: NavGroup[] = [
  {
    id: "day",
    label: "Día a día",
    items: [
      { href: "/board", label: "Tablero", icon: Columns3 },
      { href: "/calendar", label: "Calendario", icon: CalendarDays },
      { href: "/workspace", label: "Informe de tiempo", icon: Clock3 },
    ],
  },
  {
    id: "catalogs",
    label: "Catálogos",
    items: [
      { href: "/statuses", label: "Estados", icon: Tags },
      { href: "/categories", label: "Categorías", icon: Shapes },
      { href: "/people", label: "Personas", icon: Users },
      { href: "/projects", label: "Proyectos", icon: FolderKanban },
      { href: "/companies", label: "Empresas", icon: Building2 },
    ],
  },
  {
    id: "vault",
    label: "Bóveda",
    items: [
      { href: "/vault", label: "Credenciales", icon: KeyRound },
      { href: "/vault/services", label: "Servicios", icon: Server },
    ],
  },
  {
    id: "career",
    label: "Carrera",
    items: [
      { href: "/career/profile", label: "Perfil", icon: UserCircle },
      { href: "/experiences", label: "Experiencias", icon: FileUser },
      { href: "/applications", label: "Postulaciones", icon: BriefcaseBusiness },
      { href: "/career/offers", label: "Ofertas", icon: Newspaper },
      { href: "/career/portals", label: "Portales", icon: Globe2 },
      { href: "/career/catalogs", label: "Datos reutilizables", icon: Library },
    ],
  },
];

function isActivePath(pathname: string, href: string) {
  if (href === "/vault") {
    return pathname === "/vault";
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

function groupHasActive(pathname: string, group: NavGroup) {
  return group.items.some((item) => isActivePath(pathname, item.href));
}

export function Sidebar() {
  const pathname = usePathname();
  const { open, setOpen } = useSidebar();
  const activeGroupIds = useMemo(
    () => NAV_GROUPS.filter((group) => groupHasActive(pathname, group)).map((g) => g.id),
    [pathname],
  );
  // Al abrir la app: grupos cerrados; solo se abre el del módulo activo.
  const [expanded, setExpanded] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(NAV_GROUPS.map((group) => [group.id, false])),
  );

  useEffect(() => {
    setExpanded(() => {
      const next = Object.fromEntries(
        NAV_GROUPS.map((group) => [group.id, false]),
      ) as Record<string, boolean>;
      for (const id of activeGroupIds) next[id] = true;
      return next;
    });
  }, [activeGroupIds]);

  return (
    <aside
      className={cn(
        "sticky top-0 flex h-dvh shrink-0 flex-col border-r border-[var(--border)] bg-[var(--surface)] transition-[width,opacity,transform] duration-300 ease-out",
        open
          ? "w-60 translate-x-0 opacity-100"
          : "pointer-events-none absolute -translate-x-full opacity-0 md:w-0 md:overflow-hidden md:border-0",
      )}
      aria-hidden={!open}
    >
      <div className="flex items-center justify-between gap-2 px-4 py-5">
        <Link href="/workspace" className="group min-w-0">
          <span className="font-[family-name:var(--font-fraunces)] text-xl font-semibold tracking-tight text-[var(--ink)] transition-colors group-hover:text-[var(--accent)]">
            DailyTime
          </span>
        </Link>
        <button
          type="button"
          onClick={() => setOpen(false)}
          className="rounded-md p-1.5 text-[var(--muted)] transition-colors hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
          aria-label="Ocultar menú"
          title="Ocultar menú"
        >
          <PanelLeftClose className="size-4" />
        </button>
      </div>

      <nav className="flex flex-1 flex-col gap-3 overflow-y-auto px-3 pb-4">
        {NAV_GROUPS.map((group) => {
          const isOpen = expanded[group.id] !== false;
          return (
            <div key={group.id} className="flex flex-col gap-1">
              <button
                type="button"
                onClick={() =>
                  setExpanded((current) => ({
                    ...current,
                    [group.id]: !isOpen,
                  }))
                }
                className="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-[11px] font-semibold uppercase tracking-[0.08em] text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
                aria-expanded={isOpen}
              >
                <span>{group.label}</span>
                <ChevronDown
                  className={cn(
                    "size-3.5 transition-transform",
                    isOpen ? "rotate-0" : "-rotate-90",
                  )}
                />
              </button>
              {isOpen ? (
                <div className="flex flex-col gap-0.5 border-l border-[var(--border)] ml-2 pl-2">
                  {group.items.map(({ href, label, icon: Icon }) => {
                    const active = isActivePath(pathname, href);
                    return (
                      <Link
                        key={href}
                        href={href}
                        className={cn(
                          "flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors",
                          active
                            ? "bg-[var(--accent-soft)] text-[var(--accent)]"
                            : "text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]",
                        )}
                      >
                        <Icon className="size-4 shrink-0" />
                        {label}
                      </Link>
                    );
                  })}
                </div>
              ) : null}
            </div>
          );
        })}
      </nav>

      <div className="border-t border-[var(--border)] px-3 py-4">
        <button
          type="button"
          disabled
          title="Próximamente"
          className="flex w-full cursor-not-allowed items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-[var(--muted)] opacity-60"
        >
          <UserCircle className="size-4 shrink-0" />
          <span className="flex flex-col items-start leading-tight">
            <span>Cuenta</span>
            <span className="text-xs font-normal">Próximamente</span>
          </span>
        </button>
      </div>
    </aside>
  );
}
