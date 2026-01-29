using Microsoft.EntityFrameworkCore;
using StudentService.Commands;
using StudentService.Domain;
using StudentService.Infrastructure;
using Shared.Events;
using Shared.Events.Students;
using System.Text.Json;

namespace StudentService.Handlers;

/// <summary>
/// Handles CreateStudentCommand using transactional outbox pattern.
/// </summary>
public class CreateStudentCommandHandler
{
    private readonly StudentDbContext _dbContext;
    private readonly ILogger<CreateStudentCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CreateStudentCommandHandler(
        StudentDbContext dbContext,
        ILogger<CreateStudentCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CreateStudentCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, Email={Email}",
            command.CommandId, command.CorrelationId, command.Email);

        // Check for duplicate email
        var existingStudentByEmail = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (existingStudentByEmail != null)
        {
            _logger.LogWarning("Student with email {Email} already exists (Id={StudentId})",
                command.Email, existingStudentByEmail.Id);
            return Result<int>.Failure($"A student with email {command.Email} already exists.");
        }

        // Check for duplicate student number
        var existingStudentByNumber = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == command.StudentNumber, cancellationToken);

        if (existingStudentByNumber != null)
        {
            _logger.LogWarning("Student with number {StudentNumber} already exists (Id={StudentId})",
                command.StudentNumber, existingStudentByNumber.Id);
            return Result<int>.Failure($"A student with number {command.StudentNumber} already exists.");
        }

        // Create student aggregate
        var studentResult = Student.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.StudentNumber,
            command.EnrollmentDate,
            command.ExpectedGraduationDate,
            command.Notes);

        if (!studentResult.IsSuccess)
        {
            _logger.LogWarning("Student validation failed: {Errors}",
                string.Join(", ", studentResult.Errors));
            return Result<int>.Failure(studentResult.Errors);
        }

        var student = studentResult.Value!;

        // Transactional outbox pattern: Save entity + event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Save student entity
            await _dbContext.Students.AddAsync(student, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var studentCreatedEvent = new StudentCreated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"student/{student.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new StudentCreatedData
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
                    CreatedAt = student.CreatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = studentCreatedEvent.EventId,
                EventType = studentCreatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(studentCreatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created Student: StudentId={StudentId}, EventId={EventId}",
                student.Id, studentCreatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(studentCreatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", studentCreatedEvent.EventId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to push event to ApiGateway: {StatusCode}", response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error pushing event to ApiGateway (development mode)");
                    }
                });
            }

            return Result<int>.Success(student.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create student: {Error}", ex.Message);
            return Result<int>.Failure("An error occurred while creating the student.");
        }
    }
}
