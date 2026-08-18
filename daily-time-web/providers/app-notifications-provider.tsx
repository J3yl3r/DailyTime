"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { toast } from "sonner";

export type AppNotificationTone = "info" | "success" | "error";

export type AppNotification = {
  id: string;
  title: string;
  body?: string;
  tone: AppNotificationTone;
  createdAt: string;
  read: boolean;
};

type NotifyInput = {
  title: string;
  body?: string;
  tone?: AppNotificationTone;
  /** Also show a sonner toast. Default true. */
  toast?: boolean;
};

type AppNotificationsContextValue = {
  items: AppNotification[];
  unreadCount: number;
  notify: (input: NotifyInput) => void;
  markAllRead: () => void;
  clear: () => void;
};

const AppNotificationsContext = createContext<AppNotificationsContextValue | null>(
  null,
);

const MAX_ITEMS = 40;

export function AppNotificationsProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<AppNotification[]>([]);

  const notify = useCallback((input: NotifyInput) => {
    const tone = input.tone ?? "info";
    const item: AppNotification = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      title: input.title,
      body: input.body,
      tone,
      createdAt: new Date().toISOString(),
      read: false,
    };
    setItems((prev) => [item, ...prev].slice(0, MAX_ITEMS));

    if (input.toast === false) return;
    if (tone === "success") toast.success(input.title, { description: input.body, duration: 4500 });
    else if (tone === "error") toast.error(input.title, { description: input.body, duration: 5500 });
    else toast.message(input.title, { description: input.body, duration: 4000 });
  }, []);

  const markAllRead = useCallback(() => {
    setItems((prev) => prev.map((item) => ({ ...item, read: true })));
  }, []);

  const clear = useCallback(() => setItems([]), []);

  const value = useMemo<AppNotificationsContextValue>(
    () => ({
      items,
      unreadCount: items.filter((item) => !item.read).length,
      notify,
      markAllRead,
      clear,
    }),
    [items, notify, markAllRead, clear],
  );

  return (
    <AppNotificationsContext.Provider value={value}>
      {children}
    </AppNotificationsContext.Provider>
  );
}

export function useAppNotifications() {
  const ctx = useContext(AppNotificationsContext);
  if (!ctx) {
    throw new Error("useAppNotifications debe usarse dentro de AppNotificationsProvider.");
  }
  return ctx;
}
