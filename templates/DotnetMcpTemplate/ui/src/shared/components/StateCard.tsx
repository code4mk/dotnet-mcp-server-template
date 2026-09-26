import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

/** Full-view empty or error state: icon, headline, explanation. */
export function StateCard({
  icon: Icon,
  title,
  children,
  tone = "neutral",
}: {
  icon: LucideIcon;
  title: string;
  children: ReactNode;
  tone?: "neutral" | "danger";
}) {
  const danger = tone === "danger";
  return (
    <div
      role={danger ? "alert" : undefined}
      className={`mx-auto flex max-w-md flex-col items-center gap-2 rounded-2xl border p-8 text-center ${
        danger ? "border-border-danger bg-background-danger text-text-danger" : "border-border-primary bg-background-secondary"
      }`}
    >
      <Icon className={`size-7 ${danger ? "" : "text-text-tertiary"}`} aria-hidden />
      <h2 className="text-base font-semibold">{title}</h2>
      <div className={`text-sm ${danger ? "" : "text-text-secondary"}`}>{children}</div>
    </div>
  );
}
