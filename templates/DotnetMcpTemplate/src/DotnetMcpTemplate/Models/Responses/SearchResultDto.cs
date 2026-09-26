namespace DotnetMcpTemplate.Models.Responses;

/// <summary>A search hit. <paramref name="Uri"/> is a resource the client can read for details.</summary>
public sealed record SearchResultDto(string Kind, string Title, string Snippet, string Uri);
