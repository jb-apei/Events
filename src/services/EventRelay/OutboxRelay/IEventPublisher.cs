using Shared.Events;

namespace EventRelay.OutboxRelay;

/// <summary>
/// Interface for publishing events to Azure Event Grid.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to the appropriate Event Grid topic based on event type.
    /// </summary>
    /// <param name="eventEnvelope">The event to publish</param>
    /// <param name="eventType">The type of event (for routing to correct topic)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if published successfully, false otherwise</returns>
    Task<bool> PublishAsync(EventEnvelope eventEnvelope, string eventType, CancellationToken cancellationToken = default);
}
