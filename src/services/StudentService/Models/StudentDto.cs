using System.Text.Json.Serialization;

namespace StudentService.Models;

/// <summary>
/// Data Transfer Object for Student entity.
/// Used for API responses to ensure consistent property naming with frontend.
/// </summary>
public class StudentDto
{
    /// <summary>
    /// Unique identifier for the student (mapped from Id).
    /// </summary>
    [JsonPropertyName("studentId")]
    public int StudentId { get; set; }

    /// <summary>
    /// Student's first name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Student's last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Student's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Unique student number.
    /// </summary>
    [JsonPropertyName("studentNumber")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Student status (Active, Inactive, Graduated, Withdrawn).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Date the student enrolled.
    /// </summary>
    [JsonPropertyName("enrollmentDate")]
    public DateTime EnrollmentDate { get; set; }

    /// <summary>
    /// Expected graduation date.
    /// </summary>
    [JsonPropertyName("expectedGraduationDate")]
    public DateTime? ExpectedGraduationDate { get; set; }

    /// <summary>
    /// Optional notes about the student.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// When the student was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the student was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
