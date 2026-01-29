using System.Text.Json.Serialization;

namespace Shared.Events.Instructors;

/// <summary>
/// Event published when an Instructor is deactivated.
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

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("deactivatedAt")]
    public DateTime DeactivatedAt { get; set; }
}
