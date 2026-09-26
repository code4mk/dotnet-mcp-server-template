using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Auth.Oidc;
using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DotnetMcpTemplate.IntegrationTests.Core.Auth;

/// <summary>
/// ISignInHandler runs once per sign-in with the ID token and userinfo kept apart; what it stores in SessionData reaches
/// every token (sign-in and refresh) through enrichers; it can refuse the sign-in.
/// </summary>
[Collection(ServerCollection.Name)]
public sealed class SignInHandlerTests(SignInHandlerTests.Factory factory) : IClassFixture<SignInHandlerTests.Factory>
{
    [Fact]
    public async Task Handler_sees_id_token_and_userinfo_apart_and_its_data_reaches_every_token()
    {
        Backend.Reset();
        var signIn = await OidcSignIn.SignInAsync(factory);

        // No ambiguity: each source on its own, plus the combined view.
        var seen = Backend.LastContext!;
        Assert.Equal("Alice", seen.IdToken["name"]!.ToString());                // ID token
        Assert.Equal("Alice Example", seen.UserInfo!["name"]!.ToString());      // userinfo response
        Assert.Equal("Alice Example", seen.IdentityProviderClaims["name"]!.ToString());
        Assert.False(string.IsNullOrEmpty(seen.IdentityProviderAccessToken));   // to call your API as the user

        // Data the handler got from the backend is in the token.
        var token = new JsonWebToken(signIn.AccessToken);
        Assert.Equal(["projects.read", "projects.write"], token.Claims.Where(c => c.Type == "permissions").Select(c => c.Value));
        Assert.Equal("u-42", token.GetClaim("internal_user_id").Value);

        // Refresh: the backend is NOT called again, the enricher still has the session data.
        var refreshed = await OidcSignIn.TokenAsync(signIn.Browser, OidcSignIn.Refresh(signIn.RefreshToken, signIn.ClientId));
        Assert.Equal(1, Backend.Calls);
        Assert.Equal([TokenIssue.SignIn, TokenIssue.Refresh], Backend.EnrichReasons);
        Assert.Equal("u-42", new JsonWebToken(refreshed.GetProperty("access_token").GetString()).GetClaim("internal_user_id").Value);
    }

    [Fact]
    public async Task Handler_can_refuse_the_sign_in()
    {
        Backend.Reset();
        Backend.Refuse = "Your account has no MCP access. Ask an administrator.";

        var (_, _, _, redirect) = await OidcSignIn.UntilCallbackAsync(factory);
        var result = QueryHelpers.ParseQuery(redirect.Query);

        Assert.Equal("access_denied", result["error"].ToString());
        Assert.Equal(Backend.Refuse, result["error_description"].ToString());
        Assert.False(result.ContainsKey("code"));
        Backend.Reset();
    }

    [Fact]
    public async Task A_failing_handler_fails_the_sign_in_safely()
    {
        Backend.Reset();
        Backend.Fail = true;

        var (_, _, _, redirect) = await OidcSignIn.UntilCallbackAsync(factory);
        var result = QueryHelpers.ParseQuery(redirect.Query);

        Assert.Equal("server_error", result["error"].ToString());
        Assert.DoesNotContain("backend down", result["error_description"].ToString());   // no internals leak
        Backend.Reset();
    }

    public sealed class Factory : OidcProxyServerFactory
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            base.ConfigureTestServices(services);
            services.AddScoped<ISignInHandler, SyncUserHandler>();
            services.AddScoped<ITokenClaimsEnricher, PermissionsEnricher>();
        }
    }

    /// <summary>Stands in for your backend API and records what happened.</summary>
    private static class Backend
    {
        public static SignInContext? LastContext { get; set; }

        public static int Calls { get; set; }

        public static List<TokenIssue> EnrichReasons { get; } = [];

        public static string? Refuse { get; set; }

        public static bool Fail { get; set; }

        public static void Reset()
        {
            LastContext = null;
            Calls = 0;
            EnrichReasons.Clear();
            Refuse = null;
            Fail = false;
        }
    }

    /// <summary>What a real handler does: sync the user, fetch permissions, or refuse.</summary>
    private sealed class SyncUserHandler : ISignInHandler
    {
        public ValueTask OnSignInAsync(SignInContext context, CancellationToken cancellationToken)
        {
            Backend.Calls++;
            Backend.LastContext = context;
            if (Backend.Fail)
            {
                throw new HttpRequestException("backend down");
            }

            if (Backend.Refuse is { } reason)
            {
                context.Deny(reason);
                return ValueTask.CompletedTask;
            }

            context.SessionData["internal_user_id"] = "u-42";
            context.SessionData["permissions"] = new JsonArray("projects.read", "projects.write");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PermissionsEnricher : ITokenClaimsEnricher
    {
        public ValueTask EnrichAsync(TokenClaimsContext context, CancellationToken cancellationToken)
        {
            Backend.EnrichReasons.Add(context.Reason);
            context.Set("permissions", context.SessionValues("permissions").ToArray());
            context.Set("internal_user_id", context.SessionData["internal_user_id"]!.ToString());
            return ValueTask.CompletedTask;
        }
    }
}
