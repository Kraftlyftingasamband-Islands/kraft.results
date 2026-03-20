namespace KRAFT.Results.Contracts;

public sealed record class PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);