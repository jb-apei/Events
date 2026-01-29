using Microsoft.EntityFrameworkCore;
using InstructorService.Commands;
using InstructorService.Domain;
using InstructorService.Infrastructure;
using Shared.Events;
using Shared.Events.Instructors;
using System.Text.Json;

namespace InstructorService.Handlers;

/// <summary>
/// Handles CreateInstructorCommand using transactional outbox pattern.
/// </summary>
public class CreateInstructorCommandHandler
{
    private readonly InstructorDbContext _dbContext;
    private readonly ILogger<CreateInstructorCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CreateInstructorCommandHandler(
        InstructorDbContext dbContext,
        ILogger<CreateInstructorCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(CreateInstructorCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CreateInstructorCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, Email={Email}",
            command.CommandId, command.CorrelationId, command.Email);

        // Check for duplicate email
        var existingByEmail = await _dbContext.Instructors
            .FirstOrDefaultAsync(i => i.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (existingByEmail != null)
        {
            _logger.LogWarning("Instructor with email {Email} already exists (Id={InstructorId})",
                command.Email, existingByEmail.Id);
            return Result<int>.Failure($"An instructor with email {command.Email} already exists.");
        }

        // Check for duplicate employee number
        var existingByEmpNum = await _dbContext.Instructors
            .FirstOrDefaultAsync(i => i.EmployeeNumber == command.EmployeeNumber, cancellationToken);

        if (existingByEmpNum != null)
        {
            _logger.LogWarning("Instructor with employee number {EmployeeNumber} already exists (Id={InstructorId})",
                command.EmployeeNumber, existingByEmpNum.Id);
            return Result<int>.Failure($"An instructor with employee number {command.EmployeeNumber} already exists.");
        }

        // Create instructor aggregate
        var instructorResult = Instructor.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.EmployeeNumber,
            command.HireDate,
            command.Phone,
            command.Specialization,
            command.Notes);

        if (!instructorResult.IsSuccess)
        {
            _logger.LogWarning("Instructor validation failed: {Errors}",
                string.Join(", ", instructorResult.Errors));
            return Result<int>.Failure(instructorResult.Errors);
        }

        var instructor = instructorResult.Value!;

        // Transactional outbox pattern: Save entity + event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Save instructor entity
            await _dbContext.Instructors.AddAsync(instructor, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var instructorCreatedEvent = new InstructorCreated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"instructor/{instructor.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new InstructorCreatedData
                {
                    InstructorId = instructor.Id,
                    FirstName = instructor.FirstName,
                    LastName = instructor.LastName,
                    Email = instructor.Email,
                    Phone = instructor.Phone,
                    EmployeeNumber = instructor.EmployeeNumber,
                    Specialization = instructor.Specialization,
                    HireDate = instructor.HireDate,
                    Status = instructor.Status,
                    Notes = instructor.Notes,
                    CreatedAt = instructor.CreatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = instructorCreatedEvent.EventId,
                EventType = instructorCreatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(instructorCreatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created Instructor: InstructorId={InstructorId}, EventId={EventId}",
                instructor.Id, instructorCreatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(instructorCreatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", instructorCreatedEvent.EventId);
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
            _logger.LogError(ex, "Error creating instructor");
            return Result<int>.Failure($"Error creating instructor: {ex.Message}");
        }
    }
}
