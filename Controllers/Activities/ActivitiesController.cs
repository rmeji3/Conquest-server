using Conquest.Data.App;
using Conquest.Dtos.Activities;
using Conquest.Models.Activities;
using Conquest.Models.Places;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conquest.Controllers.Activities
{
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ActivitiesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult<ActivityDetailsDto>> Create([FromBody] CreateActivityDto dto)
        {
            // 1. Validate place
            var place = await _db.Places.FindAsync(dto.PlaceId);
            if (place is null)
                return NotFound(new { error = "Place not found" });

            // 2. Normalize name
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { error = "Activity name is required." });

            // 3. Optional: validate ActivityKind if provided
            ActivityKind? kind = null;
            if (dto.ActivityKindId is int kindId)
            {
                kind = await _db.ActivityKinds.FindAsync(kindId);
                if (kind is null)
                    return BadRequest(new { error = "Invalid activity kind." });
            }

            // 4. Enforce uniqueness per place (PlaceId + Name)
            var exists = await _db.PlaceActivities
                .AnyAsync(pa => pa.PlaceId == dto.PlaceId && pa.Name == name);

            if (exists)
                return Conflict(new { error = "An activity with that name already exists at this place." });

            // 5. Create PlaceActivity
            var pa = new PlaceActivity
            {
                PlaceId = dto.PlaceId,
                ActivityKindId = dto.ActivityKindId,
                Name = name,
                Description = dto.Description,
                CreatedUtc = DateTime.UtcNow
            };

            _db.PlaceActivities.Add(pa);
            await _db.SaveChangesAsync();

            // 6. Map to DTO
            var result = new ActivityDetailsDto(
                pa.Id,
                pa.PlaceId,
                pa.Name,
                pa.ActivityKindId,
                kind?.Name,
                pa.Description,
                pa.CreatedUtc
            );

            return Ok(result);
        }
    }
}
