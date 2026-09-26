using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

namespace DotnetMcpTemplate.IntegrationTests.Core.Auth;

/// <summary>With AUTH_STORE=file a restart or deploy doesn't sign anyone out: the client refreshes and carries on.</summary>
[Collection(ServerCollection.Name)]
public sealed class SignInPersistenceTests : IDisposable
{
    private readonly string _storePath = Path.Combine(Path.GetTempPath(), "mcp-auth-it-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Refresh_works_after_a_restart()
    {
        OidcSignIn.Result signIn;
        await using (var before = new FileStoreOidcServerFactory(_storePath))
        {
            signIn = await OidcSignIn.SignInAsync(before);
        }

        // New process, same store: the client registration, the session and the refresh token are still there.
        await using var after = new FileStoreOidcServerFactory(_storePath);
        var browser = OidcSignIn.Browser(after);
        var refreshed = await OidcSignIn.TokenAsync(browser, OidcSignIn.Refresh(signIn.RefreshToken, signIn.ClientId));

        await using var mcp = await after.ConnectAsync(refreshed.GetProperty("access_token").GetString());
        var whoami = await mcp.CallToolAsync("whoami");
        Assert.NotEqual(true, whoami.IsError);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storePath))
        {
            Directory.Delete(_storePath, recursive: true);
        }
    }
}
