using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models;

public class UpdateProspectRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
