export const env = {
  apiUrl: process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:5110",
  voiceApiUrl: process.env.NEXT_PUBLIC_VOICE_API_URL ?? "http://localhost:5400",
  workerApiUrl: process.env.NEXT_PUBLIC_WORKER_API_URL ?? "http://localhost:5500",
} as const;
