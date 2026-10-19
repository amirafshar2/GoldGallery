"use client";

import { Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";

/** Small helpers shared by admin CRUD screens. */
export function RowActions({ onEdit, onDelete }: { onEdit?: () => void; onDelete?: () => void }) {
  return (
    <div className="flex justify-end gap-1">
      {onEdit && <Button size="icon" variant="ghost" onClick={onEdit} aria-label="ویرایش"><Pencil /></Button>}
      {onDelete && <Button size="icon" variant="ghost" onClick={() => { if (window.confirm("حذف شود؟")) onDelete(); }} aria-label="حذف"><Trash2 className="text-destructive" /></Button>}
    </div>
  );
}

export function Notice({ text, ok }: { text: string; ok?: boolean }) {
  if (!text) return null;
  return <p className={`rounded-xl border p-3 text-sm ${ok ? "border-emerald-600/40 text-emerald-700" : "border-destructive/40 text-destructive"}`} data-testid="notice">{text}</p>;
}
