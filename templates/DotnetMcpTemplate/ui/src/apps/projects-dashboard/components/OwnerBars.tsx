import { ChevronRight } from "lucide-react";

import type { OwnerProjectCount } from "../schema";

/** Projects per owner as bars. Each row is a button: selecting it loads that owner's projects. */
export function OwnerBars({
  owners,
  selectedId,
  onSelect,
}: {
  owners: OwnerProjectCount[];
  selectedId: number | null;
  onSelect: (owner: OwnerProjectCount) => void;
}) {
  const max = Math.max(1, ...owners.map((o) => o.projects));

  return (
    <section className="rounded-xl border border-border-primary p-4">
      <h2 className="mb-3 text-sm font-semibold">Projects per owner</h2>
      <ul className="space-y-1">
        {owners.map((owner) => {
          const selected = owner.ownerUserId === selectedId;
          return (
            <li key={owner.ownerUserId}>
              <button
                type="button"
                onClick={() => onSelect(owner)}
                aria-pressed={selected}
                className={`group grid w-full cursor-pointer grid-cols-[minmax(0,10rem)_1fr_2.5rem_1rem] items-center gap-3 rounded-lg px-2 py-1.5 text-left text-sm transition hover:bg-background-tertiary focus-visible:outline-2 focus-visible:outline-ring-primary ${
                  selected ? "bg-background-info" : ""
                }`}
              >
                <span className="truncate">{owner.ownerName}</span>
                <span className="h-2.5 overflow-hidden rounded-full bg-background-tertiary">
                  <span
                    className={`block h-full rounded-full transition-[width] ${selected ? "bg-accent" : "bg-accent/60 group-hover:bg-accent/80"}`}
                    style={{ width: `${(owner.projects / max) * 100}%` }}
                  />
                </span>
                <span className="text-right tabular-nums text-text-secondary">{owner.projects}</span>
                <ChevronRight className="size-4 text-text-tertiary" aria-hidden />
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
