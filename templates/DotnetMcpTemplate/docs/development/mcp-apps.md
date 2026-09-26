# MCP Apps

MCP Apps let a tool show an interactive UI in the conversation. Three parts:

1. **Bundle**: `ui/` builds one self-contained HTML per entry into `src/DotnetMcpTemplate/ui_dist/<entry>.html`
   (`pnpm run build` for all, `pnpm run build <entry>` for one).
2. **Resource**: `Capabilities/Resources/UiResources.cs` serves it as `ui://<entry>` with MIME type `text/html;profile=mcp-app`
   (`Core/Mcp/Apps/UiBundles.cs` reads it from `ui_dist/` in the content root, cached outside dev;
   `MCP_UI_DIST_DIR` overrides the folder).
3. **Tool link**: `[McpAppUi(ResourceUri = "ui://<entry>")]` on the tool. The SDK's `WithMcpApps()` adds the
   `_meta.ui.resourceUri` and advertises the extension. Return structured content (`UseStructuredContent = true`).

Clients without MCP Apps support still get the tool's normal result. A missing bundle returns:
`UI bundle not found at .../ui_dist/<entry>.html. Run "pnpm install && pnpm run build" in ui/.`

Resource `_meta.ui` options (CSP domains, borders) go in `[McpMeta("ui", "...json...")]`.

## The view

`ui/` is React + Vite + Tailwind with `@modelcontextprotocol/ext-apps` 2.x. One folder per app in `ui/src/apps/`,
a thin bootstrap per entry in `ui/src/entries/`, and a shared host connection (`useToolOutput<T>()`, `useCallTool()`,
`useMcpApp()`). Tool outputs are validated with zod schemas that mirror the C# records. The sample `projects-dashboard` renders the `show_projects_dashboard` result and drills down by calling
`list_projects` from the view, paged, without another model turn. Structure, patterns and "adding a view":
[ui/README.md](../../ui/README.md).

- Tools the view calls should use `UseStructuredContent = true`, so the view gets typed data, not text.
- View calls go through the host with the user's token: auth and scope policies apply as usual.
- A failed, cancelled or mismatched tool result shows its message in the view (never an endless spinner).
- `pnpm dev` previews every view with sample data (`sandbox.ts`), no server needed.
- `pnpm run build [entry ...]` builds all entries or only the named ones. CI fails if `ui_dist/` is out of date.
