using DotnetMcpTemplate.Integrations.SampleApi;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

internal sealed class FakeSampleApiClient : ISampleApiClient
{
    private readonly List<SampleUser> _users =
    [
        new(1, "Leanne Graham", "Bret", "leanne@example.com", null, null, new SampleCompany("Romaguera")),
        new(2, "Ervin Howell", "Antonette", "ervin@example.com", null, null, null),
    ];

    private readonly List<SamplePost> _posts =
    [
        new(1, 1, "Launch website", "Build and launch the new website."),
        new(2, 2, "Data migration", "Move customer data."),
    ];

    public Task<IReadOnlyList<SampleUser>> GetUsersAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SampleUser>>(_users);

    public Task<SampleUser?> GetUserAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<SamplePost>> GetPostsAsync(int? userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SamplePost>>(_posts.Where(p => userId is null || p.UserId == userId).ToList());

    public Task<SamplePost?> GetPostAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_posts.FirstOrDefault(p => p.Id == id));

    public Task<SamplePost> CreatePostAsync(SampleCreatePost post, CancellationToken cancellationToken)
    {
        lock (_posts)
        {
            var created = new SamplePost(_posts.Max(p => p.Id) + 1, post.UserId, post.Title, post.Body);
            _posts.Add(created);
            return Task.FromResult(created);
        }
    }
}
