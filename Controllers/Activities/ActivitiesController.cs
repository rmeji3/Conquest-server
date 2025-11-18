using Conquest.Data.App;
using Conquest.Dtos.Activities;
using Microsoft.AspNetCore.Mvc;
using Conquest.Models.Activities;

namespace Conquest.Controllers.Activities
{
    // Controllers/ActivitiesController.cs
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController(AppDbContext db) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ActivityDetailsDto>> Create([FromBody] CreateActivityDto dto)
        {
            var place = await db.Places.FindAsync(dto.PlaceId);
            if (place == null) return NotFound(new { error = "Place not found" });

            var act = new Activity { PlaceId = dto.PlaceId, Type = dto.Type.Trim(), Notes = dto.Notes };
            db.Activities.Add(act);
            await db.SaveChangesAsync();

            return Ok(new ActivityDetailsDto(act.Id, act.PlaceId, act.Type, act.Notes, act.CreatedUtc));
        }
    }

}
