import { api } from "./client";
import type { AppNotification } from "@/types/notification";

export const notificationsApi = {
  list: () => api<AppNotification[]>("/api/Notification"),
  markRead: (id: number) => api(`/api/Notification/${id}/read`, { method: "POST" }),
};