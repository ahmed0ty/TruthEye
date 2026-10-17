export type Severity = "high" | "medium" | "low";

export const EVENT_META: Record<string, { label: string; weight: number; severity: Severity }> = {
  MultipleFaces: { label: "Multiple faces", weight: 3, severity: "high" },
  NoFace: { label: "No face detected", weight: 3, severity: "high" },
  PhoneDetected: { label: "Phone detected", weight: 3, severity: "high" },
  TabSwitch: { label: "Switched tab", weight: 2, severity: "medium" },
  FullscreenExit: { label: "Left fullscreen", weight: 2, severity: "medium" },
  WindowBlur: { label: "Window lost focus", weight: 1, severity: "low" },
  CopyPaste: { label: "Copy / paste", weight: 1, severity: "low" },
  ContextMenu: { label: "Right-click", weight: 1, severity: "low" },
  ConnectionLost: { label: "Connection lost", weight: 2, severity: "medium" },
};

export const metaOf = (type: string) =>
  EVENT_META[type] ?? { label: type, weight: 1, severity: "low" as Severity };

export const SEVERITY_STYLE: Record<Severity, string> = {
  high: "bg-red-50 text-red-700",
  medium: "bg-amber-50 text-amber-700",
  low: "bg-slate-100 text-slate-600",
};

export type Risk = "high" | "medium" | "low";

export function riskOf(logs: { eventType: string }[]): { level: Risk; score: number } {
  const score = logs.reduce((a, l) => a + metaOf(l.eventType).weight, 0);
  return { score, level: score >= 8 ? "high" : score >= 3 ? "medium" : "low" };
}

export const RISK_STYLE: Record<Risk, { badge: string; ring: string; label: string }> = {
  high: { badge: "bg-red-600 text-white", ring: "border-red-300", label: "High risk" },
  medium: { badge: "bg-amber-500 text-white", ring: "border-amber-300", label: "Medium risk" },
  low: { badge: "bg-emerald-600 text-white", ring: "border-slate-200", label: "Clean" },
};