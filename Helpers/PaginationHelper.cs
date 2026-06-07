using Microsoft.EntityFrameworkCore;

namespace PharmeasyAPI.Helpers;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public List<T> Data { get; set; } = new();

    public PagedResult<TOut> MapData<TOut>(Func<T, TOut> mapper) => new()
    {
        Page = Page,
        Limit = Limit,
        Total = Total,
        TotalPages = TotalPages,
        Data = Data.Select(mapper).ToList()
    };
}

public static class PaginationHelper
{
    public static async Task<PagedResult<T>> PaginateAsync<T>(
        this IQueryable<T> query, int page, int limit)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);
        var total = await query.CountAsync();
        var data = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
        return new PagedResult<T>
        {
            Page = page,
            Limit = limit,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / limit),
            Data = data
        };
    }
}
