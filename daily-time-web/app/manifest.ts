import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "DailyTime",
    short_name: "DailyTime",
    description: "Organiza tareas, notas, tiempo y bóveda.",
    start_url: "/workspace",
    display: "standalone",
    background_color: "#f8fafc",
    theme_color: "#2563eb",
    lang: "es",
    orientation: "any",
    categories: ["productivity", "utilities"],
    icons: [
      {
        src: "/icons/icon-192.png",
        sizes: "192x192",
        type: "image/png",
        purpose: "any",
      },
      {
        src: "/icons/icon-512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "any",
      },
      {
        src: "/icons/icon-512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable",
      },
    ],
  };
}
