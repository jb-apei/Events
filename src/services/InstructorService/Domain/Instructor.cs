namespace InstructorService.Domain;

/// <summary>
/// Instructor aggregate root representing an employee who teaches courses.
/// </summary>
public class Instructor
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string Status { get; private set; } = InstructorStatus.Active;
    public string? Specialization { get; private set; }
    public DateTime HireDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires parameterless constructor
    private Instructor() { }

    /// <summary>
    /// Creates a new Instructor with validation.
    /// </summary>
    public static Result<Instructor> Create(
        string firstName,
        string lastName,
        string email,
        string employeeNumber,
        DateTime hireDate,
        string? phone = null,
        string? specialization = null,
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

        if (string.IsNullOrWhiteSpace(employeeNumber))
            validationErrors.Add("Employee number is required.");

        if (hireDate > DateTime.UtcNow.Date)
            validationErrors.Add("Hire date cannot be in the future.");

        if (!string.IsNullOrWhiteSpace(specialization) && specialization.Length > 100)
            validationErrors.Add("Specialization must not exceed 100 characters.");

        if (validationErrors.Any())
            return Result<Instructor>.Failure(validationErrors);

        var instructor = new Instructor
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            EmployeeNumber = employeeNumber.Trim(),
            Status = InstructorStatus.Active,
            Specialization = specialization?.Trim(),
            HireDate = hireDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Result<Instructor>.Success(instructor);
    }

    /// <summary>
    /// Updates the instructor with validation.
    /// </summary>
    public Result<Instructor> Update(
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? specialization = null,
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

        if (!string.IsNullOrWhiteSpace(specialization) && specialization.Length > 100)
            validationErrors.Add("Specialization must not exceed 100 characters.");

        if (validationErrors.Any())
            return Result<Instructor>.Failure(validationErrors);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        Specialization = specialization?.Trim();
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result<Instructor>.Success(this);
    }

    /// <summary>
    /// Deactivates the instructor.
    /// </summary>
    public void Deactivate()
    {
        Status = InstructorStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
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
/// Valid instructor statuses.
/// </summary>
public static class InstructorStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string OnLeave = "OnLeave";

    public static readonly string[] ValidStatuses = { Active, Inactive, OnLeave };

    public static bool IsValid(string status) => ValidStatuses.Contains(status);
}
