/* eslint-disable @next/next/no-img-element */
import { mediaUrl } from "@/lib/media";
import { cn } from "@/lib/utils";

export function Avatar({
  name,
  src,
  size = 40,
  className,
}: {
  name: string;
  src?: string | null;
  size?: number;
  className?: string;
}) {
  const url = mediaUrl(src);
  const initials = name.replace("Dr. ", "").split(" ").map((p) => p[0]).slice(0, 2).join("");

  if (url) {
    return (
      <img
        src={url}
        alt={name}
        style={{ width: size, height: size }}
        className={cn("shrink-0 rounded-xl object-cover", className)}
      />
    );
  }
  return (
    <div
      style={{ width: size, height: size, fontSize: size * 0.36 }}
      className={cn("flex shrink-0 items-center justify-center rounded-xl bg-[#0A2540] font-bold text-teal-300", className)}
    >
      {initials}
    </div>
  );
}