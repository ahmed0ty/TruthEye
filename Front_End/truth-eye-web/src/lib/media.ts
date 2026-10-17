const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

/** Returns a full image URL, or null when the user has no uploaded photo (default.jpg etc.) */
export function mediaUrl(path?: string | null): string | null {
  if (!path || !path.startsWith("/uploads/")) return null;
  return `${BASE_URL}${path}`;
}