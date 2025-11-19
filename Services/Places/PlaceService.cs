using Conquest.Data.App;
using Conquest.Dtos.Activities;
using Conquest.Dtos.Places;
using Conquest.Models.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Places;

public class PlaceService(AppDbContext db, ILogger<PlaceService> logger) : IPlaceService
{
    public async Task<PlaceDetailsDto> CreatePlaceAsync(UpsertPlaceDto dto, string userId)
    {
        // Daily rate limit per user (e.g. 10 per day)
        var today = DateTime.UtcNow.Date;

        var createdToday = await db.Places
            .CountAsync(p => p.OwnerUserId == userId && p.CreatedUtc >= today);

        if (createdToday >= 10)
        {
            logger.LogWarning("Place creation rate limit reached for user {UserId}", userId);
            throw new InvalidOperationException("You’ve reached the daily limit for adding places.");
        }

        var place = new Place
        {
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            OwnerUserId = userId,
            IsPublic = dto.IsPublic,
            CreatedUtc = DateTime.UtcNow
        };

        db.Places.Add(place);
        await db.SaveChangesAsync();

        logger.LogInformation("Place created: {PlaceId} by {UserId}", place.Id, userId);

        return new PlaceDetailsDto(
            place.Id,
            place.Name,
            place.Address ?? string.Empty,
            place.Latitude,
            place.Longitude,
            place.IsPublic,
            IsOwner: true,
            Activities: Array.Empty<ActivitySummaryDto>(),
            ActivityKinds: Array.Empty<string>()
        );
    }

    public async Task<PlaceDetailsDto?> GetPlaceByIdAsync(int id, string? userId)
    {
        var p = await db.Places
            .AsNoTracking()
            .Include(x => x.PlaceActivities)
                .ThenInclude(pa => pa.ActivityKind)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p is null) return null;

        var isOwner = userId != null && p.OwnerUserId == userId;

        // Hide private places from non-owners
        if (!p.IsPublic && !isOwner)
            return null;

        var activities = p.PlaceActivities
            .Select(a => new ActivitySummaryDto(
                a.Id,
                a.Name,
                a.ActivityKindId,
                a.ActivityKind?.Name
            ))
            .ToArray();

        var activityKindNames = p.PlaceActivities
            .Where(a => a.ActivityKind != null)
            .Select(a => a.ActivityKind!.Name)
            .Distinct()
            .ToArray();

        return new PlaceDetailsDto(
            p.Id,
            p.Name,
            p.Address ?? string.Empty,
            p.Latitude,
            p.Longitude,
            p.IsPublic,
            isOwner,
            activities,
            activityKindNames
        );
    }

    public async Task<IEnumerable<PlaceDetailsDto>> SearchNearbyAsync(double lat, double lng, double radiusKm, string? activityName, string? activityKind, string? userId)
    {
        logger.LogDebug("Nearby search: lat={Lat}, lng={Lng}, radius={Radius}", lat, lng, radiusKm);

        var latDelta = radiusKm / 111.0;
        var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));
        var minLat = lat - latDelta;
        var maxLat = lat + latDelta;
        var minLng = lng - lngDelta;
        var maxLng = lng + lngDelta;

        var q = db.Places
            .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat &&
                        p.Longitude >= minLng && p.Longitude <= maxLng)
            .Where(p => p.IsPublic || (userId != null && p.OwnerUserId == userId))
            .AsNoTracking()
            .Include(p => p.PlaceActivities)
                .ThenInclude(pa => pa.ActivityKind)
            .AsQueryable();

        // Filter by ACTIVITY NAME (PlaceActivity.Name)
        if (!string.IsNullOrWhiteSpace(activityName))
        {
            var an = activityName.Trim().ToLowerInvariant();
            q = q.Where(p =>
                p.PlaceActivities.Any(a =>
                    a.Name.ToLower() == an));
        }

        // Filter by ACTIVITY KIND (ActivityKind.Name)
        if (!string.IsNullOrWhiteSpace(activityKind))
        {
            var ak = activityKind.Trim().ToLowerInvariant();
            q = q.Where(p =>
                p.PlaceActivities.Any(a =>
                    a.ActivityKind != null &&
                    a.ActivityKind.Name.ToLower() == ak));
        }

        var list = await q
            .Select(p => new
            {
                p,
                DistanceKm = 6371.0 * 2.0 * Math.Asin(
                    Math.Sqrt(
                        Math.Pow(Math.Sin((p.Latitude - lat) * Math.PI / 180.0 / 2.0), 2) +
                        Math.Cos(lat * Math.PI / 180.0) * Math.Cos(p.Latitude * Math.PI / 180.0) *
                        Math.Pow(Math.Sin((p.Longitude - lng) * Math.PI / 180.0 / 2.0), 2)
                    )
                )
            })
            .Where(x => x.DistanceKm <= radiusKm)
            .OrderBy(x => x.DistanceKm)
            .Take(100)
            .ToListAsync();

        return list.Select(x =>
        {
            var activityKindNames = x.p.PlaceActivities
                .Where(a => a.ActivityKind != null)
                .Select(a => a.ActivityKind!.Name)
                .Distinct()
                .ToArray();

            var isOwner = userId != null && x.p.OwnerUserId == userId;

            var activities = x.p.PlaceActivities
                .Select(a => new ActivitySummaryDto(
                    a.Id,
                    a.Name,
                    a.ActivityKindId,
                    a.ActivityKind?.Name
                ))
                .ToArray();
            
            return new PlaceDetailsDto(
                x.p.Id,
                x.p.Name,
                x.p.Address ?? string.Empty,
                x.p.Latitude,
                x.p.Longitude,
                x.p.IsPublic,
                isOwner,
                activities,
                activityKindNames
            );
        }).ToList();
    }
}
