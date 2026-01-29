using System.Text.Json.Serialization;

namespace InstructorService.Commands;

/// <summary>
/// Command to create a new Instructor.
/// Consumed from Service Bus queue "identity-commands".
/// </summary>
public class CreateInstructorCommand
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

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

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
