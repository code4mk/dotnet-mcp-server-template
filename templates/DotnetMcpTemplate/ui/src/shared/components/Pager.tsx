import { ChevronLeft, ChevronRight, Loader2 } from "lucide-react";

/** Previous / next with "page x of y". `loadingPage` is the page being fetched, if any. */
export function Pager({
  page,
  totalPages,
  loadingPage,
  onPage,
}: {
  page: number;
  totalPages: number;
  loadingPage: number | null;
  onPage: (page: number) => void;
}) {
  if (totalPages <= 1) return null;
  const busy = loadingPage !== null;
  const button =
    "inline-flex cursor-pointer items-center gap-1 rounded-lg border border-border-primary px-2.5 py-1 text-xs font-medium " +
    "transition hover:bg-background-tertiary disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-ring-primary";

  return (
    <nav className="flex items-center justify-between gap-3 pt-2" aria-label="Pagination">
      <button type="button" className={button} disabled={busy || page <= 1} onClick={() => onPage(page - 1)}>
        {loadingPage === page - 1 ? <Loader2 className="size-3.5 animate-spin" /> : <ChevronLeft className="size-3.5" />}
        Previous
      </button>
      <span className="text-xs text-text-secondary tabular-nums">
        Page {page} of {totalPages}
      </span>
      <button type="button" className={button} disabled={busy || page >= totalPages} onClick={() => onPage(page + 1)}>
        Next
        {loadingPage === page + 1 ? <Loader2 className="size-3.5 animate-spin" /> : <ChevronRight className="size-3.5" />}
      </button>
    </nav>
  );
}
