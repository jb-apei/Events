using System.Text.Json.Serialization;

namespace Shared.Events.Students;

/// <summary>
/// Event published when a Student's status changes significantly
/// (e.g., enrollment status, graduation, withdrawal).
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

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; set; } = string.Empty;

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("changeReason")]
    public string? ChangeReason { get; set; }

    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("changedBy")]
    public string? ChangedBy { get; set; }
}
