using Microsoft.AspNetCore.WebUtilities;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>HTTP endpoints of the OAuth proxy. The logic lives in <see cref="OAuthProxy"/>.</summary>
public static class OAuthProxyEndpoints
{
    private const string TransactionCookie = "mcp_oauth_txn";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        // Metadata: RFC 8414 path plus the OIDC path some clients try first.
        endpoints.MapGet("/.well-known/oauth-authorization-server", (OAuthProxy proxy) => Results.Json(proxy.Metadata()))
            .RequireCors(OAuthCors.PolicyName).AllowAnonymous().ExcludeFromDescription();
        endpoints.MapGet("/.well-known/openid-configuration", (OAuthProxy proxy) => Results.Json(proxy.Metadata()))
            .RequireCors(OAuthCors.PolicyName).AllowAnonymous().ExcludeFromDescription();

        endpoints.MapPost(OAuthProxy.RegisterPath, RegisterAsync)
            .RequireCors(OAuthCors.PolicyName).AllowAnonymous().DisableAntiforgery().ExcludeFromDescription();
        endpoints.MapGet(OAuthProxy.AuthorizePath, AuthorizeAsync)
            .AllowAnonymous().ExcludeFromDescription();
        endpoints.MapPost(OAuthProxy.ConsentPath, ConsentAsync)
            .AllowAnonymous().DisableAntiforgery().ExcludeFromDescription();
        endpoints.MapGet(OAuthProxy.CallbackPath, CallbackAsync)
            .AllowAnonymous().ExcludeFromDescription();
        endpoints.MapPost(OAuthProxy.TokenPath, TokenAsync)
            .RequireCors(OAuthCors.PolicyName).AllowAnonymous().DisableAntiforgery().ExcludeFromDescription();
    }

    private static async Task<IResult> RegisterAsync(HttpRequest request, OAuthProxy proxy, CancellationToken cancellationToken)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<RegistrationRequest>(cancellationToken)
                ?? throw new OAuthException("invalid_client_metadata", "A JSON body is required.");
            return Results.Json(await proxy.RegisterAsync(body, cancellationToken), statusCode: StatusCodes.Status201Created);
        }
        catch (OAuthException exception)
        {
            return OAuthError(exception);
        }
        catch (System.Text.Json.JsonException)
        {
            return OAuthError(new OAuthException("invalid_client_metadata", "The body is not valid JSON."));
        }
    }

    private static async Task<IResult> AuthorizeAsync(HttpContext context, OAuthProxy proxy, CancellationToken cancellationToken)
    {
        AuthorizationTransaction transaction;
        try
        {
            transaction = await proxy.BeginAsync(context.Request.Query, cancellationToken);
        }
        catch (RedirectableOAuthException exception)
        {
            return Results.Redirect(ErrorRedirect(exception));
        }
        catch (OAuthException exception)
        {
            return ConsentPage.Error(exception.Message);
        }

        if (proxy.HasRememberedConsent(context.Request, transaction.ClientId))
        {
            return Results.Redirect(await proxy.StartUpstreamLoginAsync(transaction, cancellationToken));
        }

        var client = await proxy.GetClientAsync(transaction.ClientId, cancellationToken);
        context.Response.Cookies.Append(TransactionCookie, transaction.Id, CookieOptions(context, TimeSpan.FromMinutes(10), OAuthProxy.ConsentPath));
        return ConsentPage.Render(proxy.ServerName, client?.ClientName ?? "MCP client", transaction, OAuthProxy.ConsentPath);
    }

    private static async Task<IResult> ConsentAsync(HttpContext context, OAuthProxy proxy, CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var transactionId = form["transaction"].ToString();

        // CSRF: the form must come from the page we rendered in this browser.
        var transaction = await proxy.GetTransactionAsync(transactionId, cancellationToken);
        if (transaction is null
            || context.Request.Cookies[TransactionCookie] != transactionId
            || !AuthCrypto.FixedTimeEquals(form["csrf"].ToString(), transaction.CsrfToken))
        {
            return ConsentPage.Error("This consent request expired or is invalid. Start again from your MCP client.");
        }

        context.Response.Cookies.Delete(TransactionCookie, CookieOptions(context, TimeSpan.Zero, OAuthProxy.ConsentPath));

        if (form["decision"] != "allow")
        {
            return Results.Redirect(proxy.DenyRedirect(transaction));
        }

        if (proxy.ConsentRememberDays > 0)
        {
            context.Response.Cookies.Append(
                proxy.ConsentCookieName(transaction.ClientId),
                proxy.ConsentCookieValue(transaction.ClientId),
                CookieOptions(context, TimeSpan.FromDays(proxy.ConsentRememberDays), OAuthProxy.AuthorizePath));
        }

        return Results.Redirect(await proxy.StartUpstreamLoginAsync(transaction, cancellationToken));
    }

    private static async Task<IResult> CallbackAsync(HttpContext context, OAuthProxy proxy, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Redirect(await proxy.CompleteUpstreamLoginAsync(context.Request.Query, cancellationToken));
        }
        catch (OAuthException exception)
        {
            return ConsentPage.Error(exception.Message);
        }
    }

    private static async Task<IResult> TokenAsync(HttpContext context, OAuthProxy proxy, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!context.Request.HasFormContentType)
        {
            return OAuthError(new OAuthException("invalid_request", "Use application/x-www-form-urlencoded."));
        }

        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            return Results.Json(await proxy.TokenAsync(form, context.Request.Headers.Authorization, cancellationToken));
        }
        catch (OAuthException exception)
        {
            return OAuthError(exception);
        }
    }

    private static IResult OAuthError(OAuthException exception) =>
        Results.Json(new Dictionary<string, string>
        {
            ["error"] = exception.Error,
            ["error_description"] = exception.Message,
        }, statusCode: exception.StatusCode);

    private static string ErrorRedirect(RedirectableOAuthException exception) =>
        QueryHelpers.AddQueryString(exception.RedirectUri, new Dictionary<string, string?>
        {
            ["error"] = exception.Error,
            ["error_description"] = exception.Message,
            ["state"] = exception.State,
        }.Where(p => p.Value is not null));

    private static CookieOptions CookieOptions(HttpContext context, TimeSpan lifetime, string path) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = path,
        MaxAge = lifetime,
        IsEssential = true,
    };
}
