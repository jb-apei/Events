using Microsoft.EntityFrameworkCore;
using ProspectService.Commands;
using ProspectService.Domain;
using ProspectService.Infrastructure;
using Shared.Events;
using Shared.Events.Prospects;
using System.Text.Json;

namespace ProspectService.Handlers;

/// <summary>
/// Handles CreateProspectCommand using transactional outbox pattern.
/// </summary>
public class CreateProspectCommandHandler
{
    private readonly ProspectDbContext _dbContext;
    private readonly ILogger<CreateProspectCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CreateProspectCommandHandler(
        ProspectDbContext dbContext,
        ILogger<CreateProspectCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(CreateProspectCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CreateProspectCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, Email={Email}",
            command.CommandId, command.CorrelationId, command.Email);

        // Check for duplicate email
        var existingProspect = await _dbContext.Prospects
            .FirstOrDefaultAsync(p => p.Email == command.Email.ToLowerInvariant(), cancellationToken);

        if (existingProspect != null)
        {
            _logger.LogWarning("Prospect with email {Email} already exists (Id={ProspectId})",
                command.Email, existingProspect.Id);
            return Result<int>.Failure($"A prospect with email {command.Email} already exists.");
        }

        // Create prospect aggregate
        var prospectResult = Prospect.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.Source,
            command.Notes);

        if (!prospectResult.IsSuccess)
        {
            _logger.LogWarning("Prospect validation failed: {Errors}",
                string.Join(", ", prospectResult.Errors));
            return Result<int>.Failure(prospectResult.Errors);
        }

        var prospect = prospectResult.Value!;

        // Transactional outbox pattern: Save entity + event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Save prospect entity
            await _dbContext.Prospects.AddAsync(prospect, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var prospectCreatedEvent = new ProspectCreated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"prospect/{prospect.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new ProspectCreatedData
                {
                    ProspectId = prospect.Id,
                    FirstName = prospect.FirstName,
                    LastName = prospect.LastName,
                    Email = prospect.Email,
                    Phone = prospect.Phone,
                    Source = prospect.Source,
                    Status = prospect.Status,
                    Notes = prospect.Notes,
                    CreatedAt = prospect.CreatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = prospectCreatedEvent.EventId,
                EventType = prospectCreatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(prospectCreatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created Prospect: ProspectId={ProspectId}, EventId={EventId}",
                prospect.Id, prospectCreatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(prospectCreatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", prospectCreatedEvent.EventId);
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

            return Result<int>.Success(prospect.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create prospect: {Error}", ex.Message);
            return Result<int>.Failure("An error occurred while creating the prospect.");
        }
    }
}
