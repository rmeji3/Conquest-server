namespace Ping.Services.Reviews;

/// <summary>
/// Pure ranking logic for the global explore feed. Deliberately entity- and EF-free:
/// scoring happens in memory over a bounded candidate pool because the app runs on
/// Postgres in prod but SQLite in tests, and neither log/exp math nor spatial distance
/// translates reliably on both providers.
///
/// Ranking = time-decayed popularity ("hot" score) x proximity decay, then a per-ping
/// diversity cap, then reserved slots that pull recent reviews forward so brand-new
/// content is guaranteed visibility even with zero likes.
/// </summary>
public static class ExploreFeedRanker
{
    /// <summary>How fast popularity decays with age. score ~ (likes+1) / (ageHours+2)^Gravity.</summary>
    public const double Gravity = 1.2;

    /// <summary>Distance at which the proximity multiplier drops to 1/e (~37%).</summary>
    public const double DistanceScaleKm = 50.0;

    /// <summary>Max reviews per ping in the primary ranked segment; extras sink to the tail.</summary>
    public const int MaxPerPing = 2;

    /// <summary>Reviews newer than this qualify for reserved "fresh" slots.</summary>
    public static readonly TimeSpan FreshWindow = TimeSpan.FromHours(48);

    /// <summary>Every Nth feed position is reserved for a fresh review (1-based: positions 5, 10, ...).</summary>
    public const int FreshSlotInterval = 5;

    public record Candidate(int Id, int PingId, int Likes, DateTime CreatedAt, double Latitude, double Longitude);

    /// <summary>
    /// Orders the candidate pool into the final feed sequence. Deterministic for a fixed
    /// (candidates, asOf, userLat, userLon), which is what keeps offset pagination stable:
    /// the client fixes asOf per refresh and every page slices the same permutation.
    /// Returns a permutation of <paramref name="candidates"/> (nothing is dropped).
    /// </summary>
    public static List<Candidate> Rank(
        IReadOnlyCollection<Candidate> candidates,
        DateTime asOf,
        double? userLat,
        double? userLon)
    {
        var scored = candidates
            .OrderByDescending(c => Score(c, asOf, userLat, userLon))
            .ThenByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToList();

        // Diversity cap: a single hot place keeps its best MaxPerPing reviews in the
        // primary segment; the rest fall to an overflow tail (still score-ordered) so
        // deep scrollers can still reach them.
        var primary = new List<Candidate>(scored.Count);
        var overflow = new List<Candidate>();
        var perPing = new Dictionary<int, int>();
        foreach (var c in scored)
        {
            var seen = perPing.GetValueOrDefault(c.PingId);
            if (seen < MaxPerPing)
            {
                perPing[c.PingId] = seen + 1;
                primary.Add(c);
            }
            else
            {
                overflow.Add(c);
            }
        }

        // Fresh-slot mixing: every FreshSlotInterval-th position pulls the newest
        // not-yet-placed fresh review forward. Only primary-segment items qualify so
        // the mix can't reintroduce a review the diversity cap pushed out.
        var freshCutoff = asOf - FreshWindow;
        var freshQueue = new Queue<Candidate>(
            primary.Where(c => c.CreatedAt >= freshCutoff)
                   .OrderByDescending(c => c.CreatedAt)
                   .ThenByDescending(c => c.Id));

        var ordered = primary.Concat(overflow).ToList();
        var result = new List<Candidate>(ordered.Count);
        var placed = new HashSet<int>();
        var nextRanked = 0;

        while (result.Count < ordered.Count)
        {
            Candidate? pick = null;

            if ((result.Count + 1) % FreshSlotInterval == 0)
            {
                while (freshQueue.Count > 0 && pick is null)
                {
                    var f = freshQueue.Dequeue();
                    if (placed.Add(f.Id)) pick = f;
                }
            }

            if (pick is null)
            {
                while (nextRanked < ordered.Count && !placed.Add(ordered[nextRanked].Id)) nextRanked++;
                if (nextRanked >= ordered.Count) break;
                pick = ordered[nextRanked++];
            }

            result.Add(pick);
        }

        return result;
    }

    /// <summary>
    /// Hacker News-style hot score with a gentle gravity (likes are sparse here), times
    /// an exponential proximity decay when the viewer's location is known. The +1 on
    /// likes keeps zero-like reviews scoreable; the +2 on age keeps the base bounded.
    /// </summary>
    public static double Score(Candidate c, DateTime asOf, double? userLat, double? userLon)
    {
        var ageHours = Math.Max(0, (asOf - c.CreatedAt).TotalHours);
        var hot = (c.Likes + 1) / Math.Pow(ageHours + 2, Gravity);

        if (userLat.HasValue && userLon.HasValue)
        {
            var distKm = HaversineKm(userLat.Value, userLon.Value, c.Latitude, c.Longitude);
            hot *= Math.Exp(-distKm / DistanceScaleKm);
        }

        return hot;
    }

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
