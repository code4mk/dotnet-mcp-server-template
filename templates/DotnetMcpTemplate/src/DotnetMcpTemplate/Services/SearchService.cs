using DotnetMcpTemplate.Integrations.SampleApi;
using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

/// <summary>Public search across projects (titles and descriptions). Results point to readable resources.</summary>
internal sealed class SearchService(ISampleApiClient api) : ISearchService
{
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var term = query.Trim();
        var posts = await api.GetPostsAsync(null, cancellationToken);

        return posts
            .Where(p => p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Body.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(p => new SearchResultDto("project", p.Title, Snippet(p.Body), $"projects://{p.Id}"))
            .ToList();
    }

    private static string Snippet(string text)
    {
        var line = text.ReplaceLineEndings(" ");
        return line.Length > 140 ? line[..140] + "…" : line;
    }
}
