using System.Text.Json.Serialization;

namespace ApiGateway.Models;

/// <summary>
/// Command message envelope for Service Bus
/// </summary>
public class CommandMessage<TPayload>
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("commandType")]
    public string CommandType { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("payload")]
    public TPayload? Payload { get; set; }
}
