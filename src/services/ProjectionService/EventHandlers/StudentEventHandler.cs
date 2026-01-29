using Shared.Events;

namespace ProjectionService.EventHandlers;

/// <summary>
/// Stub handler for Student domain events (future implementation).
/// </summary>
public class StudentEventHandler
{
    private readonly ILogger<StudentEventHandler> _logger;

    public StudentEventHandler(ILogger<StudentEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles StudentCreated event (future implementation).
    /// </summary>
    public Task HandleStudentCreatedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StudentEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles StudentUpdated event (future implementation).
    /// </summary>
    public Task HandleStudentUpdatedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StudentEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles StudentChanged event (future implementation).
    /// </summary>
    public Task HandleStudentChangedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StudentEventHandler not implemented yet. Ignoring event {EventId} ({EventType})",
            eventEnvelope.EventId,
            eventEnvelope.EventType);

        return Task.CompletedTask;
    }
}
