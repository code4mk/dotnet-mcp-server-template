namespace DotnetMcpTemplate.Integrations.SampleApi;

// The external API's own shapes (JSONPlaceholder). They never leave ApiClients/: services map them to DTOs.

public sealed record SampleUser(int Id, string Name, string Username, string Email, string? Phone, string? Website, SampleCompany? Company);

public sealed record SampleCompany(string Name);

public sealed record SamplePost(int Id, int UserId, string Title, string Body);

public sealed record SampleCreatePost(int UserId, string Title, string Body);
