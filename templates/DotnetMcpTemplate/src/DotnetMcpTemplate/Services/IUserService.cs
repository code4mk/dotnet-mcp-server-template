using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

public interface IUserService
{
    /// <summary>Throws <c>AppException</c> (NotFound) when the user doesn't exist.</summary>
    Task<UserDto> GetAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> SearchAsync(SearchUsersRequest request, CancellationToken cancellationToken);
}
