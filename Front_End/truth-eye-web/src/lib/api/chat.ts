import { api } from "./client";
import type { ChatMsg, Contact, RecentConv } from "@/types/chat";

export const chatApi = {
  contacts: () => api<Contact[]>("/api/Contacts"),
  recent: () => api<RecentConv[]>("/api/Chat/recent"),
  conversation: (userId: number) => api<ChatMsg[]>(`/api/Chat/conversation/${userId}`, { query: { take: 100 } }),
  markRead: (userId: number) => api(`/api/Chat/mark-read/${userId}`, { method: "PUT" }),
};