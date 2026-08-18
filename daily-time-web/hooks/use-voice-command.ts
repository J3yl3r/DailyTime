"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { sendVoiceCommand, VoiceApiError } from "@/lib/api/voice";
import type { VoiceCommandResponse } from "@/types/voice";

export type VoiceUiStatus = "idle" | "listening" | "sending" | "error";

type SpeechRecognitionLike = {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  start: () => void;
  stop: () => void;
  abort: () => void;
  onresult: ((event: SpeechRecognitionEventLike) => void) | null;
  onerror: ((event: { error: string }) => void) | null;
  onend: (() => void) | null;
};

type SpeechRecognitionEventLike = {
  resultIndex: number;
  results: ArrayLike<{
    isFinal: boolean;
    0: { transcript: string };
  }>;
};

type SpeechRecognitionConstructor = new () => SpeechRecognitionLike;

function getSpeechRecognitionCtor(): SpeechRecognitionConstructor | null {
  if (typeof window === "undefined") return null;
  const w = window as Window & {
    SpeechRecognition?: SpeechRecognitionConstructor;
    webkitSpeechRecognition?: SpeechRecognitionConstructor;
  };
  return w.SpeechRecognition ?? w.webkitSpeechRecognition ?? null;
}

export function useVoiceCommand(options?: {
  sessionMode?: boolean;
  beforeSend?: (text: string) => Promise<boolean>;
}) {
  const sessionMode = options?.sessionMode ?? false;
  const beforeSendRef = useRef(options?.beforeSend);
  beforeSendRef.current = options?.beforeSend;
  const [status, setStatus] = useState<VoiceUiStatus>("idle");
  const [supported, setSupported] = useState(false);
  const [transcript, setTranscript] = useState("");
  const [interim, setInterim] = useState("");
  const [lastResult, setLastResult] = useState<VoiceCommandResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [armed, setArmed] = useState(false);

  const recognitionRef = useRef<SpeechRecognitionLike | null>(null);
  const autoSendRef = useRef(false);
  const transcriptRef = useRef("");
  const armedRef = useRef(false);
  const startListeningRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    setSupported(getSpeechRecognitionCtor() != null);
  }, []);

  const updateTranscript = useCallback((value: string) => {
    transcriptRef.current = value;
    setTranscript(value);
  }, []);

  const setSessionArmed = useCallback((value: boolean) => {
    armedRef.current = value;
    setArmed(value);
    if (!value) {
      autoSendRef.current = false;
    }
  }, []);

  const stopListening = useCallback(() => {
    autoSendRef.current = false;
    const recognition = recognitionRef.current;
    if (recognition) {
      try {
        recognition.onresult = null;
        recognition.onerror = null;
        recognition.onend = null;
        recognition.stop();
      } catch {
        /* ignore */
      }
      recognitionRef.current = null;
    }
    setInterim("");
    setStatus((current) => (current === "listening" ? "idle" : current));
  }, []);

  /** Ante fallo: apaga la sesión para que el usuario reactive el micrófono a mano. */
  const disarmSession = useCallback((message?: string) => {
    if (!sessionMode) return;
    setSessionArmed(false);
    stopListening();
    if (message) toast.error(message);
  }, [sessionMode, setSessionArmed, stopListening]);

  const sendText = useCallback(async (text: string) => {
    const command = text.trim();
    if (!command) {
      const message = "Escribe o dicta un comando primero.";
      setError(message);
      setStatus("error");
      disarmSession(message);
      return null;
    }

    setStatus("sending");
    setError(null);
    updateTranscript(command);
    setInterim("");

    if (beforeSendRef.current) {
      const proceed = await beforeSendRef.current(command);
      if (!proceed) {
        setStatus("idle");
        updateTranscript("");
        if (sessionMode && armedRef.current) {
          window.setTimeout(() => {
            if (armedRef.current) startListeningRef.current?.();
          }, 400);
        }
        return null;
      }
    }

    try {
      const result = await sendVoiceCommand({ text: command });
      setLastResult(result);
      if (!result.success) {
        setError(result.message);
        setStatus("error");
        // Toast lo muestra VoiceDock vía lastResult; aquí solo apagamos sesión
        disarmSession();
      } else {
        setStatus("idle");
        if (sessionMode && armedRef.current) {
          window.setTimeout(() => {
            if (armedRef.current) startListeningRef.current?.();
          }, 400);
        }
      }

      return result;
    } catch (err) {
      const message =
        err instanceof VoiceApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "No se pudo ejecutar el comando";
      setError(message);
      setStatus("error");
      setLastResult(null);
      disarmSession(message);
      return null;
    }
  }, [disarmSession, sessionMode, updateTranscript]);

  const startListening = useCallback(() => {
    const Ctor = getSpeechRecognitionCtor();
    if (!Ctor) {
      setError(
        "Tu navegador no soporta reconocimiento de voz. Usa Chrome o Edge, o escribe el comando."
      );
      setStatus("error");
      return;
    }

    stopListening();
    setError(null);
    setLastResult(null);
    updateTranscript("");
    setInterim("");
    autoSendRef.current = true;

    const recognition = new Ctor();
    recognition.lang = "es-ES";
    recognition.continuous = false;
    recognition.interimResults = true;
    recognitionRef.current = recognition;

    recognition.onresult = (event) => {
      let finalChunk = "";
      let interimChunk = "";
      for (let i = event.resultIndex; i < event.results.length; i += 1) {
        const piece = event.results[i][0].transcript;
        if (event.results[i].isFinal) finalChunk += piece;
        else interimChunk += piece;
      }
      if (finalChunk) {
        const next = `${transcriptRef.current} ${finalChunk}`.trim();
        updateTranscript(next);
      }
      setInterim(interimChunk);
    };

    recognition.onerror = (event) => {
      if (event.error === "aborted" || event.error === "no-speech") {
        // onend se encarga de apagar la sesión si no hubo texto
        autoSendRef.current = event.error === "no-speech" ? autoSendRef.current : false;
        setStatus("idle");
        return;
      }
      const message =
        event.error === "not-allowed"
          ? "Permiso de micrófono denegado."
          : `Error de voz: ${event.error}`;
      setError(message);
      setStatus("error");
      autoSendRef.current = false;
      disarmSession(message);
    };

    recognition.onend = () => {
      recognitionRef.current = null;
      setInterim("");
      if (!autoSendRef.current) {
        setStatus((current) => (current === "listening" ? "idle" : current));
        return;
      }
      autoSendRef.current = false;
      const text = transcriptRef.current.trim();
      if (text) {
        void sendText(text);
      } else {
        setStatus("idle");
        if (sessionMode) {
          disarmSession("No se detectó voz. Activa de nuevo el micrófono para reintentar.");
        } else {
          setError("No se detectó voz. Intenta de nuevo o escribe el comando.");
        }
      }
    };

    try {
      setStatus("listening");
      recognition.start();
    } catch {
      const message = "No se pudo iniciar el micrófono.";
      setError(message);
      setStatus("error");
      disarmSession(message);
    }
  }, [disarmSession, sendText, sessionMode, stopListening, updateTranscript]);

  startListeningRef.current = startListening;

  const toggleListening = useCallback(() => {
    if (status === "listening") {
      stopListening();
      return;
    }
    if (status === "sending") return;
    updateTranscript("");
    startListening();
  }, [startListening, status, stopListening, updateTranscript]);

  const toggleSession = useCallback(() => {
    if (!sessionMode) return;
    if (armedRef.current) {
      setSessionArmed(false);
      stopListening();
      return;
    }
    if (status === "sending") return;
    setSessionArmed(true);
    setError(null);
    updateTranscript("");
    startListening();
  }, [sessionMode, setSessionArmed, startListening, status, stopListening, updateTranscript]);

  useEffect(() => {
    return () => {
      const recognition = recognitionRef.current;
      if (recognition) {
        try {
          recognition.abort();
        } catch {
          /* ignore */
        }
      }
    };
  }, []);

  return {
    status,
    supported,
    transcript,
    interim,
    lastResult,
    error,
    armed,
    setTranscript: updateTranscript,
    setError,
    setSessionArmed,
    startListening,
    stopListening,
    toggleListening,
    toggleSession,
    sendText,
  };
}
