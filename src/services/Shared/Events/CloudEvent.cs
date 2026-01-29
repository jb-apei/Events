using System.Text.Json.Serialization;

namespace Shared.Events;

/// <summary>
/// CloudEvents 1.0 specification compliant event envelope.
/// Used for publishing events to Event Grid and ApiGateway.
/// </summary>
public class CloudEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("specversion")]
    public string SpecVersion { get; set; } = "1.0";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("datacontenttype")]
    public string DataContentType { get; set; } = "application/json";

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    // Additional metadata
    [JsonPropertyName("correlationid")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("causationid")]
    public string? CausationId { get; set; }
}
