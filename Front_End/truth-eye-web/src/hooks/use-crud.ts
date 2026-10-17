"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useDelete<T extends { id: number }>(
  queryKey: unknown[],
  fn: (id: number) => Promise<unknown>,
  successMsg: string
) {
  const qc = useQueryClient();
  const [target, setTarget] = useState<T | null>(null);

  const mutation = useMutation({
    mutationFn: (id: number) => fn(id),
    onSuccess: () => {
      toast.success(successMsg);
      qc.invalidateQueries({ queryKey });
      setTarget(null);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return { target, setTarget, mutation };
}