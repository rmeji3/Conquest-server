using Conquest.Dtos.Business;
using Conquest.Models.Business;
using Conquest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Conquest.Controllers
{
    [ApiController]
    [Route("api/business")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        [HttpPost("claim")]
        [Authorize]
        public async Task<ActionResult<PlaceClaim>> SubmitClaim([FromBody] CreateClaimDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var claim = await _businessService.SubmitClaimAsync(userId, dto);
                return Ok(claim);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }



        [HttpGet("analytics/{placeId}")]
        [Authorize(Roles = "Business,Admin")]
        public IActionResult GetAnalytics(int placeId)
        {
            // Placeholder: Check if user owns the place
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (!IsOwner(userId, placeId) && !IsAdmin) return Forbidden();

            return Ok(new 
            { 
                PlaceId = placeId,
                Views = 120, // Dummy data
                CheckIns = 45,
                Rating = 4.5
            });
        }
    }
}
