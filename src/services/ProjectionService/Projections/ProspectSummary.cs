using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectionService.Projections;

/// <summary>
/// Read model projection for Prospect list queries.
/// Denormalized view optimized for fast reads.
/// Built from ProspectCreated and ProspectUpdated events.
/// </summary>
[Table("ProspectSummary")]
public class ProspectSummary
{
    /// <summary>
    /// Prospect identifier (from domain model).
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProspectId { get; set; }

    /// <summary>
    /// Prospect's first name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Prospect's last name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Prospect's email address.
    /// Indexed for email lookups.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Full address (concatenated for display).
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Current status of the prospect.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "New";

    /// <summary>
    /// Lead source.
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Notes or comments about the prospect.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// UTC timestamp when prospect was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when prospect was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency.
    /// Incremented on each update.
    /// </summary>
    public int Version { get; set; }
}
