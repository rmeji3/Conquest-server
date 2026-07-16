using System;

namespace Ping.Models.Reports
{
    public enum ReportTargetType
    {
        Ping = 0,
        PingActivity = 1,
        Review = 2,
        Profile = 3,
        Bug = 4,
        Event = 5,
        EventComment = 6,
        // 7 is intentionally unused: the shipped mobile app has always sent 8
        // for Collection (its enum skips 7), and rows are already stored that
        // way — the values here must match the wire/DB values.
        Collection = 8,
        Feedback = 9
    }

    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Dismissed
    }

    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReporterId { get; set; } = string.Empty;
        public string? TargetId { get; set; }  // Null for Bug reports, required for content reports
        public ReportTargetType TargetType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ScreenshotUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
    }
}

