import {
  BarChart3, BookOpen, Building2, ClipboardList, FileText, GraduationCap, History,
  LayoutDashboard, LifeBuoy, MessageSquare, ScanEye, Settings, ShieldCheck, Stethoscope,
  Ticket, Users, type LucideIcon,
} from "lucide-react";
import type { Role } from "@/types/api";

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;
  soon?: boolean;
}

export const NAV: Record<Role, NavItem[]> = {
  SuperAdmin: [
    { label: "Home", href: "/admin", icon: LayoutDashboard },
    { label: "Universities", href: "/admin/universities", icon: Building2 },
    { label: "Faculties", href: "/admin/faculties", icon: GraduationCap },
    { label: "Departments", href: "/admin/departments", icon: BookOpen },
    { label: "Faculty Admins", href: "/admin/admins", icon: ShieldCheck },
    { label: "Support Team", href: "/admin/supports", icon: LifeBuoy },
    { label: "AI Settings", href: "/admin/ai-config", icon: Settings },
  ],
  Admin: [
    { label: "Dashboard", href: "/admin", icon: LayoutDashboard },
    { label: "Students", href: "/admin/students", icon: Users },
    { label: "Doctors", href: "/admin/doctors", icon: Stethoscope },
    { label: "Proctors", href: "/admin/proctors", icon: ScanEye },
    { label: "Departments", href: "/admin/departments", icon: Building2 },
    { label: "Courses", href: "/admin/courses", icon: BookOpen },
    { label: "Reports", href: "/admin/reports", icon: BarChart3, soon: true },
    { label: "Messages", href: "/chat", icon: MessageSquare },
  ],
  Doctor: [
    { label: "Home", href: "/doctor", icon: LayoutDashboard },
    { label: "My Exams", href: "/doctor/exams", icon: ClipboardList },
    { label: "Results", href: "/doctor/results", icon: BarChart3 },
    { label: "Messages", href: "/chat", icon: MessageSquare },
    { label: "Support", href: "/doctor/support", icon: LifeBuoy },
  ],
  Student: [
    { label: "Dashboard", href: "/student", icon: LayoutDashboard },
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
    { label: "Home", href: "/support", icon: LayoutDashboard },
    { label: "Messages", href: "/chat", icon: MessageSquare },
    { label: "Tickets", href: "/support/tickets", icon: Ticket },
  ],
};