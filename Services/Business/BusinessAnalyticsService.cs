using Ping.Data.App;
using Ping.Models.Business;
using Microsoft.EntityFrameworkCore;
using Ping.Dtos.Business;
using Ping.Models.Reviews;

namespace Ping.Services.Business;

public interface IBusinessAnalyticsService
{
    Task TrackPlaceViewAsync(int placeId);
    Task<BusinessPlaceAnalyticsDto> GetPlaceAnalyticsAsync(int placeId);
}

public class BusinessAnalyticsService(AppDbContext db, ILogger<BusinessAnalyticsService> logger) : IBusinessAnalyticsService
{
    public async Task TrackPlaceViewAsync(int placeId)
    {
        logger.LogDebug("Tracking view for placeId={PlaceId}", placeId);
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var metric = await db.PlaceDailyMetrics
            .FirstOrDefaultAsync(m => m.PlaceId == placeId && m.Date == today);
            
        if (metric == null)
        {
            logger.LogDebug("Creating new daily metric for placeId={PlaceId}, date={Date}", placeId, today);
            metric = new PlaceDailyMetric
            {
                PlaceId = placeId,
                Date = today,
                ViewCount = 1
            };
            db.PlaceDailyMetrics.Add(metric);
        }
        else
        {
            metric.ViewCount++;
            logger.LogDebug("Incremented view count for placeId={PlaceId}, date={Date}, newCount={ViewCount}", placeId, today, metric.ViewCount);
        }
        
        await db.SaveChangesAsync();
    }

    public async Task<BusinessPlaceAnalyticsDto> GetPlaceAnalyticsAsync(int placeId)
    {
        logger.LogInformation("Fetching analytics for placeId={PlaceId}", placeId);
        
        // 1. Basic Stats
        var place = await db.Places
            .Where(p => p.Id == placeId)
            .Select(p => new 
            {
                p.Favorites,
                TotalReviews = p.PlaceActivities.SelectMany(pa => pa.Reviews).Count(),
                // Avg Rating calculation might be expensive if many reviews, but let's do simple avg for now
                // Actually, accessing Reviews across PlaceActivities needs SelectMany
                AvgRating = p.PlaceActivities.SelectMany(pa => pa.Reviews)
                    .Where(r => r.Type == ReviewType.Review)
                    .Average(r => (double?)r.Rating) ?? 0.0,
            })
            .FirstOrDefaultAsync();

        if (place == null)
        {
            logger.LogWarning("Place not found for analytics: placeId={PlaceId}", placeId);
            throw new KeyNotFoundException("Place not found");
        }
        
        logger.LogDebug("Basic stats for placeId={PlaceId}: Favorites={Favorites}, TotalReviews={TotalReviews}, AvgRating={AvgRating:F2}", 
            placeId, place.Favorites, place.TotalReviews, place.AvgRating);

        // 2. Event Count
        var eventCount = await db.Events.CountAsync(e => e.PlaceId == placeId);
        logger.LogDebug("Event count for placeId={PlaceId}: {EventCount}", placeId, eventCount);

        // 3. Views History (Last 30 days)
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var viewsHistory = await db.PlaceDailyMetrics
            .Where(m => m.PlaceId == placeId && m.Date >= cutoff)
            .OrderBy(m => m.Date)
            .Select(m => new PlaceDailyStatDto(m.Date, m.ViewCount))
            .ToListAsync();
            
        var totalViews = await db.PlaceDailyMetrics
            .Where(m => m.PlaceId == placeId)
            .SumAsync(m => m.ViewCount);
        logger.LogDebug("Views for placeId={PlaceId}: TotalViews={TotalViews}, HistoryDays={HistoryDays}", 
            placeId, totalViews, viewsHistory.Count);

        // 4. Peak Hours (from Reviews/CheckIns timestamps)
        // Group by Hour of CreatedAt
        var hours = await db.Reviews
            .Where(r => r.PlaceActivity!.PlaceId == placeId) // This navigation might rely on PlaceActivity
            .GroupBy(r => r.CreatedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();
            
        var peakHours = new List<int>(new int[24]); // 0-23 initialized to 0
        foreach (var h in hours)
        {
            if (h.Hour >= 0 && h.Hour < 24) 
                peakHours[h.Hour] = h.Count;
        }
        
        // Find actual peak hour for logging
        var peakHour = peakHours.IndexOf(peakHours.Max());
        logger.LogDebug("Peak hours calculated for placeId={PlaceId}, peakHour={PeakHour} with {PeakCount} activities", 
            placeId, peakHour, peakHours.Max());

        logger.LogInformation("Analytics fetched for placeId={PlaceId}: TotalViews={TotalViews}, Favorites={Favorites}, Reviews={Reviews}, Events={Events}", 
            placeId, totalViews, place.Favorites, place.TotalReviews, eventCount);
        
        return new BusinessPlaceAnalyticsDto(
            placeId,
            totalViews,
            place.Favorites,
            place.TotalReviews,
            place.AvgRating,
            eventCount,
            viewsHistory,
            peakHours
        );
    }
}

