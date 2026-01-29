using System.Text.Json.Serialization;

namespace Shared.Events.Prospects;

/// <summary>
/// Event published when a new Prospect is created.
/// </summary>
public class ProspectCreated : EventEnvelope<ProspectCreatedData>
{
    public override string EventType => "ProspectCreated";

    public ProspectCreated()
    {
        Producer = "ProspectService";
    }
}

/// <summary>
/// Data payload for ProspectCreated event.
/// </summary>
public class ProspectCreatedData
{
    /// <summary>
    /// Unique identifier for the prospect.
    /// </summary>
    [JsonPropertyName("prospectId")]
    public int ProspectId { get; set; }

    /// <summary>
    /// Prospect's first name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Prospect's last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }= string.Empty;

    /// <summary>
    /// Prospect's email address (must be unique).
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Source of the prospect (e.g., "Website", "Referral", "Marketing Campaign").
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Current status of the prospect (e.g., "New", "Contacted", "Qualified").
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "New";

    /// <summary>
    /// Optional notes about the prospect.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// UTC timestamp when the prospect was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
