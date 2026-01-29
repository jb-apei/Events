using System.Text.Json.Serialization;

namespace InstructorService.Models;

/// <summary>
/// Data Transfer Object for Instructor entity.
/// Used for API responses to ensure consistent property naming with frontend.
/// </summary>
public class InstructorDto
{
    /// <summary>
    /// Unique identifier for the instructor (mapped from Id).
    /// </summary>
    [JsonPropertyName("instructorId")]
    public int InstructorId { get; set; }

    /// <summary>
    /// Instructor's first name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Instructor's last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Instructor's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Employee number (unique identifier).
    /// </summary>
    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Instructor status (Active, Inactive, OnLeave).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Area of specialization (e.g., Mathematics, Computer Science).
    /// </summary>
    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    /// <summary>
    /// Date the instructor was hired.
    /// </summary>
    [JsonPropertyName("hireDate")]
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Optional notes about the instructor.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// When the instructor record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the instructor record was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
