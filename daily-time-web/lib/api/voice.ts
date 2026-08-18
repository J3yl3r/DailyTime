import { env } from "@/config/env";
import type { VoiceCommandRequest, VoiceCommandResponse } from "@/types/voice";

export class VoiceApiError extends Error {
  constructor(
    message: string,
    public status?: number
  ) {
    super(message);
    this.name = "VoiceApiError";
  }
}

export async function sendVoiceCommand(
  body: VoiceCommandRequest
): Promise<VoiceCommandResponse> {
  let res: Response;
  try {
    res = await fetch(`${env.voiceApiUrl}/api/voice/command`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify({
        text: body.text,
        work_date: body.work_date,
        dry_run: body.dry_run ?? false,
      }),
    });
  } catch {
    throw new VoiceApiError(
      "No se pudo conectar con la API de voz. ¿Está corriendo en :5400?"
    );
  }

  if (!res.ok) {
    let detail = `Error HTTP ${res.status}`;
    try {
      const payload = (await res.json()) as { detail?: string; message?: string };
      detail = payload.detail ?? payload.message ?? detail;
    } catch {
      /* ignore */
    }
    throw new VoiceApiError(detail, res.status);
  }

  return (await res.json()) as VoiceCommandResponse;
}
