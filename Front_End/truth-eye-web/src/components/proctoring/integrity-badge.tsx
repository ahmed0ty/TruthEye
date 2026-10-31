import { ShieldAlert, ShieldCheck } from "lucide-react";
import { isFlagged } from "@/lib/proctoring/events";
import { cn } from "@/lib/utils";

export function IntegrityBadge({ score, count }: { score: number; count: number }) {
  const flagged = isFlagged(score);
  const events = `${count} ${count === 1 ? "event" : "events"}`;

  return (
    <span
      title={`${events} recorded · risk score ${score}`}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-bold ring-1",
        flagged ? "bg-red-50 text-red-700 ring-red-200" : "bg-emerald-50 text-emerald-700 ring-emerald-200"
      )}
    >
      {flagged ? <ShieldAlert size={13} /> : <ShieldCheck size={13} />}
      {flagged ? `Flagged · ${events}` : count > 0 ? `Clean · ${count} minor` : "Clean"}
    </span>
  );
}