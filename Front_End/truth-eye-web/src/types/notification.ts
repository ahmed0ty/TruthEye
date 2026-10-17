export interface AppNotification {
  id: number;
  message: string;
  type?: string | null;
  isRead: boolean;
  createdAt?: string | null;
  timestamp?: string | null;
}