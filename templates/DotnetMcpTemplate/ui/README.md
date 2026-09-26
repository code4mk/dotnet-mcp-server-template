# MCP Apps UI

React + Vite workspace for MCP App views. Each entry builds to **one self-contained HTML file** in
`../src/DotnetMcpTemplate/ui_dist/<entry>.html`, which the server exposes as the resource `ui://<entry>`.

```text
ui/src/<entry>/<entry>.html   → entry page (loads main.tsx)
ui/src/<entry>/main.tsx       → React app
```

## Build

```bash
pnpm install
pnpm run build                         # builds the default entry (projects-dashboard)
ENTRY=other-view pnpm run build        # another entry
pnpm run watch                         # rebuild on save (use with dotnet watch)
```

Under `dotnet run` / `dotnet watch` the server reads bundles straight from `src/DotnetMcpTemplate/ui_dist/`, so a
rebuilt bundle is served on the next `resources/read` without restarting.

The repository ships a plain-HTML placeholder `ui_dist/projects-dashboard.html`, so everything works before
this workspace is built. The Dockerfile builds the UI automatically when `ui/pnpm-lock.yaml` exists.

## Adding a view

1. Create `ui/src/<entry>/<entry>.html` and `main.tsx` (use `@modelcontextprotocol/ext-apps` to receive tool
   results and call tools from the view).
2. Add a resource method in `src/DotnetMcpTemplate/Capabilities/Resources/UiResources.cs`:
   `[McpServerResource(UriTemplate = "ui://<entry>", Name = "...", MimeType = McpApps.HtmlMimeType)]` returning `bundles.Read("<entry>")`.
3. Link the tool: `[McpAppUi(ResourceUri = "ui://<entry>")]`, ideally with `UseStructuredContent = true`.

See `docs/development/mcp-apps.md`.
