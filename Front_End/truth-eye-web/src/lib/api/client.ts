import { ensureFreshToken, refreshTokens } from "@/lib/auth/refresh";
import { clearToken, hasSession } from "@/lib/auth/storage";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public validation?: Record<string, string[]>,
    public errorCode?: string
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

// رسائل عربي مقابلة لأكواد الأخطاء اللي بيرجّعها الباك
const ERROR_CODE_MESSAGES: Record<string, string> = {
  PASSWORD_CHANGE_REQUIRED: "لازم تغيّر كلمة المرور الأول",
  EXAM_COURSE_NOT_TEACHING: "تقدر تضيف امتحانات للمواد اللي بتدرّسها بس",
  EXAM_HAS_ATTEMPTS: "الامتحان ده فيه محاولات، فمينفعش ينتقل لمادة تانية",
  EXAM_TIMING_LOCKED: "فيه طلاب بيمتحنوا دلوقتي، مينفعش تغيّر الوقت أو الدرجات لحد ما يخلّصوا",
  COURSE_ACTION_FAILED: "حصل خطأ في العملية دي، جرّب تاني",
  AUTH_INVALID_CREDENTIALS: "الرقم أو كلمة المرور غلط",
  AUTH_LOCKED: "محاولات كتير غلط. استنى شوية وجرّب تاني",
  AUTH_DISABLED: "الحساب ده متعطّل. كلّم الإدارة",
  AUTH_OTHER_DEVICE: "الحساب ده مفتوح على جهاز تاني دلوقتي",
  AUTH_FAILED: "حصل خطأ في تسجيل الدخول، جرّب تاني",
};

function buildUrl(path: string, query?: Query) {
  const url = new URL(path, BASE_URL);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null && v !== "") url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}

function errorMessage(body: any, status: number, isLogin = false): string {
  const code = body?.errorCode;
  if (code && ERROR_CODE_MESSAGES[code]) return ERROR_CODE_MESSAGES[code];

  if (status === 401 && !isLogin) return "انتهت الجلسة، سجّل الدخول تاني";
  if (status === 403) return "مش مسموحلك بالعملية دي";
  if (status === 404) return "مش لاقي اللي بتدوّر عليه";
  if (status >= 500) return "حصل خطأ في السيرفر";

  if (body?.error) return body.error;
  if (body?.message) return body.message;
  if (body?.errors) {
    const first = Object.values(body.errors as Record<string, string[]>)[0];
    if (first?.[0]) return first[0];
  }
  if (body?.title) return body.title;

  return "حصل خطأ غير متوقع";
}

export async function api<T = unknown>(path: string, opts: Options = {}): Promise<T> {
  const isLogin = path === "/api/Auth/login";

  const send = async () => {
    const token = isLogin ? null : await ensureFreshToken();
    return fetch(buildUrl(path, opts.query), {
      method: opts.method ?? "GET",
      credentials: "include",
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
  if (res.status === 401 && !isLogin && hasSession()) {
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
    if (res.status === 401 && !isLogin && !hasSession() && typeof window !== "undefined") {
      clearToken();
      window.location.href = "/login";
    }

    if (
      res.status === 403 &&
      body?.errorCode === "PASSWORD_CHANGE_REQUIRED" &&
      typeof window !== "undefined" &&
      window.location.pathname !== "/change-password"
    ) {
      window.location.href = "/change-password";
    }

    throw new ApiError(errorMessage(body, res.status, isLogin), res.status, body?.errors, body?.errorCode);
  }

  if (body && typeof body === "object" && !Array.isArray(body) && "success" in body && "data" in body) {
    return body.data as T;
  }
  return body as T;
}