using DotnetMcpTemplate.Core.Auth.Oidc;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class RedirectUriPolicyTests
{
    private readonly RedirectUriPolicy _policy = new(new OidcProxySettings
    {
        AllowedRedirectUris = "http://localhost:*,https://claude.ai/api/mcp/auth_callback,https://app.example.com/cb/*,cursor://anysphere.cursor-retrieval/*",
    });

    [Theory]
    [InlineData("http://localhost:3334/oauth/callback")]
    [InlineData("http://localhost:6274/")]
    [InlineData("https://claude.ai/api/mcp/auth_callback")]
    [InlineData("https://app.example.com/cb/mcp")]
    [InlineData("cursor://anysphere.cursor-retrieval/oauth/callback")]
    public void Allowed(string uri) => Assert.True(_policy.IsAllowed(uri));

    [Theory]
    [InlineData("http://localhost.evil.com:3334/callback")]
    [InlineData("http://localhost:3334@evil.com/callback")]
    [InlineData("https://claude.ai/api/mcp/auth_callback/extra")]
    [InlineData("https://evil.example.com/cb/mcp")]
    [InlineData("https://app.example.com:8443/cb/mcp")]
    [InlineData("http://localhost:3334/callback#fragment")]
    [InlineData("not a uri")]
    public void Rejected(string uri) => Assert.False(_policy.IsAllowed(uri));
}
