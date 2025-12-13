using System;
using System.Threading.Tasks;
using Ping.Dtos.Common;
using Ping.DTOs.Reports;
using Ping.Models.Reports;

namespace Ping.Services.Reports
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(Guid reporterId, CreateReportDto dto);
        Task<PaginatedResult<Report>> GetReportsAsync(PaginationParams pagination, ReportStatus? status = null);
    }
}

