using System;
using Ping.Models.Reports;
using System.ComponentModel.DataAnnotations;

namespace Ping.Dtos.Reports
{
    // Admin console listing: a report enriched with the reporter's identity
    // (Report only stores the reporter's user id).
    public record AdminReportDto(
        Guid Id,
        string ReporterId,
        string? ReporterUserName,
        string? ReporterEmail,
        string? TargetId,
        ReportTargetType TargetType,
        string Reason,
        string? Description,
        string? ScreenshotUrl,
        DateTime CreatedAt,
        ReportStatus Status
    );

    public record UpdateReportStatusDto([Required] ReportStatus Status);
}
