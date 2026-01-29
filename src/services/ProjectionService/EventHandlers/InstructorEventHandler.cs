using Shared.Events;

namespace ProjectionService.EventHandlers;

/// <summary>
/// Stub handler for Instructor domain events (future implementation).
/// </summary>
public class InstructorEventHandler
{
    private readonly ILogger<InstructorEventHandler> _logger;

    public InstructorEventHandler(ILogger<InstructorEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles InstructorCreated event (future implementation).
    /// </summary>
    public Task HandleInstructorCreatedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InstructorEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles InstructorUpdated event (future implementation).
    /// </summary>
    public Task HandleInstructorUpdatedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InstructorEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles InstructorDeactivated event (future implementation).
    /// </summary>
    public Task HandleInstructorDeactivatedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InstructorEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }
}
