"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Camera, Loader2, ShieldAlert, Users } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { adminApi } from "@/lib/api/admin";
import { fmtDateTime } from "@/lib/format";
import type { AiConfig } from "@/types/admin";
import type { LucideIcon } from "lucide-react";

interface SettingDef {
  name: string;
  title: string;
  desc: string;
  unit: string;
  fallback: number;
  min: number;
  max: number;
  icon: LucideIcon;
}

// Same names, defaults and limits as the backend (SettingKeys / SettingsService)
const SETTINGS: SettingDef[] = [
  {
    name: "NoFaceSeconds",
    title: "No face detected",
    desc: "How many seconds a student can be out of the camera before a violation is recorded.",
    unit: "seconds",
    fallback: 3,
    min: 2,
    max: 30,
    icon: Camera,
  },
  {
    name: "MultipleFacesSeconds",
    title: "More than one face",
    desc: "How many seconds an extra face can stay in the camera before a violation is recorded.",
    unit: "seconds",
    fallback: 2,
    min: 2,
    max: 30,
    icon: Users,
  },
  {
    name: "MaxViolationsBeforeFlag",
    title: "Violations before flagging",
    desc: "After this many recorded violations in one session, proctors get a notification. Set 0 to disable.",
    unit: "violations",
    fallback: 8,
    min: 0,
    max: 1000,
    icon: ShieldAlert,
  },
];

function SettingCard({ def, current }: { def: SettingDef; current?: AiConfig }) {
  const qc = useQueryClient();
  const saved = current?.value != null && current.value !== "" ? Number(current.value) : def.fallback;
  const [value, setValue] = useState(String(saved));
  const [error, setError] = useState("");
  const Icon = def.icon;

  const save = useMutation({
    mutationFn: (n: number) =>
      adminApi.saveAiConfig({ parameterName: def.name, value: String(n), threshold: current?.threshold ?? 0 }),
    onSuccess: () => {
      toast.success(`${def.title} saved. It applies within about 30 seconds.`);
      qc.invalidateQueries({ queryKey: ["ai-config"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const parsed = Number(value);
  const valid = value.trim() !== "" && Number.isInteger(parsed) && parsed >= def.min && parsed <= def.max;
  const changed = valid && parsed !== saved;

  const onSave = () => {
    if (!valid) return setError(`Enter a whole number from ${def.min} to ${def.max}.`);
    setError("");
    save.mutate(parsed);
  };

  return (
    <article className="flex flex-col rounded-2xl border border-slate-200 bg-white p-6">
      <div className="mb-4 flex h-11 w-11 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
        <Icon size={21} />
      </div>
      <h3 className="font-bold text-[#0A2540]">{def.title}</h3>
      <p className="mt-1 text-sm text-slate-500">{def.desc}</p>

      <div className="mt-5 flex items-center gap-3">
        <input
          type="number"
          min={def.min}
          max={def.max}
          step={1}
          value={value}
          onChange={(e) => {
            setValue(e.target.value);
            setError("");
          }}
          aria-label={def.title}
          className="w-28 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
        />
        <span className="text-sm text-slate-500">{def.unit}</span>
      </div>
      {error && <p className="mt-2 text-xs font-medium text-red-600">{error}</p>}

      <p className="mt-3 text-xs text-slate-400">
        {current?.value != null && current.value !== ""
          ? `Current: ${saved} ${def.unit} · updated ${fmtDateTime(current.updatedAt ?? current.createdAt)}`
          : `Using the built-in default (${def.fallback} ${def.unit})`}
      </p>

      <div className="mt-auto flex items-center justify-between gap-3 pt-5">
        <span className="text-xs text-slate-400">
          Allowed: {def.min} to {def.max}
        </span>
        <button
          onClick={onSave}
          disabled={!changed || save.isPending}
          className="inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-700 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40"
        >
          {save.isPending && <Loader2 size={15} className="animate-spin" />}
          Save
        </button>
      </div>
    </article>
  );
}

export default function AiConfigPage() {
  const { data, isLoading, isError, error, refetch } = useQuery({ queryKey: ["ai-config"], queryFn: adminApi.aiConfigs });
  const byName = new Map((data ?? []).map((c) => [c.parameterName, c]));

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI Settings"
        subtitle="Tune how strictly the proctoring system reacts. Changes apply within about 30 seconds."
      />

      {isLoading ? (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-64 animate-pulse rounded-2xl bg-slate-100" />
          ))}
        </div>
      ) : isError ? (
        <ErrorState title="Couldn’t load the settings" message={(error as Error).message} onRetry={() => refetch()} />
      ) : (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {SETTINGS.map((def) => (
            // key includes the saved value, so a card resets to the stored number after a refresh
            <SettingCard key={`${def.name}:${byName.get(def.name)?.value ?? ""}`} def={def} current={byName.get(def.name)} />
          ))}
        </div>
      )}
    </div>
  );
}