import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import App from "@/apps/projects-dashboard/App";
import { sandbox } from "@/apps/projects-dashboard/sandbox";
import { projectsSummarySchema } from "@/apps/projects-dashboard/schema";
import { McpAppProvider } from "@/shared/lib/mcpApp";
import "@/styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <McpAppProvider
      name="projects-dashboard"
      tool="show_projects_dashboard"
      schema={projectsSummarySchema}
      sandbox={import.meta.env.DEV ? sandbox : undefined}
    >
      <App />
    </McpAppProvider>
  </StrictMode>,
);
