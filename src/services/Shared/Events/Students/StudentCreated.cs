using System.Text.Json.Serialization;

namespace Shared.Events.Students;

/// <summary>
/// Event published when a new Student is created.
/// </summary>
public class StudentCreated : EventEnvelope<StudentCreatedData>
{
    public override string EventType => "StudentCreated";

    public StudentCreated()
    {
        Producer = "StudentService";
    }
}

/// <summary>
/// Data payload for StudentCreated event.
/// </summary>
public class StudentCreatedData
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

    [JsonPropertyName("studentNumber")]
    public string StudentNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("enrollmentDate")]
    public DateTime EnrollmentDate { get; set; }

    [JsonPropertyName("expectedGraduationDate")]
    public DateTime? ExpectedGraduationDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
