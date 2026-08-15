using Xunit;
using Moq;
using Ping.Data.App;
using Ping.Dtos.Pings;
using Ping.Models.Pings;
using Ping.Services.AI;
using Ping.Services.Background;
using Ping.Services.Follows;
using Ping.Services.Google;
using Ping.Services.Moderation;
using Ping.Services.Pings;
using Ping.Services.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ping.Tests.Services;

/// <summary>
/// A verified ping must only lose its badge when the user actually renames it away from
/// the official Google name. The semantic check is non-deterministic, so these tests pin
/// that an unchanged name never reaches it.
/// </summary>
public class PingVerificationTests : IDisposable
{
    private const string PlaceId = "google-place-123";
    private const string OfficialName = "Blue Bottle Coffee";
    private const string UserId = "user-1";

    private readonly AppDbContext _db;
    private readonly Mock<IPingNameService> _pingNameService = new();
    private readonly Mock<ISemanticService> _semanticService = new();
    private readonly Mock<IModerationService> _moderationService = new();
    private readonly Mock<IRedisService> _redis = new();
    private readonly PingService _service;

    public PingVerificationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PingVerificationDb_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _redis.Setup(r => r.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(1);
        _moderationService.Setup(m => m.CheckContentAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModerationResult(false, null));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PingCreationLimitPerDay"] = "100"
            })
            .Build();

        _service = new PingService(
            _db,
            _redis.Object,
            config,
            _pingNameService.Object,
            new Mock<IFollowService>().Object,
            _moderationService.Object,
            _semanticService.Object,
            Channel.CreateUnbounded<PingGenreJob>().Writer,
            new Mock<IServiceScopeFactory>().Object,
            new Mock<ILogger<PingService>>().Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupGooglePlace(string? address = null) =>
        _pingNameService.Setup(s => s.GetGooglePlaceByIdAsync(PlaceId))
            .ReturnsAsync(new GooglePingInfo(OfficialName, address, 41.8781, -87.6298));

    private static UpsertPingDto VerifiedDto(string name, string? address = "123 Main St, Chicago, IL 60601, USA") =>
        new(name, address, 41.8781, -87.6298, PingVisibility.Public, PingType.Verified, null, PlaceId);

    [Fact]
    public async Task CreatePing_KeepsVerified_WithoutCallingSemanticService_WhenNameMatchesGoogle()
    {
        SetupGooglePlace();

        var result = await _service.CreatePingAsync(VerifiedDto(OfficialName), UserId);

        Assert.Equal(PingType.Verified, result.Type);
        Assert.Equal(PlaceId, result.GooglePlaceId);
        _semanticService.Verify(
            s => s.VerifyPlaceNameMatchAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePing_KeepsVerified_WhenNameMatchesButSemanticServiceWouldSayNo()
    {
        SetupGooglePlace();
        _semanticService
            .Setup(s => s.VerifyPlaceNameMatchAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.CreatePingAsync(VerifiedDto($"  {OfficialName.ToUpperInvariant()}  "), UserId);

        Assert.Equal(PingType.Verified, result.Type);
        Assert.Equal(PlaceId, result.GooglePlaceId);
    }

    [Fact]
    public async Task CreatePing_DowngradesToCustom_WhenNameDiffersAndSemanticServiceRejects()
    {
        SetupGooglePlace();
        _semanticService
            .Setup(s => s.VerifyPlaceNameMatchAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.CreatePingAsync(VerifiedDto("My Secret Hideout"), UserId);

        Assert.Equal(PingType.Custom, result.Type);
        Assert.Null(result.GooglePlaceId);
    }

    [Fact]
    public async Task CreatePing_FillsBlankAddress_FromGoogleFormattedAddress()
    {
        SetupGooglePlace("1250 W 119th St, Chicago, IL 60643, USA");

        var result = await _service.CreatePingAsync(VerifiedDto(OfficialName, address: ""), UserId);

        Assert.Equal("1250 W 119th St, Chicago, IL 60643, USA", result.Address);
    }

    [Fact]
    public async Task CreatePing_DoesNotClobber_AClientSuppliedAddress()
    {
        SetupGooglePlace("1250 W 119th St, Chicago, IL 60643, USA");

        var result = await _service.CreatePingAsync(
            VerifiedDto(OfficialName, address: "999 Client Ave, Chicago, IL 60601, USA"), UserId);

        Assert.Equal("999 Client Ave, Chicago, IL 60601, USA", result.Address);
    }

    [Fact]
    public async Task UpdatePing_KeepsVerified_WithoutCallingSemanticService_WhenRenamedBackToGoogleName()
    {
        SetupGooglePlace();
        var ping = new Models.Pings.Ping
        {
            Name = "Some Other Name",
            Address = "123 Main St, Chicago, IL 60601, USA",
            Latitude = 41.8781,
            Longitude = -87.6298,
            OwnerUserId = UserId,
            Visibility = PingVisibility.Public,
            Type = PingType.Verified,
            GooglePlaceId = PlaceId,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Pings.Add(ping);
        await _db.SaveChangesAsync();

        var result = await _service.UpdatePingAsync(
            ping.Id, new UpdatePingDto { Name = OfficialName }, UserId);

        Assert.Equal(PingType.Verified, result.Type);
        Assert.Equal(PlaceId, result.GooglePlaceId);
        _semanticService.Verify(
            s => s.VerifyPlaceNameMatchAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
