using DotnetMcpTemplate.Core.ApiClients;

namespace DotnetMcpTemplate.Integrations.SampleApi;

/// <summary>Sample data source: https://jsonplaceholder.typicode.com (users and posts). Configured with SAMPLE_API_*.</summary>
public interface ISampleApiClient
{
    Task<IReadOnlyList<SampleUser>> GetUsersAsync(CancellationToken cancellationToken);

    Task<SampleUser?> GetUserAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SamplePost>> GetPostsAsync(int? userId, CancellationToken cancellationToken);

    Task<SamplePost?> GetPostAsync(int id, CancellationToken cancellationToken);

    Task<SamplePost> CreatePostAsync(SampleCreatePost post, CancellationToken cancellationToken);
}

public sealed class SampleApiClient(HttpClient http) : ApiClient(http), ISampleApiClient
{
    public const string Prefix = "SAMPLE_API";

    protected override string SourceName => "sample API";

    public async Task<IReadOnlyList<SampleUser>> GetUsersAsync(CancellationToken cancellationToken) =>
        await GetAsync<List<SampleUser>>("users", cancellationToken: cancellationToken) ?? [];

    public async Task<SampleUser?> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync<SampleUser>($"users/{id}", cancellationToken: cancellationToken);
        }
        catch (ApiClientException exception) when (exception.IsNotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SamplePost>> GetPostsAsync(int? userId, CancellationToken cancellationToken) =>
        await GetAsync<List<SamplePost>>("posts",
            query: userId is null ? null : new Dictionary<string, string?> { ["userId"] = userId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            cancellationToken: cancellationToken) ?? [];

    public async Task<SamplePost?> GetPostAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync<SamplePost>($"posts/{id}", cancellationToken: cancellationToken);
        }
        catch (ApiClientException exception) when (exception.IsNotFound)
        {
            return null;
        }
    }

    public async Task<SamplePost> CreatePostAsync(SampleCreatePost post, CancellationToken cancellationToken) =>
        await PostAsync<SamplePost>("posts", post, cancellationToken: cancellationToken)
        ?? throw new ApiClientException(SourceName, "The sample API returned no created item.");
}
