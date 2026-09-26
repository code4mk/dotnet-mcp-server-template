import { AlertTriangle, X } from "lucide-react";

/**
 * A failure of something the view did itself (e.g. loading page 2). What's already on screen is still valid, so this
 * is a dismissible banner, not an error state that replaces the view.
 */
export function Notice({ message, onDismiss }: { message: string; onDismiss: () => void }) {
  return (
    <div
      role="alert"
      className="flex items-start gap-2.5 rounded-xl border border-border-warning bg-background-warning p-3 text-sm text-text-warning"
    >
      <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
      <p className="min-w-0 flex-1">{message}</p>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Dismiss"
        className="shrink-0 cursor-pointer rounded-md p-0.5 transition hover:bg-black/5 focus-visible:outline-2 focus-visible:outline-ring-primary"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  );
}
