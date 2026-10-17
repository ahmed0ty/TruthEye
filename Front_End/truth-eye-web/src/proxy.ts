import { NextResponse, type NextRequest } from "next/server";
import { decodeToken, isExpired } from "@/lib/auth/jwt";
import { ROLE_HOME, type Role } from "@/types/api";

const GUARDS: Record<string, Role[]> = {
  "/student": ["Student"],
  "/doctor": ["Doctor"],
  "/admin": ["Admin", "SuperAdmin"],
  "/proctor": ["Proctor", "Admin"],
  "/support": ["Support", "SuperAdmin"],
  "/exam": ["Student"],
};

export function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;
  const token = req.cookies.get("te_token")?.value;
const refresh = req.cookies.get("te_refresh")?.value;
const user = token ? decodeToken(token) : null;
const valid = !!user && (!isExpired(user.exp) || !!refresh);

    if (pathname === "/login" || pathname === "/register") {
    if (valid) return NextResponse.redirect(new URL(ROLE_HOME[user!.role] ?? "/", req.url));
    return NextResponse.next();
  }

  if (!valid) {
    const res = NextResponse.redirect(new URL("/login", req.url));
    res.cookies.delete("te_token");
    return res;
  }

  for (const [prefix, roles] of Object.entries(GUARDS)) {
    if (pathname === prefix || pathname.startsWith(prefix + "/")) {
      if (!roles.includes(user!.role)) {
        return NextResponse.redirect(new URL(ROLE_HOME[user!.role] ?? "/login", req.url));
      }
    }
  }
  return NextResponse.next();
}

export const config = {
  matcher: ["/login", "/register", "/profile/:path*", "/chat/:path*", "/student/:path*", "/doctor/:path*", "/admin/:path*", "/proctor/:path*", "/support/:path*", "/exam/:path*"],    
  }