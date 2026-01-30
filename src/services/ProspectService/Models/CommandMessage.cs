using System.Text.Json;
using System.Text.Json.Serialization;
using ProspectService.Commands;

namespace ProspectService.Models;

/// <summary>
/// Command message envelope received from Service Bus.
/// Matches the structure sent by API Gateway.
/// </summary>
public class CommandMessage
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    [JsonPropertyName("commandType")]
    public string CommandType { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    /// <summary>
    /// Extracts the payload as CreateProspectCommand
    /// </summary>
    public CreateProspectCommand? AsCreateProspectCommand()
    {
        if (Payload == null) return null;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var command = JsonSerializer.Deserialize<CreateProspectCommand>(Payload.Value.GetRawText(), options);
        if (command != null)
        {
            command.CommandId = CommandId;
            command.CorrelationId = CorrelationId;
        }
        return command;
    }

    /// <summary>
    /// Extracts the payload as UpdateProspectCommand
    /// </summary>
    public UpdateProspectCommand? AsUpdateProspectCommand()
    {
        if (Payload == null) return null;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var command = JsonSerializer.Deserialize<UpdateProspectCommand>(Payload.Value.GetRawText(), options);
        if (command != null)
        {
            command.CommandId = CommandId;
            command.CorrelationId = CorrelationId;
        }
        return command;
    }
}
