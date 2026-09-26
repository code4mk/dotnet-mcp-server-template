using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> ListAsync(int? ownerUserId, int page, int pageSize, CancellationToken cancellationToken);

    Task<ProjectDto> GetAsync(int id, CancellationToken cancellationToken);

    Task<ProjectDto> CreateAsync(CreateProjectRequest request, AppUser user, CancellationToken cancellationToken);

    Task<ProjectsSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
