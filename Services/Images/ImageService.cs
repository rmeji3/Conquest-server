using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Ping.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Ping.Services.Images;

public class ImageService(IStorageService storageService, HttpClient httpClient, ILogger<ImageService> logger) : IImageService
{
    static ImageService()
    {
        // Bound ImageMagick's native pixel-cache RAM. Its allocations are invisible to
        // the .NET GC, and one 48MP iPhone HEIC decodes to ~200MB of pixels — two
        // concurrent decodes blew past the prod container's 512MB cgroup limit and got
        // the process OOM-killed mid-backfill. Above this cap Magick transparently
        // spills the pixel cache to a disk-backed file (slower, but bounded); above
        // the disk cap it throws a catchable exception instead of dying.
        ImageMagick.ResourceLimits.Memory = 128UL * 1024 * 1024;
        ImageMagick.ResourceLimits.Disk = 1UL * 1024 * 1024 * 1024;
    }

    // Sized for the explore feed's 2-column masonry: cards are ~180pt wide, which is
    // ~540 physical px on 3x devices. The cap applies to the LONG edge, so portrait
    // photos need headroom for their short edge to still cover the card — 500 left
    // portraits at ~375px wide and they rendered visibly upscaled/blurry.
    private const int MaxThumbnailSize = 800;
    private const int ThumbnailQuality = 80; // ImageSharp's default 75 is visibly soft
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    // We must re-encode the original to bake in EXIF orientation, which is
    // lossy for JPEG/WebP. ImageSharp's default JPEG quality is only 75, which
    // visibly softens full-size photos (e.g. the ping detail hero). Re-encode
    // at a high quality so the stored original stays close to what was uploaded.
    private const int OriginalQuality = 92;

    public async Task<(string OriginalUrl, string ThumbnailUrl)> ProcessAndUploadImageAsync(IFormFile file, string folder, string userId)
    {
        // 1. Validation
        if (file.Length > MaxFileSize)
            throw new ArgumentException($"File size exceeds {MaxFileSize / 1024 / 1024}MB limit.");

        var ext = Path.GetExtension(file.FileName);
        var extLower = ext?.ToLowerInvariant();
        bool isLottie = extLower == ".json";
        bool isShader = extLower == ".sksl";

        if (isLottie || isShader)
        {
            if (folder != "stickers")
                throw new ArgumentException("Lottie and Shader files are only allowed for stickers.");

            var assetKey = $"{folder}/{userId}/{NewUploadId()}_orig{extLower}";

            var contentType = isLottie ? "application/json" : "text/plain";
            using var fileStream = file.OpenReadStream();
            var uploadFile = new FormFile(fileStream, 0, file.Length, file.Name, file.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            var assetUrl = await storageService.UploadFileAsync(uploadFile, assetKey);
            logger.LogInformation("Uploaded non-image asset {OriginalKey} directly as Lottie/Shader", assetKey);
            return (assetUrl, assetUrl);
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic", "image/heif", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid file type. Only JPEG, PNG, WebP, GIF, HEIC, HEIF, and SVG are allowed.");

        // 2. Generate Keys. The id includes a random component: review images are now
        //    processed concurrently, and tick-only keys collide for uploads that start
        //    within the same timer resolution (silently overwriting each other in S3).
        var uploadId = NewUploadId();
        var originalKey = $"{folder}/{userId}/{uploadId}_orig{ext}";
        var thumbKey = $"{folder}/{userId}/{uploadId}_thumb{ext}";

        // 3. Decode the image so we can bake in EXIF orientation. Link unfurlers
        //    / OG-image fetchers (and other non-EXIF-aware consumers) render the
        //    raw pixels, so a photo carrying a "rotate 90°" EXIF tag shows up
        //    sideways unless we apply the rotation to the pixels here.
        //    Re-encoding is lossy and expensive though, so it only happens when the
        //    photo actually carries a non-upright orientation tag — the mobile app
        //    already re-encodes to upright JPEG on-device, so review images normally
        //    skip it and the client's bytes are uploaded untouched.
        string originalUrl;
        string thumbUrl;
        try
        {
            // Detect the source format so we can re-save the original in kind.
            IImageFormat format;
            using (var detectStream = file.OpenReadStream())
                format = await Image.DetectFormatAsync(detectStream);

            using var imageStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(imageStream);

            using var origOut = new MemoryStream();
            Task<string> originalUpload;
            if (NeedsOrientationFix(image))
            {
                // Bake EXIF orientation into the pixels, then drop the now-stale tag.
                image.Mutate(x => x.AutoOrient());

                // Upload the orientation-corrected original in its original format.
                // Pick a high-quality encoder for the lossy formats so re-encoding
                // doesn't degrade the photo; lossless/other formats keep their default.
                IImageEncoder originalEncoder = format.Name switch
                {
                    "JPEG" => new JpegEncoder { Quality = OriginalQuality },
                    "WEBP" => new WebpEncoder { Quality = OriginalQuality },
                    _ => image.Configuration.ImageFormatsManager.GetEncoder(format),
                };
                await image.SaveAsync(origOut, originalEncoder);
                origOut.Position = 0;
                var origFile = new FormFile(origOut, 0, origOut.Length, "file", $"original{ext}")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = file.ContentType
                };
                originalUpload = storageService.UploadFileAsync(origFile, originalKey);
            }
            else
            {
                originalUpload = storageService.UploadFileAsync(file, originalKey);
            }

            // Generate the thumbnail while the original uploads; the two S3 puts
            // overlap instead of running back-to-back.
            if (image.Width > MaxThumbnailSize || image.Height > MaxThumbnailSize)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxThumbnailSize, MaxThumbnailSize)
                }));
            }

            using (var outStream = new MemoryStream())
            {
                await image.SaveAsWebpAsync(outStream, new WebpEncoder { Quality = ThumbnailQuality });
                outStream.Position = 0;
                var thumbFile = new FormFile(outStream, 0, outStream.Length, "file", $"thumbnail.webp")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/webp"
                };
                thumbUrl = await storageService.UploadFileAsync(thumbFile, Path.ChangeExtension(thumbKey, ".webp"));
            }

            originalUrl = await originalUpload;
        }
        catch (UnknownImageFormatException)
        {
            // For formats ImageSharp can't decode (mainly HEIC), upload the raw
            // original as-is and fall back to Magick.NET for the thumbnail. Only if
            // that also fails does the original serve as its own thumbnail.
            originalUrl = await storageService.UploadFileAsync(file, originalKey);
            try
            {
                using var raw = new MemoryStream();
                using (var rawStream = file.OpenReadStream())
                    await rawStream.CopyToAsync(raw);

                using var webpOut = EncodeWebpThumbnailWithMagick(raw.ToArray());
                var thumbFile = new FormFile(webpOut, 0, webpOut.Length, "file", "thumbnail.webp")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/webp"
                };
                thumbUrl = await storageService.UploadFileAsync(thumbFile, Path.ChangeExtension(thumbKey, ".webp"));
                logger.LogInformation("Generated thumbnail via Magick.NET for ImageSharp-unsupported format {Extension}", ext);
            }
            catch (Exception ex)
            {
                thumbUrl = originalUrl;
                logger.LogWarning(ex, "Unsupported image format for processing, using raw original as thumbnail");
            }
        }

        logger.LogInformation("Uploaded image {OriginalKey} and thumbnail {ThumbKey}", originalKey, thumbKey);

        return (originalUrl, thumbUrl);
    }

    /// <summary>
    /// Storage-key id: ticks keep keys chronologically sortable, the GUID segment keeps
    /// them unique when several uploads for the same user start in the same tick.
    /// </summary>
    private static string NewUploadId() => $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";

    /// <summary>
    /// Thumbnail fallback for formats ImageSharp can't decode (mainly iPhone HEIC).
    /// Magick.NET ships libheif, so it can read them; output matches the primary
    /// pipeline (upright, max-edge-capped WebP at ThumbnailQuality).
    /// </summary>
    private static MemoryStream EncodeWebpThumbnailWithMagick(byte[] sourceBytes)
    {
        using var magick = new ImageMagick.MagickImage(sourceBytes);
        magick.AutoOrient();
        if (magick.Width > MaxThumbnailSize || magick.Height > MaxThumbnailSize)
        {
            // MagickGeometry shrinks to fit the box while preserving aspect ratio.
            magick.Resize(new ImageMagick.MagickGeometry(MaxThumbnailSize, MaxThumbnailSize));
        }
        magick.Quality = ThumbnailQuality;
        magick.Format = ImageMagick.MagickFormat.WebP;

        var outStream = new MemoryStream();
        magick.Write(outStream);
        outStream.Position = 0;
        return outStream;
    }

    /// <summary>True when the image carries an EXIF orientation tag other than upright.</summary>
    private static bool NeedsOrientationFix(Image image)
    {
        var exif = image.Metadata.ExifProfile;
        if (exif is null) return false;
        if (!exif.TryGetValue(ExifTag.Orientation, out var orientation) || orientation is null) return false;
        return orientation.Value != 1; // 1 = TopLeft (already upright)
    }

    public async Task<string> GenerateThumbnailFromUrlAsync(string imageUrl, string folder, string userId)
    {
        try
        {
            using var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            // Buffered so a failed ImageSharp decode can retry with Magick.NET —
            // legacy originals include HEIC, which ImageSharp has no decoder for.
            var sourceBytes = await response.Content.ReadAsByteArrayAsync();

            using var outStream = new MemoryStream();
            try
            {
                using var image = await Image.LoadAsync(new MemoryStream(sourceBytes));

                // Bake in EXIF orientation so the thumbnail is always upright.
                image.Mutate(x => x.AutoOrient());

                if (image.Width > MaxThumbnailSize || image.Height > MaxThumbnailSize)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxThumbnailSize, MaxThumbnailSize)
                    }));
                }

                await image.SaveAsWebpAsync(outStream, new WebpEncoder { Quality = ThumbnailQuality });
            }
            catch (UnknownImageFormatException)
            {
                using var magickOut = EncodeWebpThumbnailWithMagick(sourceBytes);
                await magickOut.CopyToAsync(outStream);
            }
            outStream.Position = 0;

            var thumbKey = $"{folder}/{userId}/{NewUploadId()}_thumb.webp";
            var thumbFile = new FormFile(outStream, 0, outStream.Length, "file", "thumbnail.webp")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/webp"
            };

            var thumbUrl = await storageService.UploadFileAsync(thumbFile, thumbKey);
            logger.LogInformation("Generated thumbnail {ThumbKey} from existing image {Url}", thumbKey, imageUrl);
            return thumbUrl;
        }
        catch (Exception ex)
        {
            // Non-fatal: fall back to the original image as its own thumbnail so the
            // cover still renders (just at full resolution) instead of breaking.
            logger.LogWarning(ex, "Failed to generate thumbnail from URL {Url}; using original.", imageUrl);
            return imageUrl;
        }
    }
}
