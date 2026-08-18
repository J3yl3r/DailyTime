"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { useState } from "react";
import { Toaster } from "sonner";
import { SidebarProvider } from "@/providers/sidebar-provider";
import { ConfirmProvider } from "@/providers/confirm-provider";
import { AppNotificationsProvider } from "@/providers/app-notifications-provider";
import { ScrapeSchedulerProvider } from "@/providers/scrape-scheduler-provider";
import { VoiceFormProvider } from "@/providers/voice-form-provider";
import { VoiceUiBridgeProvider } from "@/providers/voice-ui-bridge";
import { AppShell } from "@/components/layout/AppShell";

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ConfirmProvider>
        <AppNotificationsProvider>
          <ScrapeSchedulerProvider>
            <SidebarProvider>
              <VoiceUiBridgeProvider>
                <VoiceFormProvider>
                  <AppShell>{children}</AppShell>
                </VoiceFormProvider>
              </VoiceUiBridgeProvider>
              <Toaster
                richColors
                position="bottom-right"
                closeButton
                duration={2500}
                toastOptions={{
                  classNames: {
                    toast: "relative",
                    closeButton:
                      "!left-auto !right-1 !top-1 !transform-none border border-[var(--border)] bg-[var(--surface)] text-[var(--muted)] hover:!bg-[var(--surface-muted)] hover:!text-[var(--ink)]",
                  },
                }}
              />
            </SidebarProvider>
          </ScrapeSchedulerProvider>
        </AppNotificationsProvider>
      </ConfirmProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
