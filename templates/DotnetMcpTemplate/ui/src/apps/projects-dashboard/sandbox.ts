import type { Sandbox } from "@/shared/lib/mcpApp";

import type { Project, ProjectPage, ProjectsSummary } from "./schema";

// Dev sandbox (`pnpm dev`) data: what the host would push, and answers for the tools the view calls. Shaped like
// real responses; the counts vary so the bars are easy to read. Not included in production builds.

const output: ProjectsSummary = {
  totalProjects: 58,
  owners: 6,
  byOwner: [
    { ownerUserId: 3, ownerName: "Clementine Bauch", projects: 14 },
    { ownerUserId: 1, ownerName: "Leanne Graham", projects: 12 },
    { ownerUserId: 5, ownerName: "Chelsey Dietrich", projects: 10 },
    { ownerUserId: 2, ownerName: "Ervin Howell", projects: 9 },
    { ownerUserId: 7, ownerName: "Kurtis Weissnat", projects: 8 },
    { ownerUserId: 4, ownerName: "Patricia Lebsack", projects: 5 },
  ],
};

function listProjects(args: Record<string, unknown>): ProjectPage {
  const ownerUserId = Number(args.ownerUserId);
  const page = Number(args.page ?? 1);
  const pageSize = Number(args.pageSize ?? 10);
  const total = output.byOwner.find((o) => o.ownerUserId === ownerUserId)?.projects ?? 0;

  const all = Array.from({ length: total }, (_, i): Project => ({
    id: ownerUserId * 100 + i + 1,
    name: `Sample project ${i + 1}`,
    description: "Sandbox data. Inside an MCP host this list comes from the list_projects tool.",
    ownerUserId,
    priority: "medium",
    status: (i + 1) % 3 === 0 ? "done" : "active",
  }));
  const totalPages = Math.ceil(total / pageSize);

  return {
    items: all.slice((page - 1) * pageSize, page * pageSize),
    page,
    pageSize,
    totalCount: total,
    totalPages,
    hasNextPage: page < totalPages,
  };
}

export const sandbox: Sandbox<ProjectsSummary> = {
  output,
  tools: { list_projects: listProjects },
};
