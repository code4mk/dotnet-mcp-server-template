using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Common.Errors;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Services;
using DotnetMcpTemplate.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetMcpTemplate.UnitTests.Services;

public sealed class ProjectServiceTests
{
    private readonly FakeSampleApiClient _api = new();
    private readonly ProjectService _service;
    private readonly AppUser _user = new() { Id = "alice", IsAuthenticated = true };

    public ProjectServiceTests() => _service = new ProjectService(_api, NullLogger<ProjectService>.Instance);

    [Fact]
    public async Task Create_adds_a_project()
    {
        var project = await _service.CreateAsync(new CreateProjectRequest("New portal", null, "low", 30, 2), _user, CancellationToken.None);

        Assert.Equal("New portal", project.Name);
        Assert.Equal("low", project.Priority);
        Assert.Contains(_api.Posts, p => p.Title == "New portal");
    }

    [Fact]
    public async Task Create_rejects_duplicate_names_for_the_owner()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateAsync(new CreateProjectRequest("launch WEBSITE", null, "low", 30, 1), _user, CancellationToken.None));

        Assert.Equal(AppErrorKind.Conflict, exception.Kind);
    }

    [Fact]
    public async Task Create_rejects_unknown_owner()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateAsync(new CreateProjectRequest("Anything", null, "low", 30, 9), _user, CancellationToken.None));

        Assert.Equal("projects.owner_not_found", exception.Code);
    }

    [Fact]
    public async Task Get_unknown_project_is_not_found()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _service.GetAsync(404, CancellationToken.None));
        Assert.Equal(AppErrorKind.NotFound, exception.Kind);
    }

    [Fact]
    public async Task Summary_counts_projects_per_owner()
    {
        var summary = await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(3, summary.TotalProjects);
        Assert.Equal(2, summary.ByOwner.First(o => o.OwnerUserId == 1).Projects);
    }
}
