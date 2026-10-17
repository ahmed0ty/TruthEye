import { Pencil, Trash2 } from "lucide-react";

export function RowActions({
  onEdit,
  onDelete,
  extra,
}: {
  onEdit?: () => void;
  onDelete?: () => void;
  extra?: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-end gap-1">
      {extra}
      {onEdit && (
        <button onClick={onEdit} className="rounded-lg p-2 text-slate-400 transition hover:bg-teal-50 hover:text-teal-700" aria-label="Edit">
          <Pencil size={17} />
        </button>
      )}
      {onDelete && (
        <button onClick={onDelete} className="rounded-lg p-2 text-slate-400 transition hover:bg-red-50 hover:text-red-600" aria-label="Delete">
          <Trash2 size={17} />
        </button>
      )}
    </div>
  );
}

export function AddButton({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className="inline-flex items-center gap-2 rounded-xl bg-teal-700 hover:bg-teal-800 px-5 py-2.5 text-sm font-bold text-white shadow-lg shadow-teal-700/30 transition hover:shadow-xl active:scale-[0.98]"
    >
      <span className="text-lg leading-none">+</span>
      {children}
    </button>
  );
}