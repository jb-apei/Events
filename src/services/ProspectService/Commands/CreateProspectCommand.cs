using System.Text.Json.Serialization;

namespace ProspectService.Commands;

/// <summary>
/// Command to create a new Prospect.
/// Consumed from Service Bus queue "identity-commands".
/// </summary>
public class CreateProspectCommand
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

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
