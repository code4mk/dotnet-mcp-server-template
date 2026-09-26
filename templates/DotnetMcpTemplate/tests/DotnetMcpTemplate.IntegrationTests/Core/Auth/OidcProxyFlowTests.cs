using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotnetMcpTemplate.Core.Auth.Oidc;
using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.IntegrationTests.Core.Auth;

/// <summary>The whole login as an MCP client does it: register → authorize → consent → IdP → callback → token → MCP call.</summary>
[Collection(ServerCollection.Name)]
public sealed partial class OidcProxyFlowTests(OidcProxyServerFactory factory) : IClassFixture<OidcProxyServerFactory>
{
    private const string RedirectUri = "http://localhost:9999/callback";

    private HttpClient Browser() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    [Fact]
    public async Task Metadata_points_clients_to_this_server()
    {
        var metadata = await Browser().GetFromJsonAsync<JsonElement>("/.well-known/oauth-authorization-server");

        Assert.Equal("http://localhost", metadata.GetProperty("issuer").GetString());
        Assert.Equal("http://localhost/oauth/register", metadata.GetProperty("registration_endpoint").GetString());
        Assert.Contains("S256", metadata.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(e => e.GetString()));
    }

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback")]
    [InlineData("https://some-new-client.example.com/oauth/callback")]
    [InlineData("cursor://anysphere.cursor-retrieval/oauth/callback")]
    public async Task Any_mcp_client_can_register(string redirectUri)
    {
        var response = await Browser().PostAsJsonAsync("/oauth/register", new { redirect_uris = new[] { redirectUri }, token_endpoint_auth_method = "none" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("http://evil.example.com/cb")]
    [InlineData("javascript:alert(1)")]
    public async Task Registration_rejects_unsafe_redirect_uris(string redirectUri)
    {
        var response = await Browser().PostAsJsonAsync("/oauth/register", new { redirect_uris = new[] { redirectUri }, token_endpoint_auth_method = "none" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_redirect_uri", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Full_login_issues_a_token_with_merged_claims()
    {
        var browser = Browser();

        // 1. Dynamic client registration
        var registration = await (await browser.PostAsJsonAsync("/oauth/register", new
        {
            redirect_uris = new[] { RedirectUri },
            token_endpoint_auth_method = "none",
            client_name = "Test MCP client",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var clientId = registration.GetProperty("client_id").GetString()!;

        // 2. Authorize (PKCE) → consent page
        var verifier = AuthCrypto.RandomToken(48);
        var authorizeUrl = QueryHelpers.AddQueryString("/oauth/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["state"] = "client-state",
            ["code_challenge"] = AuthCrypto.PkceChallenge(verifier),
            ["code_challenge_method"] = "S256",
            ["scope"] = "mcp:tools projects:write",
            ["resource"] = "http://localhost/mcp",
        });
        var consentPage = await (await browser.GetAsync(authorizeUrl)).Content.ReadAsStringAsync();
        Assert.Contains("Test MCP client", consentPage);

        // 3. Allow → redirected to the IdP
        var consent = await browser.PostAsync("/oauth/consent", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["transaction"] = Hidden(consentPage, "transaction"),
            ["csrf"] = Hidden(consentPage, "csrf"),
            ["decision"] = "allow",
        }));
        Assert.Equal(HttpStatusCode.Redirect, consent.StatusCode);
        var idpAuthorize = QueryHelpers.ParseQuery(consent.Headers.Location!.Query);
        Assert.Equal(FakeIdentityProvider.ClientId, idpAuthorize["client_id"].ToString());
        Assert.Equal("S256", idpAuthorize["code_challenge_method"].ToString());

        // 4. The IdP redirects back with a code
        OidcProxyServerFactory.Idp.Nonce = idpAuthorize["nonce"];
        var callback = await browser.GetAsync($"/oauth/callback?code=upstream-code&state={Uri.EscapeDataString(idpAuthorize["state"]!)}");
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.StartsWith("Basic ", OidcProxyServerFactory.Idp.LastTokenRequestAuthorization);   // client_secret_basic

        var clientRedirect = callback.Headers.Location!;
        Assert.StartsWith(RedirectUri, clientRedirect.ToString());
        var result = QueryHelpers.ParseQuery(clientRedirect.Query);
        Assert.Equal("client-state", result["state"].ToString());
        Assert.Equal("http://localhost", result["iss"].ToString());

        // 5. Code → tokens (PKCE verified)
        var tokens = await Token(browser, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = result["code"]!,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = verifier,
        });
        var accessToken = tokens.GetProperty("access_token").GetString()!;
        Assert.Equal("mcp:tools projects:write", tokens.GetProperty("scope").GetString());

        // 6. Call MCP with the server token: identity = ID token + userinfo
        await using var mcp = await factory.ConnectAsync(accessToken);
        var whoami = await mcp.CallToolAsync("whoami");
        var text = string.Concat(whoami.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.Contains(FakeIdentityProvider.Subject, text);
        Assert.Contains("Alice Example", text);   // userinfo wins over the ID token's "Alice"
        Assert.Contains("Research", text);        // OIDC_TOKEN_CLAIMS=department
        Assert.Contains("admin", text);           // IdP roles, copied with OIDC_TOKEN_CLAIMS=roles

        // Copied array claims are one claim per value, so policies can use RequireClaim("roles", "admin")
        var roleClaims = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(accessToken).Claims.Where(c => c.Type == "roles");
        Assert.Equal(["admin"], roleClaims.Select(c => c.Value));

        // 7. An expired access token is replaced with the refresh token, no sign-in; the new token works
        factory.Clock.Reset();
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;
        var refreshed = await Token(browser, Refresh(refreshToken, clientId));
        var newRefreshToken = refreshed.GetProperty("refresh_token").GetString()!;
        Assert.NotEqual(refreshToken, newRefreshToken);
        Assert.Equal("mcp:tools projects:write", refreshed.GetProperty("scope").GetString());

        await using (var refreshedMcp = await factory.ConnectAsync(refreshed.GetProperty("access_token").GetString()))
        {
            Assert.NotEqual(true, (await refreshedMcp.CallToolAsync("whoami")).IsError);
        }

        // 8. A retry with the old refresh token within the grace period (concurrent refresh, lost response)
        //    gets the same new tokens instead of signing the user out
        var retried = await Token(browser, Refresh(refreshToken, clientId));
        Assert.Equal(newRefreshToken, retried.GetProperty("refresh_token").GetString());
        Assert.Equal(refreshed.GetProperty("access_token").GetString(), retried.GetProperty("access_token").GetString());

        // 9. After the grace period the old refresh token is rejected; the current one keeps rotating
        factory.Clock.Advance(TimeSpan.FromSeconds(31));
        var reused = await browser.PostAsync("/oauth/token", new FormUrlEncodedContent(Refresh(refreshToken, clientId)));
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);

        var next = await Token(browser, Refresh(newRefreshToken, clientId));
        Assert.NotEqual(newRefreshToken, next.GetProperty("refresh_token").GetString());
        factory.Clock.Reset();
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_is_rejected()
    {
        var browser = Browser();
        var registration = await (await browser.PostAsJsonAsync("/oauth/register", new { redirect_uris = new[] { RedirectUri }, token_endpoint_auth_method = "none" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var response = await browser.PostAsync("/oauth/token", new FormUrlEncodedContent(Refresh("made-up", registration.GetProperty("client_id").GetString()!)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync());
    }

    private static Dictionary<string, string> Refresh(string refreshToken, string clientId) => new()
    {
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken,
        ["client_id"] = clientId,
    };

    [Fact]
    public async Task Wrong_pkce_verifier_is_rejected()
    {
        var response = await Browser().PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = "made-up",
            ["client_id"] = "unknown",
            ["code_verifier"] = "wrong",
        }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);   // unknown client
    }

    private static async Task<JsonElement> Token(HttpClient browser, Dictionary<string, string> form)
    {
        var response = await browser.PostAsync("/oauth/token", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static string Hidden(string html, string name) =>
        WebUtility.HtmlDecode(HiddenInput().Matches(html).First(m => m.Groups[1].Value == name).Groups[2].Value);

    [GeneratedRegex("<input type=\"hidden\" name=\"([^\"]+)\" value=\"([^\"]*)\">")]
    private static partial Regex HiddenInput();
}
