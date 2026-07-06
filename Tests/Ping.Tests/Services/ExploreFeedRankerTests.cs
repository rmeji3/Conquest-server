using Xunit;
using Ping.Services.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ping.Tests.Services;

public class ExploreFeedRankerTests
{
    private static readonly DateTime AsOf = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private static ExploreFeedRanker.Candidate Make(
        int id, int pingId = 1, int likes = 0, double ageHours = 0,
        double lat = 40.0, double lon = -74.0)
        => new(id, pingId, likes, AsOf.AddHours(-ageHours), lat, lon);

    [Fact]
    public void Score_DecaysWithAge()
    {
        var fresh = Make(1, likes: 10, ageHours: 1);
        var old = Make(2, likes: 10, ageHours: 24 * 30);

        Assert.True(
            ExploreFeedRanker.Score(fresh, AsOf, null, null) >
            ExploreFeedRanker.Score(old, AsOf, null, null));
    }

    [Fact]
    public void Score_NewReviewOutranksOldPopularOne()
    {
        // The whole point of the decay: a lightly-liked review from today beats a
        // heavily-liked review from months ago.
        var newReview = Make(1, likes: 3, ageHours: 6);
        var oldPopular = Make(2, likes: 80, ageHours: 24 * 90);

        Assert.True(
            ExploreFeedRanker.Score(newReview, AsOf, null, null) >
            ExploreFeedRanker.Score(oldPopular, AsOf, null, null));
    }

    [Fact]
    public void Score_NearbyBeatsFarAwayAtEqualPopularity()
    {
        var near = Make(1, likes: 5, ageHours: 12, lat: 40.01, lon: -74.0);
        var far = Make(2, likes: 5, ageHours: 12, lat: 34.0, lon: -118.0);

        Assert.True(
            ExploreFeedRanker.Score(near, AsOf, 40.0, -74.0) >
            ExploreFeedRanker.Score(far, AsOf, 40.0, -74.0));
    }

    [Fact]
    public void Score_WithoutUserLocation_IgnoresDistance()
    {
        var near = Make(1, likes: 5, ageHours: 12, lat: 40.0, lon: -74.0);
        var far = Make(2, likes: 5, ageHours: 12, lat: 34.0, lon: -118.0);

        Assert.Equal(
            ExploreFeedRanker.Score(near, AsOf, null, null),
            ExploreFeedRanker.Score(far, AsOf, null, null));
    }

    [Fact]
    public void Rank_ReturnsPermutationOfCandidates()
    {
        var candidates = Enumerable.Range(1, 50)
            .Select(i => Make(i, pingId: i % 7, likes: i % 11, ageHours: i * 5))
            .ToList();

        var ranked = ExploreFeedRanker.Rank(candidates, AsOf, 40.0, -74.0);

        Assert.Equal(candidates.Count, ranked.Count);
        Assert.Equal(
            candidates.Select(c => c.Id).OrderBy(id => id),
            ranked.Select(c => c.Id).OrderBy(id => id));
    }

    [Fact]
    public void Rank_CapsReviewsPerPingInPrimarySegment()
    {
        // 10 highly-liked reviews of one hot place + 10 zero-like reviews of other
        // places. Without the cap, the hot place would fill the first 10 slots.
        var hotPlace = Enumerable.Range(1, 10)
            .Select(i => Make(i, pingId: 99, likes: 100, ageHours: 1));
        var others = Enumerable.Range(11, 10)
            .Select(i => Make(i, pingId: i, likes: 0, ageHours: 1));
        var candidates = hotPlace.Concat(others).ToList();

        var ranked = ExploreFeedRanker.Rank(candidates, AsOf, null, null);

        var firstTen = ranked.Take(10).Count(c => c.PingId == 99);
        Assert.True(firstTen <= ExploreFeedRanker.MaxPerPing,
            $"Expected at most {ExploreFeedRanker.MaxPerPing} reviews of the hot ping in the top 10, got {firstTen}");
    }

    [Fact]
    public void Rank_FreshSlotsSurfaceZeroLikeNewReviews()
    {
        // 20 old-but-popular reviews vs one brand-new review with zero likes. The
        // fresh review must appear within the first fresh-slot interval, not at
        // the bottom of the feed.
        var popular = Enumerable.Range(1, 20)
            .Select(i => Make(i, pingId: i, likes: 50, ageHours: 24 * 10));
        var brandNew = Make(100, pingId: 100, likes: 0, ageHours: 0.5);
        var candidates = popular.Append(brandNew).ToList();

        var ranked = ExploreFeedRanker.Rank(candidates, AsOf, null, null);

        var position = ranked.FindIndex(c => c.Id == 100);
        Assert.InRange(position, 0, ExploreFeedRanker.FreshSlotInterval - 1);
    }

    [Fact]
    public void Rank_IsDeterministicForFixedAsOf()
    {
        var candidates = Enumerable.Range(1, 30)
            .Select(i => Make(i, pingId: i % 5, likes: i % 13, ageHours: i * 3))
            .ToList();

        var first = ExploreFeedRanker.Rank(candidates, AsOf, 40.0, -74.0);
        var second = ExploreFeedRanker.Rank(candidates, AsOf, 40.0, -74.0);

        Assert.Equal(first.Select(c => c.Id), second.Select(c => c.Id));
    }

    [Fact]
    public void HaversineKm_KnownDistance()
    {
        // NYC to LA is ~3,936 km.
        var dist = ExploreFeedRanker.HaversineKm(40.7128, -74.0060, 34.0522, -118.2437);
        Assert.InRange(dist, 3900, 3980);
    }
}
