using System.Text.Json.Serialization;

namespace Shared.Events;

/// <summary>
/// Base event envelope following CloudEvents v1.0 specification with additional tracking fields.
/// All domain events MUST inherit from this base class to ensure consistent event metadata.
/// </summary>
public abstract class EventEnvelope
{
    /// <summary>
    /// Unique identifier for this event instance. Used for idempotency/deduplication.
    /// </summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of the event (e.g., "ProspectCreated", "ProspectUpdated").
    /// Used for routing and deserialization.
    /// </summary>
    [JsonPropertyName("eventType")]
    public abstract string EventType { get; }

    /// <summary>
    /// Schema version for backward compatibility (e.g., "1.0", "2.1").
    /// Increment for breaking changes, use for migration logic.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// UTC timestamp when the event occurred. Must be in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Name of the service that produced this event (e.g., "ProspectService").
    /// Used for troubleshooting and distributed tracing.
    /// </summary>
    [JsonPropertyName("producer")]
    public string Producer { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID to trace the entire request flow across services.
    /// Passed from incoming HTTP headers or generated for new operations.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Causation ID - the ID of the command or event that caused this event.
    /// Forms a parent-child relationship for event chains.
    /// </summary>
    [JsonPropertyName("causationId")]
    public string? CausationId { get; set; }

    /// <summary>
    /// Subject identifies the aggregate instance (e.g., "prospect/12345").
    /// Used for partitioning and filtering in Event Grid.
    /// </summary>
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID for multi-tenant scenarios.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// W3C Trace Context traceparent header for distributed tracing.
    /// Format: version-trace-id-span-id-flags
    /// </summary>
    [JsonPropertyName("traceparent")]
    public string? TraceParent { get; set; }
}

/// <summary>
/// Typed event envelope with strongly-typed data payload.
/// Use this for all domain events to ensure type safety.
/// </summary>
/// <typeparam name="TData">The event-specific data type</typeparam>
public abstract class EventEnvelope<TData> : EventEnvelope where TData : class
{
    /// <summary>
    /// The event-specific payload containing business data.
    /// </summary>
    [JsonPropertyName("data")]
    public TData Data { get; set; } = default!;
}
