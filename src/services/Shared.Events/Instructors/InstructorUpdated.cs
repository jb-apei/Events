using System.Text.Json.Serialization;

namespace Shared.Events.Instructors;

/// <summary>
/// Event published when Instructor information is updated.
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
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("changedFields")]
    public List<string>? ChangedFields { get; set; }
}
