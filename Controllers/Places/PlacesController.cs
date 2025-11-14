using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Place
{
    using Conquest.Data.App;
    using Conquest.Dtos.Places;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using static Conquest.Dtos.Places.PlaceDto;
    using Conquest.Models.Places;

    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PlacesController(AppDbContext db) => _db = db;

        // POST /api/places
        [HttpPost]
        public async Task<ActionResult<PlaceDto>> Create([FromBody] UpsertPlaceDto dto)
        {
            var place = new Place
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };
            _db.Places.Add(place);
            await _db.SaveChangesAsync();

            // Fix for IDE0305: Use collection initializer for Array.Empty<string>()
            var result = new PlaceDetailsDto(
                place.Id,
                place.Name,
                place.Address,
                place.Latitude,
                place.Longitude,
                Array.Empty<string>() // This is already the simplest form; no further simplification needed.
            );
            return CreatedAtAction(nameof(GetById), new { id = place.Id }, result);
        }

        // Fix for CS0029: Change return type to ActionResult<PlaceDetailsDto> and return Ok() for GET
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PlaceDetailsDto>> GetById(int id)
        {
            var p = await _db.Places.Include(x => x.Activities).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            return Ok(new PlaceDetailsDto(
                p.Id,
                p.Name,
                p.Address,
                p.Latitude,
                p.Longitude,
                p.Activities.Select(a => a.Type).Distinct().ToArray()
            ));
        }

        // GET /api/places/nearby?lat=..&lng=..&radiusKm=5&activityType=tennis
        [HttpGet("nearby")]
        public async Task<ActionResult<IEnumerable<PlaceDetailsDto>>> Nearby(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] double radiusKm = 5,
            [FromQuery] string? activityType = null)
        {
            // quick prefilter by bounding box to avoid scanning entire table
            var latDelta = radiusKm / 111.0;                     // ~111km per lat degree
            var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));
            var minLat = lat - latDelta;
            var maxLat = lat + latDelta;
            var minLng = lng - lngDelta;
            var maxLng = lng + lngDelta;

            var q = _db.Places
                .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat &&
                            p.Longitude >= minLng && p.Longitude <= maxLng)
                .Include(p => p.Activities)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(activityType))
            {
                var at = activityType.Trim().ToLowerInvariant();
                q = q.Where(p => p.Activities.Any(a => a.Type.ToLower() == at));
            }

            var list = await q
                .Select(p => new
                {
                    p,
                    // Haversine distance (km) computed in-app. For big data, switch to SQL Server + geography.
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

            return Ok(list.Select(x =>
                new PlaceDetailsDto(
                    x.p.Id, x.p.Name, x.p.Address, x.p.Latitude, x.p.Longitude,
                    x.p.Activities.Select(a => a.Type).Distinct().ToArray()
                )).ToList());
        }
    }
}
