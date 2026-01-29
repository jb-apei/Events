using InstructorService.Domain;

namespace InstructorService.Models;

/// <summary>
/// Maps between domain entities and DTOs.
/// </summary>
public static class InstructorMapper
{
    /// <summary>
    /// Converts Instructor domain entity to InstructorDto for API responses.
    /// </summary>
    public static InstructorDto ToDto(this Instructor instructor)
    {
        return new InstructorDto
        {
            InstructorId = instructor.Id,
            FirstName = instructor.FirstName,
            LastName = instructor.LastName,
            Email = instructor.Email,
            Phone = instructor.Phone,
            EmployeeNumber = instructor.EmployeeNumber,
            Status = instructor.Status,
            Specialization = instructor.Specialization,
            HireDate = instructor.HireDate,
            Notes = instructor.Notes,
            CreatedAt = instructor.CreatedAt,
            UpdatedAt = instructor.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a collection of Instructor entities to DTOs.
    /// </summary>
    public static List<InstructorDto> ToDtoList(this IEnumerable<Instructor> instructors)
    {
        return instructors.Select(i => i.ToDto()).ToList();
    }
}
