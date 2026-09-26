import type { LucideIcon } from "lucide-react";

/** One KPI tile: label, big number, optional hint. */
export function Stat({ icon: Icon, label, value, hint }: { icon: LucideIcon; label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-xl border border-border-primary bg-background-secondary p-4">
      <div className="flex items-center gap-2 text-xs font-medium text-text-secondary">
        <Icon className="size-4 text-text-tertiary" aria-hidden />
        {label}
      </div>
      <div className="mt-1 text-2xl font-semibold tabular-nums">{value}</div>
      {hint && <div className="mt-0.5 text-xs text-text-tertiary">{hint}</div>}
    </div>
  );
}
