using System.Text.Json.Serialization;

namespace Shared.Events.Instructors;

/// <summary>
/// Event published when an Instructor is deactivated (e.g., resignation, termination).
/// </summary>
public class InstructorDeactivated : EventEnvelope<InstructorDeactivatedData>
{
    public override string EventType => "InstructorDeactivated";

    public InstructorDeactivated()
    {
        Producer = "InstructorService";
    }
}

/// <summary>
/// Data payload for InstructorDeactivated event.
/// </summary>
public class InstructorDeactivatedData
{
    [JsonPropertyName("instructorId")]
    public int InstructorId { get; set; }

    [JsonPropertyName("deactivationReason")]
    public string? DeactivationReason { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; set; }

    [JsonPropertyName("deactivatedAt")]
    public DateTime DeactivatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("deactivatedBy")]
    public string? DeactivatedBy { get; set; }
}
