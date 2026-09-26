namespace DotnetMcpTemplate.Core.Auth;

/// <summary>Claim names used in server tokens and read by <see cref="AppUser"/>.</summary>
public static class AppClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string PreferredUsername = "preferred_username";
    public const string Picture = "picture";
    public const string Scope = "scope";
    public const string EntraScope = "scp";
    public const string ClientId = "client_id";
    public const string SessionId = "sid";
    public const string IdentityProvider = "idp";
}
