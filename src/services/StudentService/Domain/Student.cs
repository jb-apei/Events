namespace StudentService.Domain;

/// <summary>
/// Student aggregate root representing an enrolled student.
/// </summary>
public class Student
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string StudentNumber { get; private set; } = string.Empty;
    public string Status { get; private set; } = StudentStatus.Active;
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? ExpectedGraduationDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires parameterless constructor
    private Student() { }

    /// <summary>
    /// Creates a new Student with validation.
    /// </summary>
    public static Result<Student> Create(
        string firstName,
        string lastName,
        string email,
        string? phone,
        string studentNumber,
        DateTime enrollmentDate,
        DateTime? expectedGraduationDate,
        string? notes = null)
    {
        // Validation
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName))
            validationErrors.Add("First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            validationErrors.Add("Last name is required.");

        if (string.IsNullOrWhiteSpace(email))
            validationErrors.Add("Email is required.");
        else if (!IsValidEmail(email))
            validationErrors.Add("Email format is invalid.");

        if (string.IsNullOrWhiteSpace(studentNumber))
            validationErrors.Add("Student number is required.");

        if (enrollmentDate > DateTime.UtcNow)
            validationErrors.Add("Enrollment date cannot be in the future.");

        if (expectedGraduationDate.HasValue && expectedGraduationDate.Value <= enrollmentDate)
            validationErrors.Add("Expected graduation date must be after enrollment date.");

        if (validationErrors.Any())
            return Result<Student>.Failure(validationErrors);

        var student = new Student
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            StudentNumber = studentNumber.Trim(),
            Status = StudentStatus.Active,
            EnrollmentDate = enrollmentDate,
            ExpectedGraduationDate = expectedGraduationDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Result<Student>.Success(student);
    }

    /// <summary>
    /// Updates the student with validation.
    /// </summary>
    public Result<Student> Update(
        string firstName,
        string lastName,
        string email,
        string? phone,
        DateTime? expectedGraduationDate,
        string? notes = null)
    {
        // Validation
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName))
            validationErrors.Add("First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            validationErrors.Add("Last name is required.");

        if (string.IsNullOrWhiteSpace(email))
            validationErrors.Add("Email is required.");
        else if (!IsValidEmail(email))
            validationErrors.Add("Email format is invalid.");

        if (expectedGraduationDate.HasValue && expectedGraduationDate.Value <= EnrollmentDate)
            validationErrors.Add("Expected graduation date must be after enrollment date.");

        if (validationErrors.Any())
            return Result<Student>.Failure(validationErrors);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        ExpectedGraduationDate = expectedGraduationDate;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result<Student>.Success(this);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Valid student statuses.
/// </summary>
public static class StudentStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Graduated = "Graduated";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] ValidStatuses = { Active, Inactive, Graduated, Withdrawn };

    public static bool IsValid(string status) => ValidStatuses.Contains(status);
}
