namespace Ping.Services.Moderation;

public record ModerationResult(bool IsFlagged, string? Reason);

public interface IModerationService
{
    Task<ModerationResult> CheckContentAsync(string text);

    /// <summary>
    /// Moderates several texts in one API round trip. Results are positionally
    /// aligned with <paramref name="texts"/>.
    /// </summary>
    Task<IReadOnlyList<ModerationResult>> CheckContentBatchAsync(IReadOnlyList<string> texts);

    Task<ModerationResult> CheckImageAsync(string base64Image);
}

