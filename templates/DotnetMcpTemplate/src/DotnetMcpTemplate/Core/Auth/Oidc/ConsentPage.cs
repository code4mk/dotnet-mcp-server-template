using System.Net;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// The only HTML pages: the consent screen shown before a newly connecting MCP client may log in at the IdP, and the
/// error page. Required for OAuth proxies (MCP security best practices, "confused deputy"): without it, a malicious
/// client could reuse an earlier approval of another client. Layout follows the FastMCP OAuth proxy consent screen.
/// </summary>
public static class ConsentPage
{
    private const string ConfusedDeputyUrl =
        "https://modelcontextprotocol.io/specification/2025-06-18/basic/security_best_practices#confused-deputy-problem";

    public static IResult Render(string serverName, string clientName, AuthorizationTransaction transaction, string action)
    {
        var scopes = transaction.Scopes.Count == 0 ? "None (sign-in only)" : string.Join(", ", transaction.Scopes);

        return Page("Application Access Request", $"""
            <main class="container">
              {Logo}
              <h1>Application Access Request</h1>
              <div class="info-box">
                <p>The application <strong>{E(clientName)}</strong> wants to access the MCP server
                <strong>{E(serverName)}</strong>. Please ensure you recognize the callback address below.</p>
              </div>
              <div class="redirect-section">
                <span class="label">Credentials will be sent to:</span>
                <div class="value">{E(transaction.RedirectUri)}</div>
              </div>
              <details>
                <summary>Advanced Details</summary>
                <div class="detail-box">
                  {Row("Application Name", clientName)}
                  {Row("Application ID", transaction.ClientId)}
                  {Row("Redirect URI", transaction.RedirectUri)}
                  {Row("Requested Scopes", scopes)}
                </div>
              </details>
              <form method="post" action="{E(action)}">
                <input type="hidden" name="transaction" value="{E(transaction.Id)}">
                <input type="hidden" name="csrf" value="{E(transaction.CsrfToken)}">
                <div class="button-group">
                  <button type="submit" name="decision" value="allow" class="btn-approve">Allow Access</button>
                  <button type="submit" name="decision" value="deny" class="btn-deny">Deny</button>
                </div>
              </form>
            </main>
            <div class="help-link-container">
              <span class="help-link" tabindex="0">
                Why am I seeing this?
                <span class="tooltip" role="tooltip">
                  This MCP server asks for your consent before a new client can connect. This protects you from
                  <a href="{ConfusedDeputyUrl}" target="_blank" rel="noopener noreferrer" class="tooltip-link">confused
                  deputy attacks</a>, where a malicious client could impersonate you and steal access.
                  Only allow clients you just started connecting yourself.
                </span>
              </span>
            </div>
            """);
    }

    public static IResult Error(string message) =>
        Page("Authorization Error", $"""
            <main class="container">
              {Logo}
              <h1>Authorization Error</h1>
              <div class="info-box error"><p>{E(message)}</p></div>
              <p class="help-text">Close this window and start the connection again from your MCP client.</p>
            </main>
            """, StatusCodes.Status400BadRequest);

    private static string Row(string label, string value) =>
        $"""<div class="detail-row"><div class="detail-label">{E(label)}:</div><div class="detail-value">{E(value)}</div></div>""";

    /// <summary>Inline SVG (a shield with a key hole): no external image, so the strict CSP stays intact.</summary>
    private const string Logo = """
        <svg class="logo" viewBox="0 0 64 64" aria-hidden="true">
          <path d="M32 4 8 13v17c0 15 10.2 26.6 24 30 13.8-3.4 24-15 24-30V13L32 4Z" fill="var(--accent-soft)" stroke="var(--accent)" stroke-width="3" stroke-linejoin="round"/>
          <circle cx="32" cy="28" r="6" fill="var(--accent)"/>
          <path d="M29 32h6l2 12h-10l2-12Z" fill="var(--accent)"/>
        </svg>
        """;

    private const string Styles = """
        :root {
          --bg: #f9fafb; --card: #ffffff; --border: #e5e7eb; --text: #111827; --muted: #6b7280;
          --accent: #0ea5e9; --accent-soft: #e0f2fe; --info-bg: #f0f9ff; --info-border: #bae6fd; --info-text: #374151;
          --warn-bg: #fffbeb; --warn-border: #fcd34d; --subtle: #f9fafb;
          --error-bg: #fef2f2; --error-border: #fecaca; --error-text: #991b1b;
          --approve: #10b981; --deny: #6b7280; --tooltip-bg: #1f2937;
          color-scheme: light dark;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #0b0f14; --card: #111827; --border: #1f2937; --text: #f3f4f6; --muted: #9ca3af;
            --accent: #38bdf8; --accent-soft: #0c4a6e; --info-bg: #0c1a2a; --info-border: #0c4a6e; --info-text: #d1d5db;
            --warn-bg: #1f1a0b; --warn-border: #a16207; --subtle: #0f1620;
            --error-bg: #2a0f12; --error-border: #7f1d1d; --error-text: #fca5a5;
            --approve: #059669; --deny: #4b5563; --tooltip-bg: #374151;
          }
        }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
          min-height: 100vh; display: flex; align-items: center; justify-content: center;
          background: var(--bg); color: var(--text);
        }
        .container {
          background: var(--card); border: 1px solid var(--border); border-radius: 1rem; padding: 3rem 2.5rem;
          box-shadow: 0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -1px rgba(0,0,0,.06);
          text-align: center; max-width: 36rem; width: 100%; margin: 1rem;
        }
        @media (max-width: 640px) { .container { padding: 2rem 1.5rem; margin: .5rem; } }
        .logo { width: 64px; height: 64px; display: block; margin: 0 auto 1.5rem; }
        h1 { font-size: 1.5rem; font-weight: 600; margin-bottom: 1.5rem; }
        .info-box {
          background: var(--info-bg); border: 1px solid var(--info-border); border-radius: .5rem; padding: 1rem;
          margin-bottom: 1.5rem; text-align: left; font-size: .9375rem; line-height: 1.5; color: var(--info-text);
        }
        .info-box strong { color: var(--accent); font-weight: 600; }
        .info-box.error { background: var(--error-bg); border-color: var(--error-border); color: var(--error-text); }
        .redirect-section {
          background: var(--warn-bg); border: 1px solid var(--warn-border); border-radius: .5rem; padding: 1rem;
          margin-bottom: 1.5rem; text-align: left;
        }
        .redirect-section .label { display: block; font-size: .875rem; font-weight: 600; color: var(--muted); margin-bottom: .5rem; }
        .redirect-section .value, .detail-value {
          font-family: 'SF Mono', Monaco, Consolas, 'Courier New', monospace; word-break: break-all; overflow-wrap: break-word;
        }
        .redirect-section .value { font-size: .875rem; }
        details { margin-bottom: 1.5rem; text-align: left; }
        summary {
          cursor: pointer; list-style: none; font-size: .875rem; font-weight: 600; color: var(--muted);
          padding: .5rem; border-radius: .25rem;
        }
        summary::-webkit-details-marker { display: none; }
        summary:hover { background: var(--subtle); }
        summary::before { content: "\25B6"; display: inline-block; margin-right: .5rem; font-size: .75rem; transition: transform .2s; }
        details[open] summary::before { transform: rotate(90deg); }
        .detail-box {
          background: var(--subtle); border: 1px solid var(--border); border-radius: .5rem; padding: 1rem; margin-top: .5rem;
        }
        .detail-row { display: flex; padding: .5rem 0; border-bottom: 1px solid var(--border); }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { min-width: 160px; flex-shrink: 0; padding-right: 1rem; font-size: .875rem; font-weight: 600; color: var(--muted); }
        .detail-value { flex: 1; font-size: .75rem; }
        @media (max-width: 480px) { .detail-row { flex-direction: column; } .detail-label { min-width: 0; margin-bottom: .25rem; } }
        .button-group { display: flex; gap: .75rem; justify-content: center; margin-top: 1.5rem; }
        button {
          min-width: 120px; padding: .75rem 2rem; font: inherit; font-size: .9375rem; font-weight: 500; color: #fff;
          border: none; border-radius: .5rem; cursor: pointer; transition: transform .15s, box-shadow .15s;
        }
        button:hover { transform: translateY(-1px); box-shadow: 0 4px 6px -1px rgba(0,0,0,.1); }
        button:focus-visible, .help-link:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
        .btn-approve { background: var(--approve); }
        .btn-deny { background: var(--deny); }
        .help-text { font-size: .875rem; color: var(--muted); }
        .help-link-container { position: fixed; right: 1.5rem; bottom: 1.5rem; font-size: .875rem; }
        .help-link { position: relative; display: inline-block; color: var(--muted); cursor: help; border-bottom: 1px dotted var(--muted); }
        .help-link:hover, .help-link:focus { color: var(--text); border-bottom-color: var(--text); }
        .tooltip {
          position: absolute; right: 0; bottom: 100%; margin-bottom: .5rem; width: 280px; max-width: calc(100vw - 3rem);
          padding: .75rem 1rem; border-radius: .5rem; background: var(--tooltip-bg); color: #fff; font-size: .8125rem;
          line-height: 1.5; text-align: left; box-shadow: 0 10px 15px -3px rgba(0,0,0,.1);
          opacity: 0; visibility: hidden; transition: opacity .2s, visibility .2s;
        }
        .tooltip::after { content: ''; position: absolute; top: 100%; right: 1rem; border: 6px solid transparent; border-top-color: var(--tooltip-bg); }
        .help-link:hover .tooltip, .help-link:focus .tooltip, .help-link:focus-within .tooltip { opacity: 1; visibility: visible; }
        .tooltip-link { color: #7dd3fc; text-decoration: underline; }
        """;

    private static IResult Page(string title, string body, int status = StatusCodes.Status200OK) =>
        new HtmlResult($$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="referrer" content="no-referrer">
              <title>{{E(title)}}</title>
              <style>{{Styles}}</style>
            </head>
            <body>{{body}}</body>
            </html>
            """, status);

    private static string E(string value) => WebUtility.HtmlEncode(value);

    private sealed class HtmlResult(string html, int status) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = status;
            httpContext.Response.ContentType = "text/html; charset=utf-8";
            httpContext.Response.Headers.XFrameOptions = "DENY";
            httpContext.Response.Headers.ContentSecurityPolicy =
                "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; frame-ancestors 'none'";
            httpContext.Response.Headers.CacheControl = "no-store";
            return httpContext.Response.WriteAsync(html);
        }
    }
}
