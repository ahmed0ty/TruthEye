"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { z } from "zod";
import { ArrowLeft, CheckCircle2, Download, FileUp, Loader2, XCircle } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { adminApi } from "@/lib/api/admin";
import { parseCsv } from "@/lib/csv";
import { downloadCsv } from "@/lib/export-csv";
import { cn } from "@/lib/utils";

const FIELDS: Record<string, string[]> = {
  firstName: ["firstname", "first"],
  lastName: ["lastname", "last", "surname"],
  email: ["email", "mail"],
  nationalId: ["nationalid", "nid"],
  phoneNumber: ["phonenumber", "phone", "mobile"],
  password: ["password", "pass"],
  department: ["department", "dept"],
  year: ["year", "level"],
};
const REQUIRED = ["firstName", "lastName", "email", "nationalId", "phoneNumber", "department", "year"];

const schema = z.object({
  firstName: z.string().trim().min(2, "first name is too short").max(50),
  lastName: z.string().trim().min(2, "last name is too short").max(50),
  email: z.string().trim().email("invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "national ID must be 14 digits (format the column as Text in Excel)"),
  phoneNumber: z.string().trim().min(8, "phone is too short"),
  password: z.string().min(6, "password needs 6+ characters"),
});

const ALPHA = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
function genPassword() {
  const b = new Uint32Array(10);
  crypto.getRandomValues(b);
  return Array.from(b, (n) => ALPHA[n % ALPHA.length]).join("") + "!";
}

interface Payload {
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber: string;
  password: string;
  departmentId: number;
  year: number;
}
interface Row {
  line: number;
  name: string;
  email: string;
  nationalId: string;
  department: string;
  year: string;
  generated: boolean;
  payload?: Payload;
  errors: string[];
}
type Result = { ok: boolean; error?: string };

const TEMPLATE_HEADERS = ["firstName", "lastName", "email", "nationalId", "phoneNumber", "password", "department", "year"];

export default function ImportStudentsPage() {
  const qc = useQueryClient();
  const input = useRef<HTMLInputElement>(null);
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });

  const [rows, setRows] = useState<Row[]>([]);
  const [fileError, setFileError] = useState("");
  const [phase, setPhase] = useState<"idle" | "preview" | "running" | "done">("idle");
  const [results, setResults] = useState<Record<number, Result>>({});

  const clean = (v: string | undefined) => (v ?? "").trim().replace(/^'/, "");

  const build = (text: string) => {
    setFileError("");
    const table = parseCsv(text);
    if (table.length < 2) return setFileError("The file has no data rows.");

    const norm = (h: string) => h.toLowerCase().replace(/[^a-z]/g, "");
    const header = table[0].map(norm);
    const idx: Record<string, number> = {};
    for (const [field, aliases] of Object.entries(FIELDS)) idx[field] = header.findIndex((h) => aliases.includes(h));

    const missing = REQUIRED.filter((f) => idx[f] === -1);
    if (missing.length) return setFileError(`Missing columns: ${missing.join(", ")}`);

    const deptMap = new Map((departments.data ?? []).map((d) => [d.name.trim().toLowerCase(), d.id]));
    const seenEmail = new Set<string>();
    const seenNid = new Set<string>();

    const parsed: Row[] = table.slice(1).map((cells, i) => {
      const get = (f: string) => (idx[f] >= 0 ? clean(cells[idx[f]]) : "");
      const errors: string[] = [];

      const passwordGiven = get("password");
      const password = passwordGiven || genPassword();
      const data = {
        firstName: get("firstName"),
        lastName: get("lastName"),
        email: get("email"),
        nationalId: get("nationalId"),
        phoneNumber: get("phoneNumber"),
        password,
      };

      const check = schema.safeParse(data);
      if (!check.success) check.error.issues.forEach((x) => errors.push(x.message));

      const departmentId = deptMap.get(get("department").toLowerCase());
      if (!departmentId) errors.push(`unknown department "${get("department")}"`);

      const year = Number(get("year"));
      if (!Number.isInteger(year) || year < 1 || year > 7) errors.push("year must be 1 to 7");

      const e = data.email.toLowerCase();
      if (e && seenEmail.has(e)) errors.push("duplicate email in the file");
      if (data.nationalId && seenNid.has(data.nationalId)) errors.push("duplicate national ID in the file");
      seenEmail.add(e);
      seenNid.add(data.nationalId);

      return {
        line: i + 2,
        name: `${data.firstName} ${data.lastName}`.trim(),
        email: data.email,
        nationalId: data.nationalId,
        department: get("department"),
        year: get("year"),
        generated: !passwordGiven,
        errors,
        payload: errors.length ? undefined : { ...data, departmentId: departmentId!, year },
      };
    });

    setRows(parsed);
    setResults({});
    setPhase("preview");
  };

  const onPick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (!departments.data) return toast.error("Departments are still loading, try again in a moment.");
    if (!/\.csv$/i.test(file.name)) return setFileError("Please choose a .csv file (in Excel: Save As → CSV UTF-8).");
    if (file.size > 2_000_000) return setFileError("The file is too large (2MB max).");
    build(await file.text());
  };

  const valid = rows.filter((r) => r.payload);
  const invalid = rows.length - valid.length;
  const done = Object.keys(results).length;

  const run = async () => {
    setPhase("running");
    let next = 0;
    const worker = async () => {
      while (next < valid.length) {
        const r = valid[next++];
        let res: Result;
        try {
          await adminApi.createStudent({ ...r.payload });
          res = { ok: true };
        } catch (err) {
          res = { ok: false, error: err instanceof Error ? err.message : "Failed" };
        }
        setResults((prev) => ({ ...prev, [r.line]: res }));
      }
    };
    await Promise.all([worker(), worker(), worker()]);
    setPhase("done");
    qc.invalidateQueries({ queryKey: ["students"] });
    qc.invalidateQueries({ queryKey: ["admin-stat"] });
  };

  const okCount = Object.values(results).filter((r) => r.ok).length;
  const failCount = Object.values(results).filter((r) => !r.ok).length;

  const downloadReport = () =>
    downloadCsv(
      "students_import_report.csv",
      ["Line", "Name", "Email", "National ID", "Status", "Error", "Generated password"],
      rows.map((r) => {
        const res = results[r.line];
        const status = r.errors.length ? "Invalid" : res?.ok ? "Created" : res ? "Failed" : "Skipped";
        return [
          r.line, r.name, r.email, r.nationalId, status,
          r.errors.join("; ") || res?.error || "",
          res?.ok && r.generated ? r.payload!.password : "",
        ];
      })
    );

  const reset = () => {
    setRows([]);
    setResults({});
    setPhase("idle");
    setFileError("");
  };

  return (
    <div className="space-y-6">
      <Link href="/admin/students" className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-[#0A2540]">
        <ArrowLeft size={16} /> Back to students
      </Link>

      <PageHeader
        title="Import students"
        subtitle="Upload a CSV file to add many students at once."
        action={
          <button
            onClick={() =>
              downloadCsv("students_template.csv", TEMPLATE_HEADERS, [
                ["Ahmed", "Ali", "ahmed@example.com", "29901011234567", "01000000000", "", "Computer Science", 1],
              ])
            }
            className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            <Download size={17} /> Download template
          </button>
        }
      />

      {phase === "idle" && (
        <section className="animate-fade-up rounded-2xl border-2 border-dashed border-slate-300 bg-white p-10 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-teal-50 text-teal-600">
            <FileUp size={30} />
          </div>
          <p className="font-bold text-[#0A2540]">Choose a CSV file</p>
          <p className="mx-auto mt-1 max-w-md text-sm text-slate-500">
            Columns: firstName, lastName, email, nationalId, phoneNumber, department, year, and an optional password. If the password is empty, a random one is generated.
          </p>
          <input ref={input} type="file" accept=".csv,text/csv" className="hidden" onChange={onPick} />
          <button
            onClick={() => input.current?.click()}
            disabled={departments.isLoading}
            className="mt-5 inline-flex items-center gap-2 rounded-xl bg-teal-600 px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
          >
            {departments.isLoading ? <Loader2 size={16} className="animate-spin" /> : <FileUp size={16} />} Select file
          </button>
          <ul className="mx-auto mt-6 max-w-lg space-y-1 text-start text-xs text-slate-400">
            <li>• In Excel: File → Save As → <b>CSV UTF-8</b>.</li>
            <li>• Format the National ID column as <b>Text</b> before saving, or Excel will turn it into 2.99E+13.</li>
            <li>• The department name must match one of your departments exactly.</li>
          </ul>
        </section>
      )}

      {fileError && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm font-medium text-red-600">{fileError}</p>}

      {phase !== "idle" && (
        <section className="animate-fade-up overflow-hidden rounded-2xl border border-slate-200 bg-white">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-4">
            <div className="flex flex-wrap gap-2 text-xs font-semibold">
              <span className="rounded-full bg-slate-100 px-3 py-1 text-slate-600">{rows.length} rows</span>
              <span className="rounded-full bg-emerald-50 px-3 py-1 text-emerald-700">{valid.length} valid</span>
              {invalid > 0 && <span className="rounded-full bg-red-50 px-3 py-1 text-red-700">{invalid} with errors</span>}
              {phase === "done" && <span className="rounded-full bg-teal-50 px-3 py-1 text-teal-700">{okCount} created · {failCount} failed</span>}
            </div>

            <div className="flex gap-2">
              {phase === "preview" && (
                <>
                  <button onClick={reset} className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-50">Choose another</button>
                  <button
                    onClick={run}
                    disabled={valid.length === 0}
                    className="rounded-xl bg-teal-600 px-5 py-2 text-sm font-semibold text-white transition hover:bg-teal-700 disabled:opacity-50"
                  >
                    Import {valid.length} students
                  </button>
                </>
              )}
              {phase === "done" && (
                <>
                  <button onClick={downloadReport} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
                    <Download size={16} /> Report
                  </button>
                  <button onClick={reset} className="rounded-xl bg-teal-600 px-5 py-2 text-sm font-semibold text-white hover:bg-teal-700">Import another file</button>
                </>
              )}
            </div>
          </div>

          {phase === "running" && (
            <div className="border-b border-slate-100 px-4 py-3">
              <div className="mb-1.5 flex justify-between text-xs text-slate-500">
                <span>Importing...</span>
                <span>{done} / {valid.length}</span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                <div className="h-full rounded-full bg-teal-500 transition-all" style={{ width: `${valid.length ? (done / valid.length) * 100 : 0}%` }} />
              </div>
            </div>
          )}

          {phase === "done" && okCount > 0 && rows.some((r) => r.generated && results[r.line]?.ok) && (
            <p className="border-b border-amber-100 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              Some passwords were generated. Download the report now and share them securely. They aren’t shown again.
            </p>
          )}

          <div className="max-h-[28rem] overflow-auto">
            <table className="w-full min-w-[680px] text-sm">
              <thead className="sticky top-0 bg-slate-50 text-xs uppercase tracking-wide text-slate-400">
                <tr>
                  {["Line", "Student", "Email", "Department", "Year", "Status"].map((h) => (
                    <th key={h} className="px-4 py-2.5 text-start font-semibold">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => {
                  const res = results[r.line];
                  return (
                    <tr key={r.line} className="border-t border-slate-100">
                      <td className="px-4 py-2.5 text-slate-400">{r.line}</td>
                      <td className="px-4 py-2.5 font-semibold text-[#0A2540]">{r.name || "—"}</td>
                      <td className="px-4 py-2.5 text-slate-500" dir="ltr">{r.email}</td>
                      <td className="px-4 py-2.5 text-slate-600">{r.department}</td>
                      <td className="px-4 py-2.5 text-slate-600">{r.year}</td>
                      <td className="px-4 py-2.5">
                        {r.errors.length ? (
                          <span className="inline-flex items-start gap-1.5 text-xs font-medium text-red-600">
                            <XCircle size={14} className="mt-0.5 shrink-0" /> {r.errors.join("; ")}
                          </span>
                        ) : res ? (
                          res.ok ? (
                            <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-600"><CheckCircle2 size={14} /> Created</span>
                          ) : (
                            <span className="inline-flex items-start gap-1.5 text-xs font-medium text-red-600"><XCircle size={14} className="mt-0.5 shrink-0" /> {res.error}</span>
                          )
                        ) : phase === "running" ? (
                          <Loader2 size={14} className="animate-spin text-slate-300" />
                        ) : (
                          <span className={cn("text-xs font-medium text-slate-400")}>Ready</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
}