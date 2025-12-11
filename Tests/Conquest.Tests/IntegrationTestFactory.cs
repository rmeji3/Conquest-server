using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Conquest.Data.Auth;
using Conquest.Data.App;
using StackExchange.Redis;
using Moq;

namespace Conquest.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Add test configuration for JWT
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsATestSecretKeyForJWTMustBe32CharsLong!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenMinutes"] = "60"
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove the original DbContext registrations
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.RemoveAll<AppDbContext>();
            
            // Add InMemory DbContexts
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestAuthDb_" + Guid.NewGuid().ToString());
            });

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestAppDb_" + Guid.NewGuid().ToString());
            });

            // Replace Redis with Mock (if not already skipped by Program.cs)
            services.RemoveAll<IConnectionMultiplexer>();
            var mockMultiplexer = new Moq.Mock<IConnectionMultiplexer>();
            mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
            services.AddSingleton(mockMultiplexer.Object);

            // Replace IDistributedCache with in-memory version
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Replace IRedisService with Mock
            services.RemoveAll<Conquest.Services.Redis.IRedisService>();
            var mockRedis = new Moq.Mock<Conquest.Services.Redis.IRedisService>();
            mockRedis.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>())).ReturnsAsync(1);
            services.AddScoped(_ => mockRedis.Object);

            // Replace ISemanticService with Mock
            services.RemoveAll<Conquest.Services.AI.ISemanticService>();
            var mockSemantic = new Moq.Mock<Conquest.Services.AI.ISemanticService>();
            mockSemantic.Setup(x => x.FindDuplicateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync((string?)null);
            services.AddScoped(_ => mockSemantic.Object);

            // Replace IModerationService with Mock
            services.RemoveAll<Conquest.Services.Moderation.IModerationService>();
            var mockModeration = new Moq.Mock<Conquest.Services.Moderation.IModerationService>();
            mockModeration.Setup(x => x.CheckContentAsync(It.IsAny<string>())).ReturnsAsync(new Conquest.Services.Moderation.ModerationResult(false, ""));
            services.AddScoped(_ => mockModeration.Object);

            // Add Mock IChatCompletionService
            services.AddSingleton(new Moq.Mock<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>().Object);
        });
    }
}
