using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conquest.Services.Analytics;

public class AnalyticsBackgroundJob(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsBackgroundJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Analytics Background Job starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                    // Compute metrics for today (snapshot)
                    // In a real system, we might want to compute "Yesterday" fully at 00:01
                    // But for "live" dashboarding or simplicity, we can update today's metrics
                    // periodically or once a day.
                    // Let's compute for Today every hour to keep stats somewhat fresh?
                    // Or just once a day? The plan said "ComputeDailyMetricsAsync".
                    // Let's do it for Today.
                    
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    await analytics.ComputeDailyMetricsAsync(today);
                    logger.LogInformation("Computed daily metrics for {Date}", today);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing Analytics Job.");
            }

            // Wait 1 hour before next run
            // await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            // for testing, wait 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
