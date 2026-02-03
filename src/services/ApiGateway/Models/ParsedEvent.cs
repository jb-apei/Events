using System.Text.Json;

namespace ApiGateway.Models;

public class ParsedEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset EventTime { get; set; }
    public JsonElement Data { get; set; }
}
