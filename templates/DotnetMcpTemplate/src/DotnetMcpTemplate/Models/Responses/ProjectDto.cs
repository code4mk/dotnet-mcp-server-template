namespace DotnetMcpTemplate.Models.Responses;

public sealed record ProjectDto(int Id, string Name, string Description, int OwnerUserId, string Priority, string Status);

/// <summary>Data behind the projects dashboard (MCP App). Returned as structured content.</summary>
public sealed record ProjectsSummary(int TotalProjects, int Owners, IReadOnlyList<OwnerProjectCount> ByOwner);

public sealed record OwnerProjectCount(int OwnerUserId, string OwnerName, int Projects);
