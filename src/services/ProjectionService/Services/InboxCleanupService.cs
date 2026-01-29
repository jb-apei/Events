using Microsoft.EntityFrameworkCore;
using ProjectionService.Data;

namespace ProjectionService.Services;

/// <summary>
/// Background service to clean up old Inbox entries.
/// Runs periodically to maintain 7-day dedupe window.
/// </summary>
public class InboxCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);

    public InboxCleanupService(
        IServiceProvider serviceProvider,
        ILogger<InboxCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InboxCleanupService started. Will run every {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldInboxEntriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up inbox entries");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("InboxCleanupService stopped");
    }

    private async Task CleanupOldInboxEntriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProjectionDbContext>();

        var cutoffDate = DateTime.UtcNow - _retentionPeriod;

        _logger.LogInformation(
            "Starting inbox cleanup. Deleting entries older than {CutoffDate} (retention: {RetentionDays} days)",
            cutoffDate,
            _retentionPeriod.TotalDays);

        var deletedCount = await dbContext.Inbox
            .Where(i => i.ProcessedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {Count} old inbox entries", deletedCount);
        }
        else
        {
            _logger.LogInformation("No old inbox entries to delete");
        }
    }
}
