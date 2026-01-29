using System.Text.Json.Serialization;

namespace Shared.Events.Students;

/// <summary>
/// Event published when a Student is updated.
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
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("expectedGraduationDate")]
    public DateTime? ExpectedGraduationDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
