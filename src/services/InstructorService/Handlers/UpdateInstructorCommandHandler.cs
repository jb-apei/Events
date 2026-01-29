using Microsoft.EntityFrameworkCore;
using InstructorService.Commands;
using InstructorService.Domain;
using InstructorService.Infrastructure;
using Shared.Events.Instructors;
using System.Text.Json;

namespace InstructorService.Handlers;

/// <summary>
/// Handles UpdateInstructorCommand using transactional outbox pattern.
/// </summary>
public class UpdateInstructorCommandHandler
{
    private readonly InstructorDbContext _dbContext;
    private readonly ILogger<UpdateInstructorCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public UpdateInstructorCommandHandler(
        InstructorDbContext dbContext,
        ILogger<UpdateInstructorCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(UpdateInstructorCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling UpdateInstructorCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, InstructorId={InstructorId}",
            command.CommandId, command.CorrelationId, command.InstructorId);

        // Find instructor
        var instructor = await _dbContext.Instructors
            .FirstOrDefaultAsync(i => i.Id == command.InstructorId, cancellationToken);

        if (instructor == null)
        {
            _logger.LogWarning("Instructor not found: InstructorId={InstructorId}", command.InstructorId);
            return Result<int>.Failure($"Instructor with ID {command.InstructorId} not found.");
        }

        // Check for duplicate email (if email is being changed)
        if (instructor.Email != command.Email.ToLowerInvariant())
        {
            var existingInstructor = await _dbContext.Instructors
                .FirstOrDefaultAsync(i => i.Email == command.Email.ToLowerInvariant() && i.Id != command.InstructorId,
                    cancellationToken);

            if (existingInstructor != null)
            {
                _logger.LogWarning("Email {Email} is already used by another instructor (Id={OtherInstructorId})",
                    command.Email, existingInstructor.Id);
                return Result<int>.Failure($"Email {command.Email} is already in use.");
            }
        }

        // Update instructor aggregate
        var updateResult = instructor.Update(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.Specialization,
            command.Notes);

        if (!updateResult.IsSuccess)
        {
            _logger.LogWarning("Instructor validation failed: {Errors}",
                string.Join(", ", updateResult.Errors));
            return Result<int>.Failure(updateResult.Errors);
        }

        // Transactional outbox pattern: Update entity + save event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Update instructor entity
            _dbContext.Instructors.Update(instructor);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var instructorUpdatedEvent = new InstructorUpdated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"instructor/{instructor.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new InstructorUpdatedData
                {
                    InstructorId = instructor.Id,
                    FirstName = instructor.FirstName,
                    LastName = instructor.LastName,
                    Email = instructor.Email,
                    Phone = instructor.Phone,
                    Specialization = instructor.Specialization,
                    Status = instructor.Status,
                    Notes = instructor.Notes,
                    UpdatedAt = instructor.UpdatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = instructorUpdatedEvent.EventId,
                EventType = instructorUpdatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(instructorUpdatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated Instructor: InstructorId={InstructorId}, EventId={EventId}",
                instructor.Id, instructorUpdatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(instructorUpdatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", instructorUpdatedEvent.EventId);
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

            return Result<int>.Success(instructor.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating instructor");
            return Result<int>.Failure($"Error updating instructor: {ex.Message}");
        }
    }
}
