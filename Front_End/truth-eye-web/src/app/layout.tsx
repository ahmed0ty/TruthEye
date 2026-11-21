import "./globals.css";

import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import { Inter } from "next/font/google";
import { Providers } from "@/components/providers";

const inter = Inter({ subsets: ["latin"], display: "swap" });

export const metadata: Metadata = {
  title: "ExamGurd",
  description: "Online exam platform with smart proctoring",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
};

// Adds the "dark" class before the first paint, so there is no white flash
const themeScript = `try{var t=localStorage.getItem("te_theme");if(t==="dark"||(!t&&matchMedia("(prefers-color-scheme: dark)").matches))document.documentElement.classList.add("dark")}catch(e){}`;

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  // The proxy sets this per request; the inline script must carry it to pass the CSP
  const nonce = (await headers()).get("x-nonce") ?? undefined;
  return (
    <html lang="en" dir="ltr" suppressHydrationWarning>
      <head>
        <script nonce={nonce} dangerouslySetInnerHTML={{ __html: themeScript }} />
      </head>
      <body className={`${inter.className} bg-slate-50 text-slate-900 antialiased`}>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}