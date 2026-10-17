"use client";

import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Camera, Loader2, Trash2 } from "lucide-react";
import { Avatar } from "@/components/ui/avatar";
import { removeAvatar, resizeSquare, uploadAvatar } from "@/lib/api/profile";
import { mediaUrl } from "@/lib/media";

export function AvatarUploader({ name, image }: { name: string; image?: string | null }) {
  const qc = useQueryClient();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);

  const refresh = () => qc.invalidateQueries({ queryKey: ["profile"] });

  const onPick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (!/^image\/(jpeg|png|webp)$/.test(file.type)) return toast.error("Choose a JPG, PNG or WebP image");

    setBusy(true);
    try {
      await uploadAvatar(await resizeSquare(file));
      toast.success("Profile photo updated");
      refresh();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setBusy(false);
    }
  };

  const onRemove = async () => {
    setBusy(true);
    try {
      await removeAvatar();
      toast.success("Profile photo removed");
      refresh();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Couldn't remove the photo");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex items-center gap-4">
      <div className="relative">
        <Avatar name={name} src={image} size={72} className="rounded-2xl" />
        {busy && (
          <span className="absolute inset-0 flex items-center justify-center rounded-2xl bg-black/50 text-white">
            <Loader2 size={22} className="animate-spin" />
          </span>
        )}
      </div>

      <div className="flex flex-col gap-2">
        <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={onPick} />
        <button
          onClick={() => input.current?.click()}
          disabled={busy}
          className="inline-flex items-center gap-1.5 rounded-lg bg-teal-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
        >
          <Camera size={14} /> {mediaUrl(image) ? "Change photo" : "Upload photo"}
        </button>
        {mediaUrl(image) && (
          <button
            onClick={onRemove}
            disabled={busy}
            className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 transition hover:text-red-600 disabled:opacity-60"
          >
            <Trash2 size={14} /> Remove
          </button>
        )}
      </div>
    </div>
  );
}