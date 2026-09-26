# MCP Apps

MCP Apps let a tool show an interactive UI in the conversation. Three parts:

1. **Bundle**: `ui/` builds one self-contained HTML per entry into `src/DotnetMcpTemplate/ui_dist/<entry>.html`.
2. **Resource**: `Capabilities/Resources/UiResources.cs` serves it as `ui://<entry>` with MIME type `text/html;profile=mcp-app`
   (`Core/Mcp/Apps/UiBundles.cs` reads it from `ui_dist/` in the content root, cached outside dev;
   `MCP_UI_DIST_DIR` overrides the folder).
3. **Tool link**: `[McpAppUi(ResourceUri = "ui://<entry>")]` on the tool. The SDK's `WithMcpApps()` adds the
   `_meta.ui.resourceUri` and advertises the extension. Return structured content (`UseStructuredContent = true`).

Clients without MCP Apps support still get the tool's normal result. A missing bundle returns:
`UI bundle not found at .../ui_dist/<entry>.html. Run "pnpm install && pnpm run build" in ui/.`

Resource `_meta.ui` options (CSP domains, borders) go in `[McpMeta("ui", "...json...")]`.
