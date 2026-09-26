using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Auth.Oidc;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class ClaimsMergerTests
{
    private readonly ClaimsMerger _merger = new(new OidcSettings { RoleClaim = "realm_access.roles" });

    [Fact]
    public void Merge_drops_protocol_claims_and_userinfo_wins()
    {
        var idToken = JsonNode.Parse("""{"sub":"u1","iss":"https://idp","aud":"c","nonce":"n","name":"Old","email":"a@b.c"}""")!.AsObject();
        var userInfo = JsonNode.Parse("""{"sub":"u1","name":"New","department":"R&D"}""")!.AsObject();

        var merged = _merger.Merge(idToken, userInfo);

        Assert.Equal("New", merged["name"]!.ToString());
        Assert.Equal("R&D", merged["department"]!.ToString());
        Assert.Equal("a@b.c", merged["email"]!.ToString());
        Assert.False(merged.ContainsKey("iss"));
        Assert.False(merged.ContainsKey("nonce"));
    }

    [Fact]
    public void Merge_rejects_userinfo_for_another_subject()
    {
        var idToken = JsonNode.Parse("""{"sub":"u1"}""")!.AsObject();
        var userInfo = JsonNode.Parse("""{"sub":"someone-else"}""")!.AsObject();

        Assert.Throws<OAuthException>(() => _merger.Merge(idToken, userInfo));
    }

    [Fact]
    public void Roles_are_read_from_nested_claims()
    {
        var claims = JsonNode.Parse("""{"realm_access":{"roles":["admin","user"]}}""")!.AsObject();
        Assert.Equal(["admin", "user"], _merger.Roles(claims));
    }

    [Fact]
    public void Space_separated_values_are_split()
    {
        var claims = JsonNode.Parse("""{"groups":"a b"}""")!.AsObject();
        Assert.Equal(["a", "b"], ClaimsMerger.ReadList(claims, "groups"));
    }
}
