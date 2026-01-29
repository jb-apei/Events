using System.Text.Json.Serialization;

namespace Shared.Events.Students;

/// <summary>
/// Event published when Student information is updated.
/// </summary>
public class StudentUpdated : EventEnvelope<StudentUpdatedData>
{
    public override string EventType => "StudentUpdated";

    public StudentUpdated()
    {
        Producer = "StudentService";
    }
}

/// <summary>
/// Data payload for StudentUpdated event.
/// </summary>
public class StudentUpdatedData
{
    [JsonPropertyName("studentId")]
    public int StudentId { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("expectedGraduationDate")]
    public DateTime? ExpectedGraduationDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
