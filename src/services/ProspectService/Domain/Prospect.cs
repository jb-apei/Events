namespace ProspectService.Domain;

/// <summary>
/// Prospect aggregate root representing a potential student.
/// </summary>
public class Prospect
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Source { get; private set; }
    public string Status { get; private set; } = ProspectStatus.New;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires parameterless constructor
    private Prospect() { }

    /// <summary>
    /// Creates a new Prospect with validation.
    /// </summary>
    public static Result<Prospect> Create(
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? source = null,
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

        if (validationErrors.Any())
            return Result<Prospect>.Failure(validationErrors);

        var prospect = new Prospect
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            Source = source?.Trim(),
            Status = ProspectStatus.New,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Result<Prospect>.Success(prospect);
    }

    /// <summary>
    /// Updates the prospect with validation.
    /// </summary>
    public Result<Prospect> Update(
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? source = null,
        string status = ProspectStatus.New,
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

        if (!ProspectStatus.IsValid(status))
            validationErrors.Add($"Invalid status. Must be one of: {string.Join(", ", ProspectStatus.ValidStatuses)}");

        if (validationErrors.Any())
            return Result<Prospect>.Failure(validationErrors);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        Source = source?.Trim();
        Status = status;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result<Prospect>.Success(this);
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
/// Valid prospect statuses.
/// </summary>
public static class ProspectStatus
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Converted = "Converted";
    public const string Lost = "Lost";

    public static readonly string[] ValidStatuses = { New, Contacted, Qualified, Converted, Lost };

    public static bool IsValid(string status) => ValidStatuses.Contains(status);
}
