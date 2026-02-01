using EventRelay.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using Shared.Events.Prospects;
using Shared.Events.Students;
using Shared.Events.Instructors;
using System.Text.Json;

namespace EventRelay.OutboxRelay;

/// <summary>
/// Background service that polls the Outbox table and publishes unpublished events to Event Grid.
/// Implements the relay part of the Transactional Outbox pattern.
/// </summary>
public class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;

    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxRelayService started. Polling every {PollingInterval} seconds", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedAny = false;
            try
            {
                processedAny = await ProcessUnpublishedEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unpublished events");
            }

            // If we processed a full batch, run again immediately (throttle slightly to prevent CPU pinning if extremely busy)
            // Otherwise, wait for the polling interval
            if (!processedAny)
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        _logger.LogInformation("OutboxRelayService stopped");
    }

    /// <summary>
    /// Process a batch of unpublished events from the Outbox table.
    /// Returns true if a full batch was processed (indicating potentially more work), false otherwise.
    /// </summary>
    private async Task<bool> ProcessUnpublishedEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // Fetch unpublished events ordered by creation time (FIFO)
        var unpublishedEvents = await dbContext.Outbox
            .Where(e => !e.Published)
            .OrderBy(e => e.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (unpublishedEvents.Count == 0)
        {
            // No events to process
            return false;
        }

        _logger.LogInformation("Processing {EventCount} unpublished events", unpublishedEvents.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var outboxMessage in unpublishedEvents)
        {
            try
            {
                // Deserialize the event envelope from JSON
                var eventEnvelope = DeserializeEvent(outboxMessage.Payload, outboxMessage.EventType);

                if (eventEnvelope == null)
                {
                    _logger.LogError(
                        "Failed to deserialize event {EventId} ({EventType}). Marking as published to skip.",
                        outboxMessage.EventId,
                        outboxMessage.EventType);

                    // Mark as published to prevent infinite retry
                    MarkAsPublished(outboxMessage);
                    failureCount++;
                    continue;
                }

                // Publish to Event Grid
                var published = await eventPublisher.PublishAsync(
                    eventEnvelope,
                    outboxMessage.EventType,
                    cancellationToken);

                if (published)
                {
                    // Mark as published in database
                    MarkAsPublished(outboxMessage);
                    successCount++;

                    _logger.LogDebug(
                        "Event {EventId} ({EventType}) published and marked as complete (correlation: {CorrelationId})",
                        outboxMessage.EventId,
                        outboxMessage.EventType,
                        eventEnvelope.CorrelationId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to publish event {EventId} ({EventType}). Will retry in next cycle.",
                        outboxMessage.EventId,
                        outboxMessage.EventType);

                    failureCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing event {EventId} ({EventType})",
                    outboxMessage.EventId,
                    outboxMessage.EventType);

                failureCount++;
            }
        }

        // Save all changes (mark events as published)
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch complete: {SuccessCount} published, {FailureCount} failed",
            successCount,
            failureCount);

        // Return true if we processed a full batch, suggesting there might be more events
        return unpublishedEvents.Count == _batchSize;
    }

    /// <summary>
    /// Deserialize event JSON to the appropriate EventEnvelope type.
    /// </summary>
    private EventEnvelope? DeserializeEvent(string payload, string eventType)
    {
        try
        {
            // Use EventSerializer from Shared.Events with type-specific deserialization
            return eventType switch
            {
                "ProspectCreated" => EventSerializer.Deserialize<ProspectCreated>(payload),
                "ProspectUpdated" => EventSerializer.Deserialize<ProspectUpdated>(payload),
                "ProspectMerged" => EventSerializer.Deserialize<ProspectMerged>(payload),

                // Student events (when implemented)
                "StudentCreated" => EventSerializer.Deserialize<StudentCreated>(payload),
                "StudentUpdated" => EventSerializer.Deserialize<StudentUpdated>(payload),
                "StudentChanged" => EventSerializer.Deserialize<StudentChanged>(payload),

                // Instructor events (when implemented)
                "InstructorCreated" => EventSerializer.Deserialize<InstructorCreated>(payload),
                "InstructorUpdated" => EventSerializer.Deserialize<InstructorUpdated>(payload),
                "InstructorDeactivated" => EventSerializer.Deserialize<InstructorDeactivated>(payload),

                _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event payload for event type {EventType}", eventType);
            return null;
        }
    }

    /// <summary>
    /// Mark an outbox message as published.
    /// </summary>
    private static void MarkAsPublished(OutboxMessage outboxMessage)
    {
        outboxMessage.Published = true;
        outboxMessage.PublishedAt = DateTime.UtcNow;
    }
}
