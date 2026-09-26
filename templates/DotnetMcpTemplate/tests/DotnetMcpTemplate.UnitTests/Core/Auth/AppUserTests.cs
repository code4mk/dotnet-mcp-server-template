using System.Security.Claims;
using DotnetMcpTemplate.Core.Auth;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class AppUserTests
{
    private static AppUser UserWith(params Claim[] claims) =>
        AppUser.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppClaims.Subject, "alice"), .. claims], "Test")));

    [Fact]
    public void Claim_values_reads_repeated_claims()
    {
        var user = UserWith(new Claim("roles", "admin"), new Claim("roles", "editor"));

        Assert.Equal(["admin", "editor"], user.ClaimValues("roles"));
    }

    [Fact]
    public void Claim_values_reads_a_single_claim()
    {
        Assert.Equal(["tenant-1"], UserWith(new Claim("tenant_id", "tenant-1")).ClaimValues("tenant_id"));
    }

    [Fact]
    public void Claim_values_is_empty_for_a_missing_claim()
    {
        Assert.Empty(UserWith().ClaimValues("groups"));
    }
}
