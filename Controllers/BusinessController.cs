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
                return CreatedAtAction(nameof(GetPendingClaims), new { id = claim.Id }, claim);
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

        [HttpGet("claims")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ClaimDto>>> GetPendingClaims()
        {
            var claims = await _businessService.GetPendingClaimsAsync();
            return Ok(claims);
        }

        [HttpPost("claims/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reviewerId == null) return Unauthorized();

            try
            {
                await _businessService.ApproveClaimAsync(id, reviewerId);
                return Ok(new { message = "Claim approved and ownership transferred." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("claims/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectClaim(int id)
        {
            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reviewerId == null) return Unauthorized();

            try
            {
                await _businessService.RejectClaimAsync(id, reviewerId);
                return Ok(new { message = "Claim rejected." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
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
