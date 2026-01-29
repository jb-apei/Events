using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Events;

/// <summary>
/// Helper class for serializing and deserializing events with consistent settings.
/// </summary>
public static class EventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serialize an event envelope to JSON string.
    /// </summary>
    public static string Serialize<T>(T eventEnvelope) where T : EventEnvelope
    {
        return JsonSerializer.Serialize(eventEnvelope, Options);
    }

    /// <summary>
    /// Deserialize a JSON string to an event envelope.
    /// </summary>
    public static T? Deserialize<T>(string json) where T : EventEnvelope
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Deserialize a JSON string to a base EventEnvelope to inspect metadata before deserializing to specific type.
    /// </summary>
    public static EventEnvelope? DeserializeBase(string json)
    {
        // This requires a concrete implementation - will need a DynamicEventEnvelope class
        return JsonSerializer.Deserialize<DynamicEventEnvelope>(json, Options);
    }
}

/// <summary>
/// Concrete implementation of EventEnvelope for dynamic deserialization when event type is unknown.
/// </summary>
internal class DynamicEventEnvelope : EventEnvelope
{
    [JsonPropertyName("eventType")]
    public string EventTypeName { get; set; } = string.Empty;

    public override string EventType => EventTypeName;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
