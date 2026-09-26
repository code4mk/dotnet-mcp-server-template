# Connecting clients

URL: `APP_URL` + `MCP_PATH`, e.g. `https://mcp.example.com/mcp` (Streamable HTTP). Clients discover the login
automatically from the `401` response.

- **Claude** (claude.ai / Desktop): Settings → Connectors → Add custom connector → paste the URL.
  Allow `https://claude.ai/api/mcp/auth_callback` in `AUTH_ALLOWED_REDIRECT_URIS`.
- **VS Code**: `.vscode/mcp.json`: `{ "servers": { "my-server": { "type": "http", "url": "https://mcp.example.com/mcp" } } }`
- **Cursor**: `~/.cursor/mcp.json`: `{ "mcpServers": { "my-server": { "url": "https://mcp.example.com/mcp" } } }`
  (allow Cursor's redirect URI pattern if it uses a custom scheme).
- **MCP Inspector**: `npx @modelcontextprotocol/inspector` → Streamable HTTP → URL.

If a client fails to register, check the server log for the rejected redirect URI and add a pattern to
`AUTH_ALLOWED_REDIRECT_URIS`.

## Behind a reverse proxy

Set `APP_URL` to the public HTTPS URL. The server listens on `APP_PORT` (plain HTTP); terminate TLS at the proxy.
Register `${APP_URL}/oauth/callback` at your IdP.
