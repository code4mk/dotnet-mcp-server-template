using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Auth.Oidc;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class ClaimsMergerTests
{
    private readonly ClaimsMerger _merger = new();

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
}
