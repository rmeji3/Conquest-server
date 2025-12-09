using System;
using Conquest.Models.Reports;

namespace Conquest.DTOs.Reports
{
    public class CreateReportDto
    {
        public string TargetId { get; set; } = string.Empty;
        public ReportTargetType TargetType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
