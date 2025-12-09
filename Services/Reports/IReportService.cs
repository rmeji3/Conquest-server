using System;
using System.Threading.Tasks;
using Conquest.Dtos.Common;
using Conquest.DTOs.Reports;
using Conquest.Models.Reports;

namespace Conquest.Services.Reports
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(Guid reporterId, CreateReportDto dto);
        Task<PaginatedResult<Report>> GetReportsAsync(PaginationParams pagination, ReportStatus? status = null);
    }
}
