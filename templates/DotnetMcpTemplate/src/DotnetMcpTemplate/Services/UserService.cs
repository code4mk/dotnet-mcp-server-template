using DotnetMcpTemplate.Integrations.SampleApi;
using DotnetMcpTemplate.Core.Common.Errors;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

/// <summary>Business logic for users. Knows nothing about MCP; maps the external API's shapes to DTOs.</summary>
internal sealed class UserService(ISampleApiClient api) : IUserService
{
    public async Task<UserDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var user = await api.GetUserAsync(id, cancellationToken)
            ?? throw AppException.NotFound("users.not_found", $"User {id} was not found.");
        return ToDto(user);
    }

    public async Task<PagedResult<UserDto>> SearchAsync(SearchUsersRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var matches = (await api.GetUsersAsync(cancellationToken))
            .Where(u => Contains(u.Name, query) || Contains(u.Username, query) || Contains(u.Email, query))
            .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto);

        return PagedResult<UserDto>.From(matches, request.Page, request.PageSize);
    }

    internal static UserDto ToDto(SampleUser user) =>
        new(user.Id, user.Name, user.Username, user.Email, user.Company?.Name, user.Website);

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
}
