using DotnetMcpTemplate.Integrations.SampleApi;
using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Common.Errors;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

/// <summary>Projects are the sample API's posts. Business rules (unique names per owner) live here.</summary>
internal sealed class ProjectService(ISampleApiClient api, ILogger<ProjectService> logger) : IProjectService
{
    public async Task<PagedResult<ProjectDto>> ListAsync(int? ownerUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var posts = await api.GetPostsAsync(ownerUserId, cancellationToken);
        return PagedResult<ProjectDto>.From(posts.OrderBy(p => p.Id).Select(ToDto), page, pageSize);
    }

    public async Task<ProjectDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var post = await api.GetPostAsync(id, cancellationToken)
            ?? throw AppException.NotFound("projects.not_found", $"Project {id} was not found.");
        return ToDto(post);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, AppUser user, CancellationToken cancellationToken)
    {
        if (await api.GetUserAsync(request.OwnerUserId, cancellationToken) is null)
        {
            throw AppException.Rule("projects.owner_not_found", $"Owner {request.OwnerUserId} does not exist.");
        }

        var existing = await api.GetPostsAsync(request.OwnerUserId, cancellationToken);
        if (existing.Any(p => string.Equals(p.Title.Trim(), request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("projects.name_exists", $"User {request.OwnerUserId} already has a project named '{request.Name}'.");
        }

        var created = await api.CreatePostAsync(
            new SampleCreatePost(request.OwnerUserId, request.Name.Trim(), request.Description?.Trim() ?? string.Empty), cancellationToken);

        logger.LogInformation("Project {ProjectId} created by {User}", created.Id, user.Id);
        return ToDto(created) with { Priority = request.Priority };
    }

    public async Task<ProjectsSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var postsTask = api.GetPostsAsync(null, cancellationToken);
        var usersTask = api.GetUsersAsync(cancellationToken);
        await Task.WhenAll(postsTask, usersTask);

        var names = usersTask.Result.ToDictionary(u => u.Id, u => u.Name);
        var byOwner = postsTask.Result
            .GroupBy(p => p.UserId)
            .Select(g => new OwnerProjectCount(g.Key, names.GetValueOrDefault(g.Key, $"User {g.Key}"), g.Count()))
            .OrderByDescending(o => o.Projects)
            .ThenBy(o => o.OwnerUserId)
            .ToList();

        return new ProjectsSummary(postsTask.Result.Count, byOwner.Count, byOwner);
    }

    internal static ProjectDto ToDto(SamplePost post) =>
        new(post.Id, post.Title, post.Body, post.UserId, Priority: "medium", Status: post.Id % 3 == 0 ? "done" : "active");
}
