using System.Text.Json.Serialization;

namespace Shared.Events.Instructors;

/// <summary>
/// Event published when a new Instructor is created.
/// </summary>
public class InstructorCreated : EventEnvelope<InstructorCreatedData>
{
    public override string EventType => "InstructorCreated";

    public InstructorCreated()
    {
        Producer = "InstructorService";
    }
}

/// <summary>
/// Data payload for InstructorCreated event.
/// </summary>
public class InstructorCreatedData
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

    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("hireDate")]
    public DateTime HireDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
