namespace Conquest.Dtos.Common;

public class PaginatedResult<T>(IEnumerable<T> items, int count, int pageNumber, int pageSize)
{
    public IEnumerable<T> Items { get; set; } = items;
    public int TotalCount { get; set; } = count;
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public int TotalPages { get; set; } = (int)Math.Ceiling(count / (double)pageSize);
}
