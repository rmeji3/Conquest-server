using Microsoft.AspNetCore.Http;

namespace Ping.Services.Images;

public interface IImageService
{
    /// <summary>
    /// Processes an uploaded image: validate, uploads original, creates/uploads thumbnail.
    /// Returns (OriginalUrl, ThumbnailUrl).
    /// </summary>
    Task<(string OriginalUrl, string ThumbnailUrl)> ProcessAndUploadImageAsync(IFormFile file, string folder, string userId);
}
