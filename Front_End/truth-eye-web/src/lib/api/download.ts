import { authHeaders } from "@/lib/auth/refresh";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export async function downloadFile(path: string, fallbackName: string) {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: await authHeaders(),
  });

  if (!res.ok) {
    let message = "Download failed";
    try {
      const body = await res.json();
      message = body?.error ?? body?.Error ?? message;
    } catch {
      if (res.status === 404) message = "Report not found";
      if (res.status === 403) message = "You don't have access to this report";
    }
    throw new Error(message);
  }

  const cd = res.headers.get("content-disposition") ?? "";
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(cd);
  const name = match ? decodeURIComponent(match[1]) : fallbackName;

  const url = URL.createObjectURL(await res.blob());
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}