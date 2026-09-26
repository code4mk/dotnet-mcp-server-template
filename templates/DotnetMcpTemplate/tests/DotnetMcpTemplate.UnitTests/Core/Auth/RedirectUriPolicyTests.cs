using DotnetMcpTemplate.Core.Auth.Oidc;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class RedirectUriPolicyTests
{
    private readonly RedirectUriPolicy _policy = new();

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback")]
    [InlineData("https://chatgpt.com/connector_platform_oauth_redirect")]
    [InlineData("https://any-client.example.com:8443/cb?x=1")]
    [InlineData("http://localhost:6274/oauth/callback")]
    [InlineData("http://127.0.0.1:33418/")]
    [InlineData("http://[::1]:5000/callback")]
    [InlineData("cursor://anysphere.cursor-retrieval/oauth/callback")]
    [InlineData("vscode://vscode.github-authentication/did-authenticate")]
    [InlineData("com.example.app:/oauth2redirect")]
    public void Any_mcp_client_can_register(string uri) => Assert.True(_policy.IsAllowed(uri));

    [Theory]
    [InlineData("http://evil.example.com/callback")]          // clear text over the network
    [InlineData("http://localhost.evil.com:3334/callback")]  // not loopback
    [InlineData("http://192.168.1.10/callback")]              // not loopback
    [InlineData("https://user:pass@claude.ai/cb")]            // user info
    [InlineData("http://localhost:3334/callback#fragment")]   // fragment
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not a uri")]
    [InlineData("/relative/callback")]
    public void Unsafe_redirect_uris_are_refused(string uri) => Assert.False(_policy.IsAllowed(uri));
}
