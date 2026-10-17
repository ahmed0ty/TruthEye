import { authHeaders } from "@/lib/auth/refresh";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export async function uploadEvidence(
  sessionId: number,
  eventType: string,
  confidence: number,
  blob: Blob
) {
  const fd = new FormData();
  fd.append("sessionId", String(sessionId));
  fd.append("eventType", eventType);
  fd.append("confidence", String(confidence));
  fd.append("file", blob, "evidence.jpg");

  const res = await fetch(`${BASE_URL}/api/AILog/evidence`, {
    method: "POST",
    headers: await authHeaders(),
    body: fd,
  });
  if (!res.ok) throw new Error("Evidence upload failed");
}

export async function fetchEvidence(name: string): Promise<Blob> {
  const res = await fetch(`${BASE_URL}/api/AILog/evidence/${encodeURIComponent(name)}`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error("Couldn't load the image");
  return res.blob();
}