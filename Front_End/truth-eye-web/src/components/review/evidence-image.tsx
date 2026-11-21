"use client";

import { useEffect, useState } from "react";
import { authHeaders } from "@/lib/auth/refresh";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export function EvidenceImage({ name, alt }: { name: string; alt: string }) {
  const [src, setSrc] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let url: string | null = null;
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch(`${BASE_URL}/api/AILog/evidence/${encodeURIComponent(name)}`, {
          credentials: "include",
          headers: await authHeaders(),
        });
        if (!res.ok) throw new Error();
        url = URL.createObjectURL(await res.blob());
        if (!cancelled) setSrc(url);
      } catch {
        if (!cancelled) setFailed(true);
      }
    })();
    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [name]);

  if (failed) return <div className="flex h-32 items-center justify-center rounded-lg bg-slate-100 text-xs text-slate-400">Image unavailable</div>;
  if (!src) return <div className="h-32 animate-pulse rounded-lg bg-slate-100" />;
  // eslint-disable-next-line @next/next/no-img-element
  return <img src={src} alt={alt} className="h-32 w-full rounded-lg object-cover" />;
}