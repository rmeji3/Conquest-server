using Ping.Dtos.Search;
using Ping.Dtos.Common;

namespace Ping.Services.Search;

public interface ISearchService
{
    Task<UnifiedSearchResultDto> UnifiedSearchAsync(UnifiedSearchFilterDto filter, string? userId);
}
