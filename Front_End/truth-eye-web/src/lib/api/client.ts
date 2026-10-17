import { ensureFreshToken, refreshTokens } from "@/lib/auth/refresh";
import { clearToken, getRefresh } from "@/lib/auth/storage";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public validation?: Record<string, string[]>
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type Query = Record<string, string | number | boolean | undefined | null>;

interface Options {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  query?: Query;
  signal?: AbortSignal;
}

function buildUrl(path: string, query?: Query) {
  const url = new URL(path, BASE_URL);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null && v !== "") url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}

function errorMessage(body: any, status: number): string {
  if (body?.error) return body.error;
  if (body?.message) return body.message;
  if (body?.errors) {
    const first = Object.values(body.errors as Record<string, string[]>)[0];
    if (first?.[0]) return first[0];
  }
  if (body?.title) return body.title;
  if (status === 401) return "انتهت الجلسة، سجّل الدخول تاني";
  if (status === 403) return "مش مسموحلك بالعملية دي";
  if (status >= 500) return "حصل خطأ في السيرفر";
  return "حصل خطأ غير متوقع";
}

export async function api<T = unknown>(path: string, opts: Options = {}): Promise<T> {
  const isLogin = path === "/api/Auth/login";

  const send = async () => {
    const token = isLogin ? null : await ensureFreshToken();
    return fetch(buildUrl(path, opts.query), {
      method: opts.method ?? "GET",
      headers: {
        ...(opts.body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
      signal: opts.signal,
    });
  };
  const offline = () => new ApiError("مقدرتش أوصل للسيرفر. اتأكد إنه شغال.", 0);

  let res: Response;
  try {
    res = await send();
  } catch {
    throw offline();
  }

  // Expired token: refresh once and retry the same request
  if (res.status === 401 && !isLogin && getRefresh()) {
    const fresh = await refreshTokens();
    if (fresh) {
      try {
        res = await send();
      } catch {
        throw offline();
      }
    }
  }

  const text = await res.text();
  let body: any = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }

  if (!res.ok || body?.success === false) {
    if (res.status === 401 && !isLogin && !getRefresh() && typeof window !== "undefined") {
      clearToken();
      window.location.href = "/login";
    }
    throw new ApiError(errorMessage(body, res.status), res.status, body?.errors);
  }

  // فك الـ wrapper: { success, message, data } -> data
  if (body && typeof body === "object" && !Array.isArray(body) && "success" in body && "data" in body) {
    return body.data as T;
  }
  return body as T;
}