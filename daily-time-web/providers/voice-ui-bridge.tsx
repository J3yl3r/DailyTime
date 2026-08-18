"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  type ReactNode,
} from "react";
import type { VoiceCalendarCommand } from "@/types/voice";

type CalendarHandler = (command: VoiceCalendarCommand) => void;

type VoiceUiBridgeValue = {
  registerCalendarHandler: (handler: CalendarHandler) => () => void;
  dispatchCalendarCommand: (command: VoiceCalendarCommand) => void;
};

const VoiceUiBridgeContext = createContext<VoiceUiBridgeValue | null>(null);

export function VoiceUiBridgeProvider({ children }: { children: ReactNode }) {
  const calendarHandlerRef = useRef<CalendarHandler | null>(null);
  const pendingCalendarRef = useRef<VoiceCalendarCommand | null>(null);

  const registerCalendarHandler = useCallback((handler: CalendarHandler) => {
    calendarHandlerRef.current = handler;
    if (pendingCalendarRef.current) {
      const pending = pendingCalendarRef.current;
      pendingCalendarRef.current = null;
      handler(pending);
    }
    return () => {
      if (calendarHandlerRef.current === handler) {
        calendarHandlerRef.current = null;
      }
    };
  }, []);

  const dispatchCalendarCommand = useCallback((command: VoiceCalendarCommand) => {
    if (calendarHandlerRef.current) {
      calendarHandlerRef.current(command);
      return;
    }
    pendingCalendarRef.current = command;
  }, []);

  const value = useMemo(
    () => ({
      registerCalendarHandler,
      dispatchCalendarCommand,
    }),
    [dispatchCalendarCommand, registerCalendarHandler],
  );

  return (
    <VoiceUiBridgeContext.Provider value={value}>
      {children}
    </VoiceUiBridgeContext.Provider>
  );
}

export function useVoiceUiBridge() {
  const context = useContext(VoiceUiBridgeContext);
  if (!context) {
    throw new Error("useVoiceUiBridge debe usarse dentro de VoiceUiBridgeProvider");
  }
  return context;
}
