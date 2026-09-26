import { FolderKanban, Gauge, Users } from "lucide-react";

import { Stat } from "@/shared/components/Stat";

import type { ProjectsSummary } from "../schema";

export function SummaryStats({ summary }: { summary: ProjectsSummary }) {
  const average = summary.owners > 0 ? summary.totalProjects / summary.owners : 0;
  const top = summary.byOwner[0];

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <Stat icon={FolderKanban} label="Projects" value={summary.totalProjects.toLocaleString()} />
      <Stat icon={Users} label="Owners" value={summary.owners.toLocaleString()} />
      <Stat
        icon={Gauge}
        label="Average per owner"
        value={average.toFixed(1)}
        hint={top ? `Most: ${top.ownerName} (${top.projects})` : undefined}
      />
    </div>
  );
}
