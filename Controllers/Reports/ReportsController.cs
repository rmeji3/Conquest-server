using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Ping.Models.Reports;
using Ping.Dtos.Common;
using Ping.Dtos.Reports;
using Ping.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Ping.Controllers.Reports
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ReportsController(IReportService reportService) : ControllerBase
    {
        /// <summary>
        /// Creates a new report. Supports optional screenshot attachment.
        /// Use multipart/form-data when uploading a screenshot.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> CreateReport([FromForm] CreateReportDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Unauthorized();
            }

            var report = await reportService.CreateReportAsync(userId, dto, dto.Screenshot);
            
            return StatusCode(201, report);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginatedResult<AdminReportDto>>> GetReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] ReportStatus? status = null, [FromQuery] ReportTargetType? targetType = null)
        {
            var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
            var reports = await reportService.GetReportsAsync(pagination, status, targetType);
            return Ok(reports);
        }

        /// <summary>
        /// Updates a report's moderation status (Pending / Reviewed / Dismissed).
        /// </summary>
        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Report>> UpdateStatus(Guid id, [FromBody] UpdateReportStatusDto dto)
        {
            try
            {
                var report = await reportService.UpdateReportStatusAsync(id, dto.Status);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

