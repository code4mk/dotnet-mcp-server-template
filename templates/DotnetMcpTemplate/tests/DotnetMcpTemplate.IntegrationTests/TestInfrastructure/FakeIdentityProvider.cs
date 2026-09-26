using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>
/// A tiny OIDC provider behind an HttpMessageHandler: discovery, JWKS, token (issues RS256 ID tokens) and userinfo.
/// The server's "oidc-upstream" HttpClient is pointed at it, so no network is used.
/// </summary>
public sealed class FakeIdentityProvider : HttpMessageHandler
{
    public const string Issuer = "https://idp.test";
    public const string ClientId = "test-client";
    public const string ClientSecret = "test-secret";
    public const string Subject = "alice-123";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };

    /// <summary>The nonce the next ID token must carry (the test copies it from the authorize redirect).</summary>
    public string? Nonce { get; set; }

    public string? LastTokenRequestAuthorization { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        return path switch
        {
            "/.well-known/openid-configuration" => Json(new Dictionary<string, object>
            {
                ["issuer"] = Issuer,
                ["authorization_endpoint"] = Issuer + "/authorize",
                ["token_endpoint"] = Issuer + "/token",
                ["userinfo_endpoint"] = Issuer + "/userinfo",
                ["jwks_uri"] = Issuer + "/jwks",
                ["response_types_supported"] = new[] { "code" },
                ["subject_types_supported"] = new[] { "public" },
                ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            }),
            "/jwks" => Json(new { keys = new[] { Jwk() } }),
            "/token" => await TokenAsync(request, cancellationToken),
            "/userinfo" when request.Headers.Authorization?.Parameter == "upstream-access-token" => Json(new Dictionary<string, object>
            {
                ["sub"] = Subject,
                ["name"] = "Alice Example",
                ["department"] = "Research",
                ["roles"] = new[] { "admin" },
            }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };
    }

    private async Task<HttpResponseMessage> TokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastTokenRequestAuthorization = request.Headers.Authorization?.ToString();
        var form = await request.Content!.ReadAsStringAsync(cancellationToken);
        if (!form.Contains("code_verifier=", StringComparison.Ordinal) && !form.Contains("grant_type=refresh_token", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        var now = DateTime.UtcNow;
        var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = ClientId,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Subject,
                ["email"] = "alice@example.com",
                ["name"] = "Alice",
                ["nonce"] = Nonce ?? string.Empty,
            },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });

        return Json(new Dictionary<string, object>
        {
            ["access_token"] = "upstream-access-token",
            ["id_token"] = idToken,
            ["refresh_token"] = "upstream-refresh-token",
            ["expires_in"] = 3600,
            ["token_type"] = "Bearer",
        });
    }

    private object Jwk()
    {
        var parameters = _key.Rsa.ExportParameters(false);
        return new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = _key.KeyId,
            n = Base64UrlEncoder.Encode(parameters.Modulus),
            e = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
    };
}
