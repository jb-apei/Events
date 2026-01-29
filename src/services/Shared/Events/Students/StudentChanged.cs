using System.Text.Json.Serialization;

namespace Shared.Events.Students;

/// <summary>
/// Event published when a Student's status changes.
/// </summary>
public class StudentChanged : EventEnvelope<StudentChangedData>
{
    public override string EventType => "StudentChanged";

    public StudentChanged()
    {
        Producer = "StudentService";
    }
}

/// <summary>
/// Data payload for StudentChanged event.
/// </summary>
public class StudentChangedData
{
    [JsonPropertyName("studentId")]
    public int StudentId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; set; }
}
