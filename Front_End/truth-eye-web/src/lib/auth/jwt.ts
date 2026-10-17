import type { Role, SessionUser } from "@/types/api";

const CLAIMS = {
  id: ["nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
  email: ["email", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
  role: ["role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
  name: ["unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
};

function pick(payload: Record<string, unknown>, keys: string[]) {
  for (const k of keys) if (payload[k] != null) return String(payload[k]);
  return "";
}

export function decodeToken(token: string): (SessionUser & { exp: number }) | null {
  try {
    const part = token.split(".")[1];
    const b64 = part.replace(/-/g, "+").replace(/_/g, "/");
    const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
    const payload = JSON.parse(new TextDecoder().decode(bytes));

    return {
      id: Number(pick(payload, CLAIMS.id)),
      email: pick(payload, CLAIMS.email),
      name: pick(payload, CLAIMS.name),
      role: pick(payload, CLAIMS.role) as Role,
      exp: Number(payload.exp ?? 0),
    };
  } catch {
    return null;
  }
}

export function isExpired(exp: number) {
  return exp * 1000 < Date.now();
}