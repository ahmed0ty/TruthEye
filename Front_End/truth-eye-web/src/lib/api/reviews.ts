import { api } from "./client";
import type { Paged, ReviewDetail, ReviewRow } from "@/types/admin";

export const reviewApi = {
  list: (q: Record<string, string | number | boolean | undefined>) =>
    api<Paged<ReviewRow>>("/api/SessionReview", { query: q }),
  get: (id: number) => api<ReviewDetail>(`/api/SessionReview/${id}`),
  decide: (id: number, d: { decision: "Cleared" | "Confirmed"; note?: string }) =>
    api<ReviewDetail>(`/api/SessionReview/${id}/decision`, { method: "POST", body: d }),
};