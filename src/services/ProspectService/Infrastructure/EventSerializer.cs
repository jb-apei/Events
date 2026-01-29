using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProspectService.Infrastructure;

/// <summary>
/// Helper class for serializing events with proper JSON configuration.
/// Ensures consistent serialization across the application.
/// </summary>
public static class EventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an event to JSON using consistent options.
    /// </summary>
    public static string Serialize<T>(T eventData)
    {
        return JsonSerializer.Serialize(eventData, Options);
    }

    /// <summary>
    /// Deserializes JSON to an event object.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
