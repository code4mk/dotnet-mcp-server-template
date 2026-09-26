using DotnetMcpTemplate.Core.Auth.Oidc;
using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DotnetMcpTemplate.IntegrationTests.Core.Auth;

/// <summary>ITokenClaimsEnricher shapes the server token at sign-in and on every refresh, but can't touch protected claims.</summary>
[Collection(ServerCollection.Name)]
public sealed class TokenClaimsEnricherTests(TokenClaimsEnricherTests.Factory factory) : IClassFixture<TokenClaimsEnricherTests.Factory>
{
    [Fact]
    public async Task Enricher_claims_are_in_the_token_and_protected_claims_are_kept()
    {
        var signIn = await OidcSignIn.SignInAsync(factory);
        var token = new JsonWebToken(signIn.AccessToken);

        Assert.Equal(["admin"], token.Claims.Where(c => c.Type == "app_roles").Select(c => c.Value));   // mapped from the IdP
        Assert.Equal("acme", token.GetClaim("tenant").Value);                                            // looked up
        Assert.Equal(FakeIdentityProvider.Subject, token.Subject);                                       // not "someone-else"
        Assert.Equal("mcp:tools projects:write", token.GetClaim("scope").Value);                         // not "admin:all"
        Assert.Equal(signIn.ClientId, token.GetClaim("client_id").Value);

        // Runs again on refresh: values stay current without signing in again.
        var before = EnrichCounter.Calls;
        var refreshed = await OidcSignIn.TokenAsync(signIn.Browser, OidcSignIn.Refresh(signIn.RefreshToken, signIn.ClientId));
        Assert.Equal(before + 1, EnrichCounter.Calls);
        Assert.Equal("acme", new JsonWebToken(refreshed.GetProperty("access_token").GetString()).GetClaim("tenant").Value);

        // And the token still works for MCP calls.
        await using var mcp = await factory.ConnectAsync(signIn.AccessToken);
        Assert.NotEqual(true, (await mcp.CallToolAsync("whoami")).IsError);
    }

    public sealed class Factory : OidcProxyServerFactory
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            base.ConfigureTestServices(services);
            services.AddScoped<ITokenClaimsEnricher, SampleEnricher>();
        }
    }

    private static class EnrichCounter
    {
        private static int _calls;

        public static int Calls => Volatile.Read(ref _calls);

        public static void Increment() => Interlocked.Increment(ref _calls);
    }

    private sealed class SampleEnricher : ITokenClaimsEnricher
    {
        public ValueTask EnrichAsync(TokenClaimsContext context, CancellationToken cancellationToken)
        {
            EnrichCounter.Increment();
            context.Set("app_roles", context.IdpValues("roles").ToArray());
            context.Set("tenant", "acme");

            // Attempts to change protected claims are undone.
            context.Set("sub", "someone-else");
            context.Set("scope", "admin:all");
            return ValueTask.CompletedTask;
        }
    }
}
