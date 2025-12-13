using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ping.Data.App;
using Ping.Dtos.Common;
using Ping.DTOs.Reports;
using Ping.Models.Reports;

namespace Ping.Services.Reports
{
    public class ReportService(AppDbContext context) : IReportService
    {
        public async Task<Report> CreateReportAsync(Guid reporterId, CreateReportDto dto)
        {
            var report = new Report
            {
                ReporterId = reporterId,
                TargetId = dto.TargetId,
                TargetType = dto.TargetType,
                Reason = dto.Reason,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            context.Reports.Add(report);
            await context.SaveChangesAsync();

            return report;
        }

        public async Task<PaginatedResult<Report>> GetReportsAsync(PaginationParams pagination, ReportStatus? status = null)
        {
            var query = context.Reports.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            return await PaginatedResult<Report>.CreateAsync(query, pagination.PageNumber, pagination.PageSize);
        }
    }
}

