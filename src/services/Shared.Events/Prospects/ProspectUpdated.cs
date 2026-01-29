using System.Text.Json.Serialization;

namespace Shared.Events.Prospects;

/// <summary>
/// Event published when a Prospect is updated.
/// </summary>
public class ProspectUpdated : EventEnvelope<ProspectUpdatedData>
{
    public override string EventType => "ProspectUpdated";

    public ProspectUpdated()
    {
        Producer = "ProspectService";
    }
}

/// <summary>
/// Data payload for ProspectUpdated event.
/// Contains the full current state after the update.
/// </summary>
public class ProspectUpdatedData
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
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Prospect's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Source of the prospect.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Current status of the prospect.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the prospect.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// UTC timestamp when the prospect was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fields that were changed in this update (for audit/tracking).
    /// Optional but recommended for fine-grained change tracking.
    /// </summary>
    [JsonPropertyName("changedFields")]
    public List<string>? ChangedFields { get; set; }
}
