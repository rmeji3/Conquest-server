using Xunit;
using Ping.Services.Admin;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ping.Tests.Services;

public class MinVersionServiceTests : IDisposable
{
    private readonly string _filePath;
    private readonly MinVersionService _service;

    public MinVersionServiceTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"minversion_test_{Guid.NewGuid()}.json");
        _service = new MinVersionService(_filePath);
    }

    [Fact]
    public async Task GetMinVersionAsync_WhenNoFileExists_ReturnsNulls()
    {
        var config = await _service.GetMinVersionAsync();

        Assert.Null(config.MinVersion);
        Assert.Null(config.Message);
    }

    [Fact]
    public async Task SetMinVersionAsync_ThenGet_RoundTripsValues()
    {
        await _service.SetMinVersionAsync("2.1.0", "Please update to continue using Ping.");

        var config = await _service.GetMinVersionAsync();

        Assert.Equal("2.1.0", config.MinVersion);
        Assert.Equal("Please update to continue using Ping.", config.Message);
    }

    [Fact]
    public async Task SetMinVersionAsync_WithNull_ClearsConfig()
    {
        await _service.SetMinVersionAsync("2.1.0", "Update now");
        await _service.SetMinVersionAsync(null, null);

        var config = await _service.GetMinVersionAsync();

        Assert.Null(config.MinVersion);
        Assert.Null(config.Message);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
