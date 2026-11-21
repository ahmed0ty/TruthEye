import { Lock } from "lucide-react";

export function ChatClosed({ until }: { until?: string | null }) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center px-6 text-center">
      <div className="mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-amber-50">
        <Lock size={30} className="text-amber-600" />
      </div>
      <h2 className="text-lg font-bold text-slate-800">Messages are closed during exams</h2>
      <p className="mt-1 max-w-sm text-sm text-slate-500">
        Chat is unavailable while an exam is running in your faculty.
        {until && <> It reopens around {new Date(until).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}.</>}
      </p>
    </div>
  );
}