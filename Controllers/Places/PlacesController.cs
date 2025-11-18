namespace Conquest.Controllers.Places
{
    using Data.App;
    using Conquest.Dtos.Places;
    using Conquest.Models.Places;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController(AppDbContext db) : ControllerBase
    {
        // POST /api/places
        [HttpPost]
        public async Task<ActionResult<PlaceDetailsDto>> Create([FromBody] UpsertPlaceDto dto)
        {
            var place = new Place
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            db.Places.Add(place);
            await db.SaveChangesAsync();

            var result = new PlaceDetailsDto(
                place.Id,
                place.Name,
                place.Address,
                place.Latitude,
                place.Longitude,
                Array.Empty<string>(), // no activities yet
                Array.Empty<string>()  // no activity kinds yet
            );

            return CreatedAtAction(nameof(GetById), new { id = place.Id }, result);
        }

        // GET /api/places/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PlaceDetailsDto>> GetById(int id)
        {
            var p = await db.Places
                .Include(x => x.PlaceActivities)
                    .ThenInclude(pa => pa.ActivityKind)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p is null) return NotFound();

            var activityNames = p.PlaceActivities
                .Select(a => a.Name)
                .Distinct()
                .ToArray();

            var activityKindNames = p.PlaceActivities
                .Where(a => a.ActivityKind != null)
                .Select(a => a.ActivityKind!.Name)
                .Distinct()
                .ToArray();

            return Ok(new PlaceDetailsDto(
                p.Id,
                p.Name,
                p.Address!,
                p.Latitude,
                p.Longitude,
                activityNames,
                activityKindNames
            ));
        }

        // GET /api/places/nearby?lat=..&lng=..&radiusKm=5&activityName=soccer&activityKind=outdoor
        [HttpGet("nearby")]
        public async Task<ActionResult<IEnumerable<PlaceDetailsDto>>> Nearby(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] double radiusKm = 5,
            [FromQuery] string? activityName = null,
            [FromQuery] string? activityKind = null)
        {
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));
            var minLat = lat - latDelta;
            var maxLat = lat + latDelta;
            var minLng = lng - lngDelta;
            var maxLng = lng + lngDelta;

            var q = db.Places
                .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat &&
                            p.Longitude >= minLng && p.Longitude <= maxLng)
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

            var result = list.Select(x =>
            {
                var activityNames = x.p.PlaceActivities
                    .Select(a => a.Name)
                    .Distinct()
                    .ToArray();

                var activityKindNames = x.p.PlaceActivities
                    .Where(a => a.ActivityKind != null)
                    .Select(a => a.ActivityKind!.Name)
                    .Distinct()
                    .ToArray();

                return new PlaceDetailsDto(
                    x.p.Id,
                    x.p.Name,
                    x.p.Address!,
                    x.p.Latitude,
                    x.p.Longitude,
                    activityNames,
                    activityKindNames
                );
            }).ToList();

            return Ok(result);
        }
    }
}
