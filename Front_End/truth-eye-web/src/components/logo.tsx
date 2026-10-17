import { ShieldCheck } from "lucide-react";
import { cn } from "@/lib/utils";

export function Logo({ light = false, className }: { light?: boolean; className?: string }) {
  return (
    <div className={cn("flex items-center gap-3", className)}>
      <div
        className={cn(
          "flex h-11 w-11 items-center justify-center rounded-xl",
          light
            ? "bg-teal-400/15 text-teal-300 ring-1 ring-teal-300/30"
            : "bg-[#0A2540] text-teal-300"
        )}
      >
        <ShieldCheck size={24} />
      </div>
      <span
        className={cn(
          "text-xl font-bold tracking-tight",
          light ? "text-white" : "text-[#0A2540]"
        )}
      >
        TruthEye
      </span>
    </div>
  );
}