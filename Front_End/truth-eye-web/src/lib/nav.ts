import {
  BarChart3, BookOpen, Building2, ClipboardList, FileText, GraduationCap, History,
  LayoutDashboard, LifeBuoy, MessageSquare, ScanEye, ScrollText, Settings, ShieldCheck,
  Presentation, Ticket, UserCog, Users, type LucideIcon,
} from "lucide-react";
import type { Role } from "@/types/api";

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;
  soon?: boolean;
}

const facultyCore: NavItem[] = [
  { label: "Home", href: "/admin", icon: LayoutDashboard },
  { label: "Students", href: "/admin/students", icon: Users },
  { label: "Doctors", href: "/admin/doctors", icon: Presentation },
  { label: "Proctors", href: "/admin/proctors", icon: ScanEye },
  { label: "Departments", href: "/admin/departments", icon: Building2 },
  { label: "Courses", href: "/admin/courses", icon: BookOpen },
];
const staffItem: NavItem = { label: "Staff", href: "/admin/staff", icon: UserCog };
const logItem: NavItem = { label: "Activity Log", href: "/admin/activity", icon: ScrollText };
const messages: NavItem = { label: "Messages", href: "/chat", icon: MessageSquare };

export const NAV: Record<Role, NavItem[]> = {
  SuperAdmin: [
    { label: "Home", href: "/admin", icon: LayoutDashboard },
    { label: "Universities", href: "/admin/universities", icon: Building2 },
    { label: "Faculties", href: "/admin/faculties", icon: GraduationCap },
    { label: "Departments", href: "/admin/departments", icon: BookOpen },
    { label: "Deans", href: "/admin/admins", icon: ShieldCheck },
    { label: "Support Team", href: "/admin/supports", icon: LifeBuoy },
    logItem,
    { label: "AI Settings", href: "/admin/ai-config", icon: Settings },
  ],
  Admin: [...facultyCore, staffItem, logItem, messages],
  FacultySecretary: [...facultyCore, staffItem, logItem, messages],
  TechnicalStaff: [...facultyCore, staffItem, messages],
  Doctor: [
    { label: "Home", href: "/doctor", icon: LayoutDashboard },
    { label: "My Exams", href: "/doctor/exams", icon: ClipboardList },
    { label: "Results", href: "/doctor/results", icon: BarChart3 },
    { label: "Messages", href: "/chat", icon: MessageSquare },
    { label: "Support", href: "/doctor/support", icon: LifeBuoy },
  ],
  Student: [
    { label: "Home", href: "/student", icon: LayoutDashboard },
    { label: "Available Exams", href: "/student/exams", icon: ClipboardList },
    { label: "My History", href: "/student/history", icon: History },
    { label: "Reports", href: "/student/reports", icon: FileText },
    { label: "Messages", href: "/chat", icon: MessageSquare },
    { label: "Support", href: "/student/support", icon: LifeBuoy },
  ],
  Proctor: [
    { label: "Home", href: "/proctor", icon: LayoutDashboard },
    { label: "Live Monitoring", href: "/proctor/live", icon: ScanEye },
    { label: "Alerts", href: "/proctor/alerts", icon: ShieldCheck },
    { label: "Messages", href: "/chat", icon: MessageSquare },
  ],
  Support: [
    { label: "Home", href: "/support", icon: LifeBuoy },
    { label: "Tickets", href: "/support/tickets", icon: Ticket },
    { label: "Messages", href: "/chat", icon: MessageSquare },
  ],
};