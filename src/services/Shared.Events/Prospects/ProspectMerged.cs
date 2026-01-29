using System.Text.Json.Serialization;

namespace Shared.Events.Prospects;

/// <summary>
/// Event published when two Prospect records are merged into one.
/// The source prospect is merged into the target prospect and then deactivated.
/// </summary>
public class ProspectMerged : EventEnvelope<ProspectMergedData>
{
    public override string EventType => "ProspectMerged";

    public ProspectMerged()
    {
        Producer = "ProspectService";
    }
}

/// <summary>
/// Data payload for ProspectMerged event.
/// </summary>
public class ProspectMergedData
{
    /// <summary>
    /// ID of the target prospect (the one that survives).
    /// </summary>
    [JsonPropertyName("targetProspectId")]
    public int TargetProspectId { get; set; }

    /// <summary>
    /// ID of the source prospect (the one being merged and deactivated).
    /// </summary>
    [JsonPropertyName("sourceProspectId")]
    public int SourceProspectId { get; set; }

    /// <summary>
    /// Reason for the merge (e.g., "Duplicate email detected").
    /// </summary>
    [JsonPropertyName("mergeReason")]
    public string? MergeReason { get; set; }

    /// <summary>
    /// UTC timestamp when the merge occurred.
    /// </summary>
    [JsonPropertyName("mergedAt")]
    public DateTime MergedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User or system that performed the merge.
    /// </summary>
    [JsonPropertyName("mergedBy")]
    public string? MergedBy { get; set; }
}
