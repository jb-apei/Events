using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectionService.Data;

/// <summary>
/// Inbox pattern implementation for event idempotency.
/// Tracks processed events to prevent duplicate processing.
/// </summary>
[Table("Inbox")]
public class InboxMessage
{
    /// <summary>
    /// Unique event identifier from the event envelope.
    /// Primary key for deduplication.
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Type of event (e.g., "ProspectCreated").
    /// Used for troubleshooting and analytics.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the event was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The aggregate subject (e.g., "prospect/123").
    /// Useful for querying processing history.
    /// </summary>
    [MaxLength(200)]
    public string? Subject { get; set; }
}
