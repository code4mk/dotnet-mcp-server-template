# ADR-0004: MCP Apps UI architecture

| Status | Accepted |
| --- | --- |

## Decision

MCP App views live in `ui/`: React + Vite + Tailwind on `@modelcontextprotocol/ext-apps` 2.x.

- **One self-contained HTML file per entry**, built into `src/DotnetMcpTemplate/ui_dist/` and served as
  `ui://<entry>`. Entries are discovered from `ui/src/entries/`; any single entry or all can be built.
- **One folder per app** (`ui/src/apps/<entry>/`) with a thin entry bootstrap, and one shared host connection
  (`McpAppProvider`: `useToolOutput`, `useCallTool`, `useMcpApp`).
- **Tool outputs are validated at runtime** with zod schemas that mirror the C# records (`schema.ts`); the
  TypeScript types are inferred from them.
- **A sandbox** (`sandbox.ts`: sample output + sample tool answers) makes every view work in `pnpm dev` with no
  server, through the same code path as in a host. It is left out of production bundles.
- **Built bundles are committed.** The server runs without Node; CI rebuilds (reproducibly) and fails on a diff.

## Why

- Hosts load one document into a sandboxed iframe: a single file per view is the format they accept.
- Views call tools themselves (paging, drill-down) through the host with the user's token, so the same auth and
  scope rules apply as for the model, and the model isn't needed for every click.
- C# and TypeScript types drift silently; runtime validation turns drift into a readable error in the view.
- Designing against sample data without a server, host or login is the fastest loop for UI work.

## Consequences

- Each bundle carries its own React and MCP client (~450 kB, ~140 kB gzipped). Fine for a handful of views; keep
  dependencies lean as views grow.
- A C# response change needs the matching `schema.ts` change and a rebuilt bundle in the same commit.
- Tools a view calls should return structured content (`UseStructuredContent = true`).
