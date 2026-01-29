namespace ProspectService.Infrastructure;

/// <summary>
/// Outbox message for transactional outbox pattern.
/// Stores events that need to be published to Event Grid.
/// </summary>
public class OutboxMessage
{
    public long Id { get; set; }

    /// <summary>
    /// Unique event identifier (for idempotency).
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the event (e.g., "ProspectCreated").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized event payload (full EventEnvelope).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the event has been published to Event Grid.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// When the event was published (null if not published yet).
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}
