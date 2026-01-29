using System.Text.Json.Serialization;

namespace Shared.Events.Instructors;

/// <summary>
/// Event published when an Instructor is updated.
/// </summary>
public class InstructorUpdated : EventEnvelope<InstructorUpdatedData>
{
    public override string EventType => "InstructorUpdated";

    public InstructorUpdated()
    {
        Producer = "InstructorService";
    }
}

/// <summary>
/// Data payload for InstructorUpdated event.
/// </summary>
public class InstructorUpdatedData
{
    [JsonPropertyName("instructorId")]
    public int InstructorId { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
