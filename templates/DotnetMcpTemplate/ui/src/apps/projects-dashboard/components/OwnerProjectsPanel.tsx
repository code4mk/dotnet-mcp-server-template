import { Loader2, X } from "lucide-react";

import { Pager } from "@/shared/components/Pager";

import type { OwnerProjects } from "../lib/useProjectsDashboard";

/** The selected owner's projects, fetched with list_projects and paged from the view. */
export function OwnerProjectsPanel({
  selected,
  onPage,
  onClose,
}: {
  selected: OwnerProjects;
  onPage: (page: number) => void;
  onClose: () => void;
}) {
  const { owner, result, loadingPage } = selected;

  return (
    <section className="rounded-xl border border-border-primary p-4" aria-busy={loadingPage !== null}>
      <header className="mb-3 flex items-center justify-between gap-2">
        <h2 className="min-w-0 truncate text-sm font-semibold">
          {owner.ownerName}
          <span className="ml-2 font-normal text-text-secondary">{owner.projects} projects</span>
        </h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Close"
          className="cursor-pointer rounded-md p-1 text-text-secondary transition hover:bg-background-tertiary focus-visible:outline-2 focus-visible:outline-ring-primary"
        >
          <X className="size-4" aria-hidden />
        </button>
      </header>

      {!result ? (
        <div className="flex items-center gap-2 py-6 text-sm text-text-secondary" role="status">
          <Loader2 className="size-4 animate-spin" aria-hidden /> Loading projects…
        </div>
      ) : result.items.length === 0 ? (
        <p className="py-6 text-sm text-text-secondary">No projects for this owner.</p>
      ) : (
        <>
          <ul className={`divide-y divide-border-primary transition-opacity ${loadingPage !== null ? "opacity-60" : ""}`}>
            {result.items.map((project) => (
              <li key={project.id} className="flex items-start justify-between gap-3 py-2.5">
                <div className="min-w-0">
                  <div className="truncate text-sm font-medium first-letter:uppercase">{project.name}</div>
                  <div className="line-clamp-1 text-xs text-text-secondary">{project.description}</div>
                </div>
                <StatusBadge status={project.status} />
              </li>
            ))}
          </ul>
          <Pager page={result.page} totalPages={result.totalPages} loadingPage={loadingPage} onPage={onPage} />
        </>
      )}
    </section>
  );
}

function StatusBadge({ status }: { status: string }) {
  const done = status === "done";
  return (
    <span
      className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-medium capitalize ${
        done ? "bg-background-tertiary text-text-secondary" : "bg-background-info text-text-info"
      }`}
    >
      {status}
    </span>
  );
}
