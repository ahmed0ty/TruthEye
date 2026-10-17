export interface Ticket {
  id: number;
  ticketNumber: string;
  category: string;
  priority: string;
  status: string;
  subject: string;
  description: string;
  resolution?: string | null;
  userName?: string | null;
  assignedToName?: string | null;
  createdAt?: string | null;
}