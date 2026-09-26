# MCP Apps UI

React + Vite + Tailwind workspace for MCP App views. Each entry builds to **one self-contained HTML file** in
`../src/DotnetMcpTemplate/ui_dist/<entry>.html`, which the server exposes as the resource `ui://<entry>`.

```bash
pnpm install
pnpm dev                              # sandbox at http://localhost:5173: every entry with sample data
pnpm run build                        # typecheck + build all entries into ui_dist/
pnpm run build projects-dashboard     # typecheck + build only these entries (one or more names)
pnpm run watch [entry ...]            # rebuild on save; with `dotnet watch` new bundles are served without a restart
pnpm run entries                      # list entries
```

CI builds every entry and fails if the committed bundles in `ui_dist/` don't match the source (builds are
reproducible), so commit `ui_dist/` together with your UI changes.

## Layout

```text
ui/
├── scripts/build.mjs            Finds entries, runs one `vite build` per entry (single-file bundles need one input)
├── src/
│   ├── entries/                 Thin bootstraps, one pair per MCP App
│   │   ├── index.html             Dev sandbox (not shipped)
│   │   ├── projects-dashboard.html
│   │   └── projects-dashboard.tsx  <McpAppProvider tool schema sandbox><App /></McpAppProvider>
│   ├── apps/<entry>/            One folder per MCP App
│   │   ├── App.tsx                Composition only
│   │   ├── components/            Presentational pieces
│   │   ├── lib/use<Entry>.ts      State + calls back to the server
│   │   ├── schema.ts              zod schemas of the tool outputs (mirror the C# records); types come from them
│   │   └── sandbox.ts             Sample output + sample tool answers for `pnpm dev` (dev builds only)
│   ├── shared/
│   │   ├── lib/mcpApp.tsx         Host connection: useToolOutput<T>(), useCallTool(), useMcpApp(), host theme
│   │   ├── lib/toolResult.ts      Tool result → validated output or a readable error
│   │   ├── lib/schemas.ts         Schemas for shared records (PagedResult)
│   │   └── components/            Stat, Pager, Notice, StateCard
│   └── styles.css               Tailwind + host theme tokens
└── vite.config.ts
```

## How a view works

1. **Tool result in.** The model calls the tool (`show_projects_dashboard`) and the host pushes its result. The
   provider validates `structuredContent` against the app's schema; `useToolOutput<T>()` returns it. The first paint
   waits for it, so sample data never flashes.
2. **Failures in.** If the tool failed (`isError`), was cancelled, or returned data that doesn't match `schema.ts`
   (e.g. a C# property was renamed), the view shows that message instead of a spinner or a broken layout.
3. **Calls out.** The view calls server tools itself: `const callTool = useCallTool();`
   `await callTool("list_projects", { ownerUserId, page }, projectPageSchema)`. The call goes through the host with
   the signed-in user's token, so `[Authorize]` and scope policies apply, and the output is validated too. Return
   structured content from those tools (`UseStructuredContent = true`).
4. **Errors out.** A failed call the view made (page 2, drill-down) is a dismissible `Notice`: what's on screen is
   still valid. When the model runs the tool again, the view resets to the new result.
5. **Theme.** The host's CSS variables (`--color-background-primary`, `--color-text-secondary`, ...) are Tailwind
   tokens in `styles.css`: use `bg-background-primary`, `text-text-secondary`, `border-border-primary`. Dark mode
   follows the host (`data-theme`), or the OS in the sandbox.

**Sandbox.** Outside a host, the provider renders `sandbox.output` and `useCallTool()` answers from
`sandbox.tools`, so hooks have one code path and the whole view (drill-down, paging, loading states) works with no
server. Entries pass `sandbox={import.meta.env.DEV ? sandbox : undefined}`, so sample data is not in production
bundles; opening a built bundle directly shows "Open this view in an MCP host".

The sample `projects-dashboard` shows all of this: KPIs and projects per owner from the tool result; selecting an
owner loads that owner's projects with `list_projects`, paged from the view.

## Adding a view

1. `src/entries/<entry>.html` + `<entry>.tsx` (copy the projects-dashboard pair). Names are kebab-case; a leading
   `_` skips the entry.
2. `src/apps/<entry>/`: `schema.ts` (mirror the C# response record), `sandbox.ts`, `lib/use<Entry>.ts`, `App.tsx`.
3. Add a link to `src/entries/index.html` for the sandbox.
4. Server: a resource in `src/DotnetMcpTemplate/Capabilities/Resources/UiResources.cs`
   (`[McpServerResource(UriTemplate = "ui://<entry>", ..., MimeType = McpApps.HtmlMimeType)]` returning
   `bundles.Read("<entry>")`) and `[McpAppUi(ResourceUri = "ui://<entry>")]` + `UseStructuredContent = true` on the tool.
5. `pnpm run build <entry>` and commit the bundle in `ui_dist/`.

See `docs/development/mcp-apps.md`.
