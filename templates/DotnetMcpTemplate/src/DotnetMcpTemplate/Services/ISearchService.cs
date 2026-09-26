using DotnetMcpTemplate.Models.Responses;

namespace DotnetMcpTemplate.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}
