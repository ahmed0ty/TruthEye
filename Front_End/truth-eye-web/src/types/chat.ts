export interface Contact {
  id: number;
  name: string;
  role: string;
}

export interface ChatMsg {
  id: number;
  messageText: string;
  sentAt: string;
  isRead: boolean;
  senderId: number;
  receiverId: number;
  isMine: boolean;
}

export interface RecentConv {
  userId: number;
  lastMessage: { messageText: string; sentAt: string; senderId: number };
  unreadCount: number;
}

export interface Contact {
  id: number;
  name: string;
  role: string;
  profileImage?: string | null;
}