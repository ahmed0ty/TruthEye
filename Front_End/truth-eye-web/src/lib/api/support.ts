import { api } from "./client";
import type { SupportTicket } from "@/types/support";

export const supportApi = {
  all: (status?: string) => api<SupportTicket[]>("/api/SupportTicket", { query: { status } }),
  update: (id: number, d: { status: string; resolution?: string }) =>
    api<SupportTicket>(`/api/SupportTicket/${id}`, { method: "PUT", body: d }),
};