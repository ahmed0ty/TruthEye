import { NextResponse, type NextRequest } from "next/server";
import { decodeToken, isExpired } from "@/lib/auth/jwt";
import { ROLE_HOME, type Role } from "@/types/api";

const GUARDS: Record<string, Role[]> = {
  "/student": ["Student"],
  "/doctor": ["Doctor"],
  "/admin": ["Admin", "FacultySecretary", "TechnicalStaff", "SuperAdmin"],
  "/proctor": ["Proctor", "Admin", "FacultySecretary", "TechnicalStaff"],
  "/support": ["Support", "SuperAdmin"],
  "/exam": ["Student"],
};

const isDev = process.env.NODE_ENV !== "production";

function newNonce() {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  return btoa(String.fromCharCode(...bytes));
}

// Scripts must carry this nonce. 'unsafe-inline' is gone, so an injected <script> has no nonce and will not run.
function buildCsp(nonce: string) {
  const api = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";
  return [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}' 'strict-dynamic' 'wasm-unsafe-eval'${isDev ? " 'unsafe-eval'" : ""}`,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: blob: https: http://localhost:5210",
    "media-src 'self' blob:",
    "font-src 'self' data:",
    "connect-src 'self' https: wss: http://localhost:5210 ws://localhost:5210 http://localhost:8000",
    "worker-src 'self' blob:",
    "object-src 'none'",
    "base-uri 'self'",
    "frame-ancestors 'none'",
    `report-uri ${api}/api/csp-report`,
  ].join("; ");
}

// Login state and role checks. Returns a redirect, or null to let the request continue.
function guard(req: NextRequest): NextResponse | null {
  const { pathname } = req.nextUrl;
  if (pathname === "/") return null;

  const token = req.cookies.get("te_token")?.value;
  const session = req.cookies.get("te_session")?.value;
  const user = token ? decodeToken(token) : null;
  const valid = !!user && (!isExpired(user.exp) || !!session);

  if (pathname === "/login" || pathname === "/register") {
    if (valid) return NextResponse.redirect(new URL(ROLE_HOME[user!.role] ?? "/", req.url));
    return null;
  }

  if (!valid) {
    const res = NextResponse.redirect(new URL("/login", req.url));
    res.cookies.delete("te_token");
    res.cookies.delete("te_session");
    return res;
  }

  for (const [prefix, roles] of Object.entries(GUARDS)) {
    if (pathname === prefix || pathname.startsWith(prefix + "/")) {
      if (!roles.includes(user!.role)) {
        return NextResponse.redirect(new URL(ROLE_HOME[user!.role] ?? "/login", req.url));
      }
    }
  }
  return null;
}

export function proxy(req: NextRequest) {
  const nonce = newNonce();
  const csp = buildCsp(nonce);

  const requestHeaders = new Headers(req.headers);
  requestHeaders.set("x-nonce", nonce);
  requestHeaders.set("Content-Security-Policy", csp);

  const res = guard(req) ?? NextResponse.next({ request: { headers: requestHeaders } });
  res.headers.set("Content-Security-Policy", csp);
  return res;
}

export const config = {
  matcher: ["/", "/login", "/register", "/change-password", "/profile/:path*", "/chat/:path*", "/student/:path*", "/doctor/:path*", "/admin/:path*", "/proctor/:path*", "/support/:path*", "/exam/:path*"],
}
