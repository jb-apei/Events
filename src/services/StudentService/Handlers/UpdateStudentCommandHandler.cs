using Microsoft.EntityFrameworkCore;
using StudentService.Commands;
using StudentService.Domain;
using StudentService.Infrastructure;
using Shared.Events.Students;
using System.Text.Json;

namespace StudentService.Handlers;

/// <summary>
/// Handles UpdateStudentCommand using transactional outbox pattern.
/// </summary>
public class UpdateStudentCommandHandler
{
    private readonly StudentDbContext _dbContext;
    private readonly ILogger<UpdateStudentCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public UpdateStudentCommandHandler(
        StudentDbContext dbContext,
        ILogger<UpdateStudentCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(UpdateStudentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling UpdateStudentCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, StudentId={StudentId}",
            command.CommandId, command.CorrelationId, command.StudentId);

        // Find student
        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken);

        if (student == null)
        {
            _logger.LogWarning("Student not found: StudentId={StudentId}", command.StudentId);
            return Result<int>.Failure($"Student with ID {command.StudentId} not found.");
        }

        // Check for duplicate email (if email is being changed)
        if (student.Email != command.Email.ToLowerInvariant())
        {
            var existingStudent = await _dbContext.Students
                .FirstOrDefaultAsync(s => s.Email == command.Email.ToLowerInvariant() && s.Id != command.StudentId,
                    cancellationToken);

            if (existingStudent != null)
            {
                _logger.LogWarning("Email {Email} is already used by another student (Id={OtherStudentId})",
                    command.Email, existingStudent.Id);
                return Result<int>.Failure($"Email {command.Email} is already in use.");
            }
        }

        // Update student aggregate
        var updateResult = student.Update(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.ExpectedGraduationDate,
            command.Notes);

        if (!updateResult.IsSuccess)
        {
            _logger.LogWarning("Student validation failed: {Errors}",
                string.Join(", ", updateResult.Errors));
            return Result<int>.Failure(updateResult.Errors);
        }

        // Transactional outbox pattern: Update entity + save event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Update student entity
            _dbContext.Students.Update(student);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var studentUpdatedEvent = new StudentUpdated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"student/{student.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new StudentUpdatedData
                {
                    StudentId = student.Id,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    Phone = student.Phone,
                    Status = student.Status,
                    ExpectedGraduationDate = student.ExpectedGraduationDate,
                    Notes = student.Notes,
                    UpdatedAt = student.UpdatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = studentUpdatedEvent.EventId,
                EventType = studentUpdatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(studentUpdatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated Student: StudentId={StudentId}, EventId={EventId}",
                student.Id, studentUpdatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(studentUpdatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", studentUpdatedEvent.EventId);
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
            _logger.LogError(ex, "Failed to update student: {Error}", ex.Message);
            return Result<int>.Failure("An error occurred while updating the student.");
        }
    }
}
