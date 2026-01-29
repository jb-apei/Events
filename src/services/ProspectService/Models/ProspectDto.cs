using System.Text.Json.Serialization;

namespace ProspectService.Models;

/// <summary>
/// Data Transfer Object for Prospect entity.
/// Used for API responses to ensure consistent property naming with frontend.
/// </summary>
public class ProspectDto
{
    /// <summary>
    /// Unique identifier for the prospect (mapped from Id).
    /// Frontend expects this as a string.
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
    /// Prospect status (New, Contacted, Qualified, etc.).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the prospect.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// When the prospect was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the prospect was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
