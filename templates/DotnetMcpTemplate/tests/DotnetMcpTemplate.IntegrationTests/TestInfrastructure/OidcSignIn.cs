using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotnetMcpTemplate.Core.Auth.Oidc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>Signs in as an MCP client would (register, authorize, consent, IdP, callback, token) without assertions.</summary>
public static partial class OidcSignIn
{
    public const string RedirectUri = "http://localhost:9999/callback";

    public sealed record Result(HttpClient Browser, string ClientId, JsonElement Tokens)
    {
        public string AccessToken => Tokens.GetProperty("access_token").GetString()!;

        public string RefreshToken => Tokens.GetProperty("refresh_token").GetString()!;
    }

    public static HttpClient Browser(OidcProxyServerFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public static async Task<Result> SignInAsync(OidcProxyServerFactory factory)
    {
        var browser = Browser(factory);
        var registration = await (await browser.PostAsJsonAsync("/oauth/register", new
        {
            redirect_uris = new[] { RedirectUri },
            token_endpoint_auth_method = "none",
            client_name = "Test MCP client",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var clientId = registration.GetProperty("client_id").GetString()!;

        var verifier = AuthCrypto.RandomToken(48);
        var consentPage = await (await browser.GetAsync(QueryHelpers.AddQueryString("/oauth/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["code_challenge"] = AuthCrypto.PkceChallenge(verifier),
            ["code_challenge_method"] = "S256",
            ["scope"] = "mcp:tools projects:write",
        }))).Content.ReadAsStringAsync();

        var consent = await browser.PostAsync("/oauth/consent", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["transaction"] = Hidden(consentPage, "transaction"),
            ["csrf"] = Hidden(consentPage, "csrf"),
            ["decision"] = "allow",
        }));
        var idpAuthorize = QueryHelpers.ParseQuery(consent.Headers.Location!.Query);

        OidcProxyServerFactory.Idp.Nonce = idpAuthorize["nonce"];
        var callback = await browser.GetAsync($"/oauth/callback?code=upstream-code&state={Uri.EscapeDataString(idpAuthorize["state"]!)}");
        var code = QueryHelpers.ParseQuery(callback.Headers.Location!.Query)["code"].ToString();

        var tokens = await TokenAsync(browser, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = verifier,
        });
        return new Result(browser, clientId, tokens);
    }

    public static Dictionary<string, string> Refresh(string refreshToken, string clientId) => new()
    {
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken,
        ["client_id"] = clientId,
    };

    public static async Task<JsonElement> TokenAsync(HttpClient browser, Dictionary<string, string> form)
    {
        var response = await browser.PostAsync("/oauth/token", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Token request failed ({(int)response.StatusCode}): {body}");
        }

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static string Hidden(string html, string name) =>
        WebUtility.HtmlDecode(HiddenInput().Matches(html).First(m => m.Groups[1].Value == name).Groups[2].Value);

    [GeneratedRegex("<input type=\"hidden\" name=\"([^\"]+)\" value=\"([^\"]*)\">")]
    private static partial Regex HiddenInput();
}
