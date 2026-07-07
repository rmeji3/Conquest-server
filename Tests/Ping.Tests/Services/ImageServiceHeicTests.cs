using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ping.Services.Images;
using Ping.Services.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ping.Tests.Services;

/// <summary>
/// HEIC handling: ImageSharp has no HEIC decoder, so these paths must fall back to
/// Magick.NET and still produce a real WebP thumbnail. Uses a genuine HEIC fixture
/// (Assets/sample.heic) — legacy review originals in prod are iPhone HEICs.
/// </summary>
public class ImageServiceHeicTests
{
    private static readonly string HeicPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sample.heic");

    private static (ImageService Service, Dictionary<string, byte[]> Uploads) CreateService(HttpMessageHandler? handler = null)
    {
        var uploads = new Dictionary<string, byte[]>();
        var storage = new Mock<IStorageService>();
        storage
            .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .Returns(async (IFormFile file, string key) =>
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                uploads[key] = ms.ToArray();
                return $"https://cdn.test/{key}";
            });

        var httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        var service = new ImageService(storage.Object, httpClient, Mock.Of<ILogger<ImageService>>());
        return (service, uploads);
    }

    private static IFormFile HeicFormFile()
    {
        var bytes = File.ReadAllBytes(HeicPath);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "photo.heic")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/heic"
        };
    }

    [Fact]
    public async Task ProcessAndUploadImageAsync_Heic_ProducesRealWebpThumbnail()
    {
        var (service, uploads) = CreateService();

        var (originalUrl, thumbUrl) = await service.ProcessAndUploadImageAsync(HeicFormFile(), "reviews", "user1");

        Assert.NotEqual(originalUrl, thumbUrl);
        Assert.EndsWith(".webp", thumbUrl);
        Assert.EndsWith(".heic", originalUrl); // raw original is preserved as-is

        var thumbKey = thumbUrl.Replace("https://cdn.test/", "");
        Assert.True(uploads[thumbKey].Length > 0, "thumbnail upload should not be empty");
        // WebP files start with RIFF....WEBP
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(uploads[thumbKey], 0, 4));
        Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(uploads[thumbKey], 8, 4));
    }

    [Fact]
    public async Task GenerateThumbnailFromUrlAsync_Heic_ProducesWebpInsteadOfFallingBack()
    {
        var heicBytes = await File.ReadAllBytesAsync(HeicPath);
        var handler = new StubHandler(heicBytes);
        var (service, uploads) = CreateService(handler);

        var sourceUrl = "https://cdn.test/reviews/user1/legacy_orig.heic";
        var thumbUrl = await service.GenerateThumbnailFromUrlAsync(sourceUrl, "reviews", "user1");

        // On failure this method returns the source URL; success means a new webp key.
        Assert.NotEqual(sourceUrl, thumbUrl);
        Assert.EndsWith(".webp", thumbUrl);

        var thumbKey = thumbUrl.Replace("https://cdn.test/", "");
        Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(uploads[thumbKey], 8, 4));
    }

    private sealed class StubHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
    }
}
