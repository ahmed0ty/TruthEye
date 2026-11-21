import Cookies from "js-cookie";

export const TOKEN_COOKIE = "te_token";
export const SESSION_COOKIE = "te_session"; // علامة بس، مفيهاش أي سر. الـ refresh الحقيقي HttpOnly عند الباك
const LEGACY_REFRESH_COOKIE = "te_refresh";

const opts = () => ({
  expires: 7,
  sameSite: "lax" as const,
  secure: typeof location !== "undefined" && location.protocol === "https:",
});

export const getToken = () => Cookies.get(TOKEN_COOKIE) ?? null;
export const hasSession = () => Cookies.get(SESSION_COOKIE) === "1";

export function saveTokens(token: string) {
  Cookies.set(TOKEN_COOKIE, token, opts());
  Cookies.set(SESSION_COOKIE, "1", opts());
  Cookies.remove(LEGACY_REFRESH_COOKIE);
}

export function clearToken() {
  Cookies.remove(TOKEN_COOKIE);
  Cookies.remove(SESSION_COOKIE);
  Cookies.remove(LEGACY_REFRESH_COOKIE);
}