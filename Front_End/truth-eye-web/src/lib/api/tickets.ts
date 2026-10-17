import { api } from "./client";
import type { Ticket } from "@/types/ticket";

export const ticketsApi = {
  mine: () => api<Ticket[]>("/api/SupportTicket/my-tickets"),
  all: () => api<Ticket[]>("/api/SupportTicket"),
  assigned: () => api<Ticket[]>("/api/SupportTicket/assigned"),
  create: (d: { category: string; priority: string; subject: string; description: string }) =>
    api("/api/SupportTicket", { method: "POST", body: d }),
  update: (id: number, d: { status: string; resolution: string }) =>
    api(`/api/SupportTicket/${id}`, { method: "PUT", body: d }),
  assign: (ticketId: number, supportId: number) =>
    api(`/api/SupportTicket/${ticketId}/assign/${supportId}`, { method: "PUT" }),
};