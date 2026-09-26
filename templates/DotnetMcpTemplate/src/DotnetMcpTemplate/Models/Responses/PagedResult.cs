namespace DotnetMcpTemplate.Models.Responses;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> From(IEnumerable<T> all, int page, int pageSize)
    {
        var list = all as IReadOnlyList<T> ?? all.ToList();
        return new PagedResult<T>(list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, list.Count);
    }
}
