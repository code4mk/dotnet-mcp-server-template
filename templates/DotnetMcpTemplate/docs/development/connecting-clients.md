# Connecting clients

URL: `APP_URL` + `MCP_PATH`, e.g. `https://mcp.example.com/mcp` (Streamable HTTP). Clients discover the login
automatically from the `401` response.

- **Claude** (claude.ai / Desktop): Settings → Connectors → Add custom connector → paste the URL.
- **VS Code**: `.vscode/mcp.json`: `{ "servers": { "my-server": { "type": "http", "url": "https://mcp.example.com/mcp" } } }`
- **Cursor**: `~/.cursor/mcp.json`: `{ "mcpServers": { "my-server": { "url": "https://mcp.example.com/mcp" } } }`
- **MCP Inspector**: `npx @modelcontextprotocol/inspector` → Streamable HTTP → URL.

Any MCP client can connect: there is no allow-list to maintain. Registration refuses only redirect URIs that are
unsafe for OAuth (`http` outside localhost, `javascript:`, `data:`, `file:`, fragments); the error names the URI.
Each new client is shown on the consent page before the user signs in.

## Behind a reverse proxy

Set `APP_URL` to the public HTTPS URL. The server listens on `APP_PORT` (plain HTTP); terminate TLS at the proxy.
Register `${APP_URL}/oauth/callback` at your IdP.
