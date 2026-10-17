import { decodeToken } from "./jwt";
import { clearToken, getRefresh, getToken, saveTokens } from "./storage";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";
let inflight: Promise<string | null> | null = null;

/** Returns a new access token, or null. Only wipes the session when the server says the refresh token is invalid. */
export function refreshTokens(): Promise<string | null> {
  if (inflight) return inflight;
  const rt = getRefresh();
  if (!rt) return Promise.resolve(null);

  inflight = (async () => {
    try {
      const res = await fetch(`${BASE_URL}/api/Auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: rt }),
      });
      if (!res.ok) {
        if (res.status === 400 || res.status === 401 || res.status === 403) clearToken();
        return null;
      }
      const body = await res.json();
      const d = body?.data ?? body;
      if (!d?.token) return null;
      saveTokens(d.token, d.refreshToken);
      return d.token as string;
    } catch {
      return null; // network problem: keep the session, try again later
    } finally {
      inflight = null;
    }
  })();
  return inflight;
}

/** A token that is valid for at least another minute (refreshes if needed). */
export async function ensureFreshToken(): Promise<string> {
  const t = getToken();
  const d = t ? decodeToken(t) : null;
  if (t && d && d.exp * 1000 - Date.now() > 60_000) return t;
  return (await refreshTokens()) ?? t ?? "";
}

export async function authHeaders(): Promise<Record<string, string>> {
  const token = await ensureFreshToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}