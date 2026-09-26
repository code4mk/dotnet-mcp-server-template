using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Keys derived from AUTH_TOKEN_SIGNING_KEY (HKDF), so every instance with the same key can validate tokens,
/// decrypt stored IdP tokens and verify consent cookies. No key ring or database needed.
/// </summary>
public sealed class AuthCrypto
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;

    public AuthCrypto(OidcProxySettings settings)
    {
        var master = Encoding.UTF8.GetBytes(settings.TokenSigningKey);
        AccessTokenKey = new SymmetricSecurityKey(Derive(master, "mcp-access-token")) { KeyId = "mcp-1" };
        _encryptionKey = Derive(master, "mcp-upstream-tokens");
        _hmacKey = Derive(master, "mcp-cookies");
    }

    public SymmetricSecurityKey AccessTokenKey { get; }

    /// <summary>Cryptographically random, URL-safe string (default 32 bytes = 256 bits).</summary>
    public static string RandomToken(int bytes = 32) => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(bytes));

    /// <summary>SHA-256 hash used as a store key, so leaked cache entries don't reveal codes or tokens.</summary>
    public static string Hash(string value) => Base64Url.EncodeToString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>PKCE S256: BASE64URL(SHA256(ASCII(verifier))).</summary>
    public static string PkceChallenge(string verifier) => Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var input = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[input.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_encryptionKey, tag.Length))
        {
            aes.Encrypt(nonce, input, cipher, tag);
        }

        return Base64Url.EncodeToString([.. nonce, .. tag, .. cipher]);
    }

    public string Decrypt(string protectedValue)
    {
        var data = Base64Url.DecodeFromChars(protectedValue);
        var nonce = data.AsSpan(0, 12);
        var tag = data.AsSpan(12, 16);
        var cipher = data.AsSpan(28);
        var plain = new byte[cipher.Length];
        using (var aes = new AesGcm(_encryptionKey, tag.Length))
        {
            aes.Decrypt(nonce, cipher, tag, plain);
        }

        return Encoding.UTF8.GetString(plain);
    }

    public string Sign(string value) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(value)));

    public bool Verify(string value, string signature) => FixedTimeEquals(Sign(value), signature);

    private static byte[] Derive(byte[] master, string purpose) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, master, 32, salt: null, info: Encoding.UTF8.GetBytes(purpose));
}
