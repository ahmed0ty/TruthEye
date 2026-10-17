import { api } from "./client";
import { authHeaders } from "@/lib/auth/refresh";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export async function uploadAvatar(blob: Blob): Promise<string> {
  const fd = new FormData();
  fd.append("file", blob, "avatar.jpg");

  const res = await fetch(`${BASE_URL}/api/Profile/image`, {
    method: "POST",
    headers: await authHeaders(),
    body: fd,
  });

  const body = await res.json().catch(() => null);
  if (!res.ok) throw new Error(body?.error ?? body?.Error ?? "Upload failed");
  return body?.data?.profileImage as string;
}

export const removeAvatar = () => api("/api/Profile/image", { method: "DELETE" });

/** Center-crop to a square and shrink, so phone photos never hit the 2MB limit */
export async function resizeSquare(file: File, size = 512): Promise<Blob> {
  const bmp = await createImageBitmap(file);
  const side = Math.min(bmp.width, bmp.height);
  const canvas = document.createElement("canvas");
  canvas.width = size;
  canvas.height = size;
  canvas
    .getContext("2d")!
    .drawImage(bmp, (bmp.width - side) / 2, (bmp.height - side) / 2, side, side, 0, 0, size, size);
  bmp.close();
  return new Promise((resolve, reject) =>
    canvas.toBlob(
      (b) => (b ? resolve(b) : reject(new Error("Couldn't process the image"))),
      "image/jpeg",
      0.88
    )
  );
}