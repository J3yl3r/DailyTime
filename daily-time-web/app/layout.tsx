import type { Metadata } from "next";
import { DM_Sans, Fraunces } from "next/font/google";
import { AppProviders } from "@/providers/app-providers";
import "./globals.css";

const dmSans = DM_Sans({
  variable: "--font-dm-sans",
  subsets: ["latin"],
});

const fraunces = Fraunces({
  variable: "--font-fraunces",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "DailyTime",
  description: "Organiza tareas, notas, tiempo y bóveda.",
  applicationName: "DailyTime",
  appleWebApp: {
    capable: true,
    title: "DailyTime",
    statusBarStyle: "default",
  },
  formatDetection: {
    telephone: false,
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="es"
      className={`${dmSans.variable} ${fraunces.variable} h-full antialiased`}
    >
      <body className="min-h-dvh font-sans text-[var(--ink)]">
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
