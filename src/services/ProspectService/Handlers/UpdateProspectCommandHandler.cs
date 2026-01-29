using Microsoft.EntityFrameworkCore;
using ProspectService.Commands;
using ProspectService.Domain;
using ProspectService.Infrastructure;
using Shared.Events.Prospects;
using System.Text.Json;

namespace ProspectService.Handlers;

/// <summary>
/// Handles UpdateProspectCommand using transactional outbox pattern.
/// </summary>
public class UpdateProspectCommandHandler
{
    private readonly ProspectDbContext _dbContext;
    private readonly ILogger<UpdateProspectCommandHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public UpdateProspectCommandHandler(
        ProspectDbContext dbContext,
        ILogger<UpdateProspectCommandHandler> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(UpdateProspectCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling UpdateProspectCommand: CommandId={CommandId}, CorrelationId={CorrelationId}, ProspectId={ProspectId}",
            command.CommandId, command.CorrelationId, command.ProspectId);

        // Find prospect
        var prospect = await _dbContext.Prospects
            .FirstOrDefaultAsync(p => p.Id == command.ProspectId, cancellationToken);

        if (prospect == null)
        {
            _logger.LogWarning("Prospect not found: ProspectId={ProspectId}", command.ProspectId);
            return Result<int>.Failure($"Prospect with ID {command.ProspectId} not found.");
        }

        // Check for duplicate email (if email is being changed)
        if (prospect.Email != command.Email.ToLowerInvariant())
        {
            var existingProspect = await _dbContext.Prospects
                .FirstOrDefaultAsync(p => p.Email == command.Email.ToLowerInvariant() && p.Id != command.ProspectId,
                    cancellationToken);

            if (existingProspect != null)
            {
                _logger.LogWarning("Email {Email} is already used by another prospect (Id={OtherProspectId})",
                    command.Email, existingProspect.Id);
                return Result<int>.Failure($"Email {command.Email} is already in use.");
            }
        }

        // Update prospect aggregate
        var updateResult = prospect.Update(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.Source,
            command.Status,
            command.Notes);

        if (!updateResult.IsSuccess)
        {
            _logger.LogWarning("Prospect validation failed: {Errors}",
                string.Join(", ", updateResult.Errors));
            return Result<int>.Failure(updateResult.Errors);
        }

        // Transactional outbox pattern: Update entity + save event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Update prospect entity
            _dbContext.Prospects.Update(prospect);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Create and save event to Outbox
            var prospectUpdatedEvent = new ProspectUpdated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"prospect/{prospect.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new ProspectUpdatedData
                {
                    ProspectId = prospect.Id,
                    FirstName = prospect.FirstName,
                    LastName = prospect.LastName,
                    Email = prospect.Email,
                    Phone = prospect.Phone,
                    Source = prospect.Source,
                    Status = prospect.Status,
                    Notes = prospect.Notes,
                    UpdatedAt = prospect.UpdatedAt
                }
            };

            var outboxMessage = new OutboxMessage
            {
                EventId = prospectUpdatedEvent.EventId,
                EventType = prospectUpdatedEvent.EventType,
                Payload = Infrastructure.EventSerializer.Serialize(prospectUpdatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated Prospect: ProspectId={ProspectId}, EventId={EventId}",
                prospect.Id, prospectUpdatedEvent.EventId);

            // Development mode: Push event directly to ApiGateway
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(prospectUpdatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway (development mode): {EventId}", prospectUpdatedEvent.EventId);
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
            _logger.LogError(ex, "Failed to update prospect: {Error}", ex.Message);
            return Result<int>.Failure("An error occurred while updating the prospect.");
        }
    }
}
