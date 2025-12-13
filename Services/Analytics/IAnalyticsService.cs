using Ping.Models.Analytics;
using Ping.Dtos.Analytics;

namespace Ping.Services.Analytics;

public interface IAnalyticsService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<List<DailySystemMetric>> GetHistoricalGrowthAsync(string metricType, int days);
    Task<List<TrendingPlaceDto>> GetTrendingPlacesAsync();
    Task<ModerationStatsDto> GetModerationStatsAsync();
    Task ComputeDailyMetricsAsync(DateOnly date);
    Task<List<DailySystemMetric>> GetGrowthByRegionAsync(); 
}

