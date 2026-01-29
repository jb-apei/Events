using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models;

public class CreateProspectRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
