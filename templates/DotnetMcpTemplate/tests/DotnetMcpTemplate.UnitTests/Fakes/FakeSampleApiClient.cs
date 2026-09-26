using DotnetMcpTemplate.Integrations.SampleApi;

namespace DotnetMcpTemplate.UnitTests.Fakes;

/// <summary>In-memory sample API for service tests.</summary>
internal sealed class FakeSampleApiClient : ISampleApiClient
{
    public List<SampleUser> Users { get; } =
    [
        new(1, "Leanne Graham", "Bret", "leanne@example.com", null, "hildegard.org", new SampleCompany("Romaguera")),
        new(2, "Ervin Howell", "Antonette", "ervin@example.com", null, null, null),
        new(3, "Clementine Bauch", "Samantha", "clementine@example.com", null, null, null),
    ];

    public List<SamplePost> Posts { get; } =
    [
        new(1, 1, "Launch website", "Build and launch the new website."),
        new(2, 1, "Mobile app", "First version of the mobile app."),
        new(3, 2, "Data migration", "Move customer data to the new platform."),
    ];

    public Task<IReadOnlyList<SampleUser>> GetUsersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SampleUser>>(Users);

    public Task<SampleUser?> GetUserAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<SamplePost>> GetPostsAsync(int? userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SamplePost>>(Posts.Where(p => userId is null || p.UserId == userId).ToList());

    public Task<SamplePost?> GetPostAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));

    public Task<SamplePost> CreatePostAsync(SampleCreatePost post, CancellationToken cancellationToken)
    {
        var created = new SamplePost(Posts.Max(p => p.Id) + 1, post.UserId, post.Title, post.Body);
        Posts.Add(created);
        return Task.FromResult(created);
    }
}
