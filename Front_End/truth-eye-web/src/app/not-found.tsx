import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-3 p-6 text-center">
      <p className="text-6xl font-extrabold text-teal-600">404</p>
      <p className="text-slate-500">This page doesn&apos;t exist.</p>
      <Link href="/" className="mt-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-teal-700">
        Go home
      </Link>
    </div>
  );
}