import { useCallback, useEffect, useRef, useState } from "react";

import { useCallTool, useToolOutput } from "@/shared/lib/mcpApp";
import { messageOf } from "@/shared/lib/toolResult";

import { projectPageSchema, type OwnerProjectCount, type ProjectPage, type ProjectsSummary } from "../schema";

// The model runs show_projects_dashboard; the view drills down on its own: selecting an owner calls list_projects
// through the host, without another model turn.

const PAGE_SIZE = 5;

export type OwnerProjects = {
  owner: OwnerProjectCount;
  result: ProjectPage | null;
  /** Page being fetched; null when idle. */
  loadingPage: number | null;
};

export type ProjectsDashboardView = {
  summary: ProjectsSummary;
  selected: OwnerProjects | null;
  /** A drill-down call failed; the summary on screen is still valid. */
  notice: string | null;
  selectOwner: (owner: OwnerProjectCount) => void;
  goToPage: (page: number) => void;
  clearSelection: () => void;
  dismissNotice: () => void;
};

export function useProjectsDashboard(): ProjectsDashboardView {
  const callTool = useCallTool();
  const summary = useToolOutput<ProjectsSummary>();

  const [selected, setSelected] = useState<OwnerProjects | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // A new push means the model ran the tool again: it wins over the drill-down on screen.
  const lastSummary = useRef(summary);
  useEffect(() => {
    if (lastSummary.current === summary) return;
    lastSummary.current = summary;
    setSelected(null);
    setNotice(null);
  }, [summary]);

  // Ignore responses for an owner that is no longer selected.
  const requestId = useRef(0);

  const load = useCallback(
    async (owner: OwnerProjectCount, page: number) => {
      const id = ++requestId.current;
      setNotice(null);
      setSelected((current) => ({
        owner,
        result: current?.owner.ownerUserId === owner.ownerUserId ? current.result : null,
        loadingPage: page,
      }));

      try {
        const result = await callTool("list_projects", { ownerUserId: owner.ownerUserId, page, pageSize: PAGE_SIZE }, projectPageSchema);
        if (id === requestId.current) setSelected({ owner, result, loadingPage: null });
      } catch (error) {
        if (id !== requestId.current) return;
        setNotice(messageOf(error, "Could not load the projects. Please try again."));
        setSelected((current) => (current ? { ...current, loadingPage: null } : null));
      }
    },
    [callTool],
  );

  return {
    summary,
    selected,
    notice,
    selectOwner: useCallback((owner) => void load(owner, 1), [load]),
    goToPage: useCallback((page) => selected && void load(selected.owner, page), [load, selected]),
    clearSelection: useCallback(() => {
      requestId.current++;
      setSelected(null);
    }, []),
    dismissNotice: useCallback(() => setNotice(null), []),
  };
}
