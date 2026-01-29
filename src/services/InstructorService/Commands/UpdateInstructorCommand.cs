using System.Text.Json.Serialization;

namespace InstructorService.Commands;

/// <summary>
/// Command to update an existing Instructor.
/// Consumed from Service Bus queue "identity-commands".
/// </summary>
public class UpdateInstructorCommand
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

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

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
