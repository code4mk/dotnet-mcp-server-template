namespace DotnetMcpTemplate.Models.Responses;

public sealed record UserDto(int Id, string Name, string Username, string Email, string? Company, string? Website);
