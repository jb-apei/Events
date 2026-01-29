using StudentService.Domain;

namespace StudentService.Models;

/// <summary>
/// Maps between domain entities and DTOs.
/// </summary>
public static class StudentMapper
{
    /// <summary>
    /// Converts Student domain entity to StudentDto for API responses.
    /// </summary>
    public static StudentDto ToDto(this Student student)
    {
        return new StudentDto
        {
            StudentId = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            Phone = student.Phone,
            StudentNumber = student.StudentNumber,
            Status = student.Status,
            EnrollmentDate = student.EnrollmentDate,
            ExpectedGraduationDate = student.ExpectedGraduationDate,
            Notes = student.Notes,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a collection of Student entities to DTOs.
    /// </summary>
    public static List<StudentDto> ToDtoList(this IEnumerable<Student> students)
    {
        return students.Select(s => s.ToDto()).ToList();
    }
}
