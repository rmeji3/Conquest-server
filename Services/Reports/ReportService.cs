using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ping.Data.App;
using Ping.Dtos.Common;
using Ping.Dtos.Reports;
using Ping.Models.AppUsers;
using Ping.Models.Reports;
using Ping.Services.Storage;

namespace Ping.Services.Reports
{
    public class ReportService(AppDbContext context, IStorageService storageService) : IReportService
    {
        public async Task<Report> CreateReportAsync(string reporterId, CreateReportDto dto, IFormFile? screenshot = null)
        {
            // Validate: Bug reports and app Feedback don't need a TargetId, but all other report types do
            if (dto.TargetType != ReportTargetType.Bug && dto.TargetType != ReportTargetType.Feedback && string.IsNullOrWhiteSpace(dto.TargetId))
            {
                throw new ArgumentException("TargetId is required for content reports (Ping, Review, Profile, Event, etc.).");
            }

            string? screenshotUrl = null;

            // Upload screenshot if provided
            if (screenshot != null)
            {
                var key = $"reports/{reporterId}/{Guid.NewGuid()}{Path.GetExtension(screenshot.FileName)}";
                screenshotUrl = await storageService.UploadFileAsync(screenshot, key);
            }

            var report = new Report
            {
                ReporterId = reporterId,
                TargetId = dto.TargetId,
                TargetType = dto.TargetType,
                Reason = dto.Reason,
                Description = dto.Description,
                ScreenshotUrl = screenshotUrl,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            context.Reports.Add(report);
            await context.SaveChangesAsync();

            return report;
        }

        public async Task<PaginatedResult<AdminReportDto>> GetReportsAsync(PaginationParams pagination, ReportStatus? status = null, ReportTargetType? targetType = null)
        {
            var query = context.Reports.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (targetType.HasValue)
            {
                query = query.Where(r => r.TargetType == targetType.Value);
            }

            // Left-join the reporter (Report has no navigation property) so the
            // admin console can show who filed it; deleted accounts fall out as null.
            var projected =
                from r in query
                join u in context.Set<AppUser>() on r.ReporterId equals u.Id into reporters
                from u in reporters.DefaultIfEmpty()
                orderby r.CreatedAt descending
                select new AdminReportDto(
                    r.Id,
                    r.ReporterId,
                    u != null ? u.UserName : null,
                    u != null ? u.Email : null,
                    r.TargetId,
                    r.TargetType,
                    r.Reason,
                    r.Description,
                    r.ScreenshotUrl,
                    r.CreatedAt,
                    r.Status
                );

            return await PaginatedResult<AdminReportDto>.CreateAsync(projected, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Report> UpdateReportStatusAsync(Guid reportId, ReportStatus status)
        {
            var report = await context.Reports.FirstOrDefaultAsync(r => r.Id == reportId)
                ?? throw new KeyNotFoundException($"Report {reportId} not found.");

            report.Status = status;
            await context.SaveChangesAsync();

            return report;
        }
    }
}

