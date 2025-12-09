using Conquest.Data.App;
using Conquest.Data.Auth;
using Conquest.Dtos.Analytics;
using Conquest.Models.Analytics; // For DailySystemMetric, UserActivityLog
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Conquest.Models.AppUsers; // For AppUser
using Conquest.Models; // For Place, Review, etc.
using Conquest.Models.Reviews;
using Conquest.Models.Reports;

using Conquest.Models.Places;

namespace Conquest.Services.Analytics;

public class AnalyticsService(
    AuthDbContext authDb,
    AppDbContext appDb,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-6); 
        var thirtyDaysAgo = today.AddDays(-29);

        // DAU: Users active today
        var dau = await authDb.UserActivityLogs
            .CountAsync(l => l.Date == today);

        // WAU: Distinct users active in last 7 days including today
        var wau = await authDb.UserActivityLogs
            .Where(l => l.Date >= sevenDaysAgo)
            .Select(l => l.UserId)
            .Distinct()
            .CountAsync();

        // MAU: Distinct users active in last 30 days
        var mau = await authDb.UserActivityLogs
            .Where(l => l.Date >= thirtyDaysAgo)
            .Select(l => l.UserId)
            .Distinct()
            .CountAsync();

        // Total Users
        var totalUsers = await authDb.Users.CountAsync();

        // New Users Today (Approximation: Users created today? We don't have CreatedAt. 
        // We'll use: Users whose FIRST activity log is today)
        // This is expensive if table is huge, but for now it's okay.
        // Optimization: Add CreatedAt to AppUser.
        // Fallback: Just return 0 or rely on DailyMetric "NewUsers" logic if we had it.
        // Let's try to query Users joined with Logs? No.
        // Let's just use TotalUsers for now and skip NewUsers logic in real-time to save perf,
        // or approximate by looking at Logs where LoginCount=1 and it is the ONLY log for that user? No.
        
        // Revised: Return 0 for NewUsersToday until we have CreatedAt column or better tracking.
        var newUsers = 0; 

        return new DashboardStatsDto(dau, wau, mau, totalUsers, newUsers);
    }

    public async Task<List<DailySystemMetric>> GetHistoricalGrowthAsync(string metricType, int days)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        return await authDb.DailySystemMetrics
            .Where(m => m.MetricType == metricType && m.Date >= cutoff)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<List<DailySystemMetric>> GetGrowthByRegionAsync()
    {
        // Example: Returning latest region stats
        // In reality, this depends on "Dimensions" JSON in DailySystemMetric
        // For now, return empty or implement basic logic if metrics exist
        return await authDb.DailySystemMetrics
            .Where(m => m.MetricType == "RegionGrowth")
            .OrderByDescending(m => m.Date)
            .Take(7)
            .ToListAsync();
    }

    public async Task<List<TrendingPlaceDto>> GetTrendingPlacesAsync()
    {
        // Trending: Places with most Reviews/CheckIns in last 7 days
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var trending = await appDb.Reviews
            .Where(r => r.CreatedAt >= cutoff && r.PlaceActivity!.Place.Visibility == PlaceVisibility.Public)
            .GroupBy(r => r.PlaceActivity!.Place)
            .Select(g => new
            {
                Place = g.Key,
                Count = g.Count(),
                Reviews = g.Count(x => x.Type == ReviewType.Review),
                CheckIns = g.Count(x => x.Type == ReviewType.CheckIn)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return trending.Select(t => new TrendingPlaceDto(
            t.Place.Id,
            t.Place.Name,
            t.Reviews,
            t.CheckIns,
            t.Count
        )).ToList();
    }

    public async Task<ModerationStatsDto> GetModerationStatsAsync()
    {
        var pendingReports = await appDb.Reports
            .CountAsync(r => r.Status == ReportStatus.Pending);

        var bannedUsers = await authDb.Users
            .CountAsync(u => u.IsBanned);

        var bannedIps = await authDb.IpBans
            .CountAsync();

        return new ModerationStatsDto(pendingReports, bannedUsers, bannedIps, 0);
    }

    public async Task ComputeDailyMetricsAsync(DateOnly date)
    {
        // Idempotency: Check if already computed?
        // We can overwrite or skip. Let's delete existing for this date/type to avoid dupes.
        
        // 1. DAU
        var dauCount = await authDb.UserActivityLogs.CountAsync(l => l.Date == date);
        await SaveMetric(date, "DAU", dauCount);

        // 2. Total Users (Snapshot)
        var totalUsers = await authDb.Users.CountAsync();
        await SaveMetric(date, "TotalUsers", totalUsers);
        
        // 3. New Users (Requires CreatedAt or diffing TotalUsers with yesterday? messy). 
        // Skip for now.

        // 4. Trending Region (if we had IpAddress geolocation) - Placeholder
    }

    private async Task SaveMetric(DateOnly date, string type, double value, string? dimensions = null)
    {
        var existing = await authDb.DailySystemMetrics
            .FirstOrDefaultAsync(m => m.Date == date && m.MetricType == type);

        if (existing != null)
        {
            existing.Value = value;
            existing.Dimensions = dimensions;
        }
        else
        {
            authDb.DailySystemMetrics.Add(new DailySystemMetric
            {
                Date = date,
                MetricType = type,
                Value = value,
                Dimensions = dimensions
            });
        }
        await authDb.SaveChangesAsync();
    }
}
