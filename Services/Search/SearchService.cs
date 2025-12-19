using Ping.Dtos.Search;
using Ping.Dtos.Common;
using Ping.Services.Profiles;
using Ping.Services.Pings;
using Ping.Services.Events;
using Ping.Dtos.Events;
using Ping.Models.Pings;

namespace Ping.Services.Search;

public class SearchService(
    IProfileService profileService,
    IPingService pingService,
    IEventService eventService,
    ILogger<SearchService> logger) : ISearchService
{
    public async Task<UnifiedSearchResultDto> UnifiedSearchAsync(UnifiedSearchFilterDto filter, string? userId)
    {
        logger.LogInformation("Unified search for query: {Query} by User: {UserId}", filter.Query, userId ?? "Anonymous");

        var pagination = new PaginationParams { PageNumber = filter.PageNumber, PageSize = filter.PageSize };

        // 1. Search Profiles
        var profilesTask = userId != null 
            ? profileService.SearchProfilesAsync(filter.Query, userId, pagination)
            : Task.FromResult(new PaginatedResult<Ping.Dtos.Profiles.ProfileDto>(new List<Ping.Dtos.Profiles.ProfileDto>(), 0, filter.PageNumber, filter.PageSize));

        // 2. Search Pings
        var pingsTask = pingService.SearchNearbyAsync(
            filter.Latitude,
            filter.Longitude,
            filter.RadiusKm,
            filter.Query, // Use query for name search
            null, // activityName
            null, // pinGenreName (could map filter.PingGenreId to name if needed, but service takes ID in some places and name in others. Let's stick to name if possible or update service)
            filter.Tags,
            null, // visibility
            null, // type
            userId,
            pagination
        );

        // 3. Search Events
        var eventFilter = new EventFilterDto
        {
            Query = filter.Query,
            Latitude = filter.Latitude,
            Longitude = filter.Longitude,
            RadiusKm = filter.RadiusKm,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            GenreId = filter.EventGenreId
        };
        var eventsTask = eventService.GetPublicEventsAsync(eventFilter, pagination, userId);

        await Task.WhenAll(profilesTask, pingsTask, eventsTask);

        return new UnifiedSearchResultDto(
            await profilesTask,
            await pingsTask,
            await eventsTask
        );
    }
}
