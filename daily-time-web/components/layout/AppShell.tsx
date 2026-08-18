"use client";

import type { ReactNode } from "react";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";
import { VoiceDock } from "@/components/features/voice/VoiceDock";
import { useSidebar } from "@/providers/sidebar-provider";
import { cn } from "@/lib/utils/cn";

export function AppShell({ children }: { children: ReactNode }) {
  const { open } = useSidebar();

  return (
    <div className="flex min-h-dvh bg-[var(--canvas)]">
      <Sidebar />
      <div
        className={cn(
          "flex min-h-dvh min-w-0 flex-1 flex-col transition-[margin] duration-300",
          !open && "md:ml-0"
        )}
      >
        <Topbar />
        <main className="flex-1 p-4 md:p-6">{children}</main>
      </div>
      <VoiceDock />
    </div>
  );
}
