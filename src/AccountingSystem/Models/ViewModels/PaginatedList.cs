using System.Collections;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Models.ViewModels;

public class PaginatedList<T> : IReadOnlyList<T>
{
    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalPages { get; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    public PaginationViewModel Pagination => new()
    {
        PageIndex = PageIndex,
        TotalPages = TotalPages,
        PageSize = PageSize,
        TotalCount = TotalCount
    };

    public T this[int index] => Items[index];
    public int Count => Items.Count;

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var normalizedPageSize = pageSize is >= 10 and <= 100 ? pageSize : 20;
        var totalCount = await source.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        var normalizedPageIndex = Math.Clamp(pageIndex <= 0 ? 1 : pageIndex, 1, totalPages);

        var items = await source
            .Skip((normalizedPageIndex - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        return new PaginatedList<T>(items, totalCount, normalizedPageIndex, normalizedPageSize);
    }
}
