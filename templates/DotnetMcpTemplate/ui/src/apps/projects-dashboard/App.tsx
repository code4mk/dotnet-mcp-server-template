import { FolderOpen } from "lucide-react";

import { Notice } from "@/shared/components/Notice";
import { StateCard } from "@/shared/components/StateCard";

import { OwnerBars } from "./components/OwnerBars";
import { OwnerProjectsPanel } from "./components/OwnerProjectsPanel";
import { SummaryStats } from "./components/SummaryStats";
import { useProjectsDashboard } from "./lib/useProjectsDashboard";

// Projects dashboard (MCP App for the show_projects_dashboard tool): KPIs and projects per owner from the tool
// result; select an owner to load their projects with list_projects, straight from the view.
export default function App() {
  const { summary, selected, notice, selectOwner, goToPage, clearSelection, dismissNotice } = useProjectsDashboard();

  if (summary.byOwner.length === 0) {
    return (
      <StateCard icon={FolderOpen} title="No projects yet">
        Projects appear here once owners create them.
      </StateCard>
    );
  }

  return (
    <main className="mx-auto max-w-3xl space-y-4 p-4">
      {notice && <Notice message={notice} onDismiss={dismissNotice} />}
      <SummaryStats summary={summary} />
      <div className={`grid gap-4 ${selected ? "md:grid-cols-2" : ""}`}>
        <OwnerBars owners={summary.byOwner} selectedId={selected?.owner.ownerUserId ?? null} onSelect={selectOwner} />
        {selected && <OwnerProjectsPanel selected={selected} onPage={goToPage} onClose={clearSelection} />}
      </div>
    </main>
  );
}
