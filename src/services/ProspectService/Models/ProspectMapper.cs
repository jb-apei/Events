using ProspectService.Domain;

namespace ProspectService.Models;

/// <summary>
/// Maps between domain entities and DTOs.
/// </summary>
public static class ProspectMapper
{
    /// <summary>
    /// Converts Prospect domain entity to ProspectDto for API responses.
    /// </summary>
    public static ProspectDto ToDto(this Prospect prospect)
    {
        return new ProspectDto
        {
            ProspectId = prospect.Id,
            FirstName = prospect.FirstName,
            LastName = prospect.LastName,
            Email = prospect.Email,
            Phone = prospect.Phone,
            Status = prospect.Status,
            Notes = prospect.Notes,
            CreatedAt = prospect.CreatedAt,
            UpdatedAt = prospect.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a collection of Prospect entities to DTOs.
    /// </summary>
    public static List<ProspectDto> ToDtoList(this IEnumerable<Prospect> prospects)
    {
        return prospects.Select(p => p.ToDto()).ToList();
    }
}
