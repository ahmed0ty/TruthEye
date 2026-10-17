"use client";

import { ErrorState } from "@/components/ui/states";

export default function GlobalError({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <div className="w-full max-w-lg">
        <ErrorState title="This page crashed" message="An unexpected error happened. You can try again." onRetry={reset} />
      </div>
    </div>
  );
}