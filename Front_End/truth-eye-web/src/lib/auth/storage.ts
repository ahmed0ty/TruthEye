import Cookies from "js-cookie";

export const TOKEN_COOKIE = "te_token";
export const REFRESH_COOKIE = "te_refresh";

const opts = () => ({
  expires: 7,
  sameSite: "lax" as const,
  secure: typeof location !== "undefined" && location.protocol === "https:",
});

export const getToken = () => Cookies.get(TOKEN_COOKIE) ?? null;
export const getRefresh = () => Cookies.get(REFRESH_COOKIE) ?? null;

export function saveTokens(token: string, refresh?: string) {
  Cookies.set(TOKEN_COOKIE, token, opts());
  if (refresh) Cookies.set(REFRESH_COOKIE, refresh, opts());
}

export function clearToken() {
  Cookies.remove(TOKEN_COOKIE);
  Cookies.remove(REFRESH_COOKIE);
}