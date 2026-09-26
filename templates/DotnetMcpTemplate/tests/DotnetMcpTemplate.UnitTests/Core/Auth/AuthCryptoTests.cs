using DotnetMcpTemplate.Core.Auth.Oidc;

namespace DotnetMcpTemplate.UnitTests.Core.Auth;

public sealed class AuthCryptoTests
{
    private readonly AuthCrypto _crypto = new(new OidcProxySettings { TokenSigningKey = new string('k', 48) });

    [Fact]
    public void Encrypt_then_decrypt_round_trips() =>
        Assert.Equal("secret token", _crypto.Decrypt(_crypto.Encrypt("secret token")));

    [Fact]
    public void Encryption_is_randomized() =>
        Assert.NotEqual(_crypto.Encrypt("same"), _crypto.Encrypt("same"));

    [Fact]
    public void Signatures_verify_only_for_the_same_value()
    {
        var signature = _crypto.Sign("consent:client-1");
        Assert.True(_crypto.Verify("consent:client-1", signature));
        Assert.False(_crypto.Verify("consent:client-2", signature));
    }

    [Fact]
    public void Pkce_matches_rfc7636_example() =>
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", AuthCrypto.PkceChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
}
