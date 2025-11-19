using System.Security.Claims;
using Conquest.Dtos.Events;
using Conquest.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conquest.Controllers.Events
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController(IEventService eventService) : ControllerBase
    {
        // POST /api/Events/create
        [HttpPost("create")]
        public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            try
            {
                var result = await eventService.CreateEventAsync(dto, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/events/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventDto>> GetById(int id)
        {
            var result = await eventService.GetEventByIdAsync(id);
            if (result is null) return NotFound("Event not found.");
            return Ok(result);
        }

        // GET /api/events/mine
        [HttpGet("mine")]
        public async Task<ActionResult<List<EventDto>>> GetMyEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var result = await eventService.GetMyEventsAsync(userId);
            return Ok(result);
        }

        // GET /api/events/attending
        [HttpGet("attending")]
        public async Task<ActionResult<List<EventDto>>> GetEventsAttending()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var result = await eventService.GetEventsAttendingAsync(userId);
            return Ok(result);
        }

        // GET /api/events/public
        [HttpGet("public")]
        public async Task<ActionResult<List<EventDto>>> GetPublicEvents(
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radiusKm)
        {
            if (!lat.HasValue || !lng.HasValue || !radiusKm.HasValue || radiusKm <= 0)
            {
                return BadRequest("lat, lng, and radiusKm are required.");
            }

            var centerLat = lat.Value;
            var centerLng = lng.Value;
            var radius = radiusKm.Value;
            var latDelta = radius / 111.32;
            var lngDelta = radius / (111.32 * Math.Cos(centerLat * Math.PI / 180.0));

            var result = await eventService.GetPublicEventsAsync(
                centerLat - latDelta,
                centerLat + latDelta,
                centerLng - lngDelta,
                centerLng + lngDelta
            );

            return Ok(result);
        }

        // DELETE /api/events/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            try
            {
                var result = await eventService.DeleteEventAsync(id, userId);
                if (!result) return NotFound("Event not found.");
                return Ok("Event deleted successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // POST /api/events/{id}/join
        [HttpPost("{id:int}/join")]
        public async Task<IActionResult> JoinEvent(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var result = await eventService.JoinEventAsync(id, userId);
            if (!result) return NotFound("Event not found.");
            return Ok("Joined event.");
        }

        // POST /api/events/{id}/leave
        [HttpPost("{id:int}/leave")]
        public async Task<IActionResult> LeaveEvent(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var result = await eventService.LeaveEventAsync(id, userId);
            if (!result) return NotFound("Not attending this event.");
            return Ok("Left event.");
        }
    }
}