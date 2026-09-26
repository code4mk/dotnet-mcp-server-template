import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";

// Starter view. Replace the message listener with @modelcontextprotocol/ext-apps (App / useApp) when you add your
// React code: it handles the host handshake, tool results, theming and calling tools from the view.

type Summary = { totalProjects: number; owners: number; byOwner: { ownerUserId: number; ownerName: string; projects: number }[] };

function Dashboard() {
  const [summary, setSummary] = useState<Summary | null>(null);

  useEffect(() => {
    const onMessage = (event: MessageEvent) => {
      const message = event.data;
      if (message?.method === "ui/notifications/tool-result") {
        setSummary(message.params?.structuredContent ?? null);
      }
    };
    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, []);

  if (!summary) return <p>Waiting for data from the host…</p>;
  const max = Math.max(...summary.byOwner.map((o) => o.projects), 1);

  return (
    <main style={{ fontFamily: "system-ui, sans-serif", padding: 16 }}>
      <h1 style={{ fontSize: 16 }}>{summary.totalProjects} projects · {summary.owners} owners</h1>
      {summary.byOwner.map((o) => (
        <div key={o.ownerUserId} style={{ display: "grid", gridTemplateColumns: "160px 1fr 32px", gap: 8, margin: "6px 0" }}>
          <span>{o.ownerName}</span>
          <div style={{ height: 10, borderRadius: 5, background: "#1f6feb", width: `${(o.projects / max) * 100}%` }} />
          <span>{o.projects}</span>
        </div>
      ))}
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<StrictMode><Dashboard /></StrictMode>);
