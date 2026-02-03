using Microsoft.EntityFrameworkCore;
using ProjectionService.Data;
using ProjectionService.Projections;
using Shared.Events;
using Shared.Events.Prospects;

namespace ProjectionService.EventHandlers;

/// <summary>
/// Handles Prospect domain events to build read model projections.
/// Implements Inbox pattern for idempotency.
/// </summary>
public class ProspectEventHandler
{
    private readonly ProjectionDbContext _dbContext;
    private readonly ILogger<ProspectEventHandler> _logger;

    public ProspectEventHandler(
        ProjectionDbContext dbContext,
        ILogger<ProspectEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Handles ProspectCreated event by creating a new ProspectSummary projection.
    /// </summary>
    public async Task HandleProspectCreatedAsync(ProspectCreated eventEnvelope, CancellationToken cancellationToken = default)
    {
        // Check if already processed (idempotency via Inbox)
        if (await IsEventProcessedAsync(eventEnvelope.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "Event {EventId} ({EventType}) already processed. Skipping.",
                eventEnvelope.EventId,
                eventEnvelope.EventType);
            return;
        }

        var data = eventEnvelope.Data;

        _logger.LogInformation(
            "Processing ProspectCreated event {EventId} for Prospect {ProspectId}",
            eventEnvelope.EventId,
            data.ProspectId);

        // Check if prospect already exists (should not happen, but defensive)
        var existingProspect = await _dbContext.ProspectSummary
            .FirstOrDefaultAsync(p => p.ProspectId == data.ProspectId, cancellationToken);

        if (existingProspect != null)
        {
            _logger.LogWarning(
                "Prospect {ProspectId} already exists in read model. Updating instead of creating.",
                data.ProspectId);

            UpdateProspectSummary(existingProspect, data);
            existingProspect.UpdatedAt = eventEnvelope.OccurredAt;
            existingProspect.Version++;
        }
        else
        {
            // Create new ProspectSummary projection
            var prospectSummary = new ProspectSummary
            {
                ProspectId = data.ProspectId,
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = data.Email,
                Phone = data.Phone,
                Address = null,
                Status = data.Status ?? "New",
                Notes = data.Notes,
                CreatedAt = eventEnvelope.OccurredAt,
                UpdatedAt = eventEnvelope.OccurredAt,
                Version = 1
            };

            _dbContext.ProspectSummary.Add(prospectSummary);
        }

        // Record event in Inbox
        await RecordInboxMessageAsync(eventEnvelope, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully processed ProspectCreated event {EventId} for Prospect {ProspectId}",
            eventEnvelope.EventId,
            data.ProspectId);
    }

    /// <summary>
    /// Handles ProspectUpdated event by updating the ProspectSummary projection.
    /// </summary>
    public async Task HandleProspectUpdatedAsync(ProspectUpdated eventEnvelope, CancellationToken cancellationToken = default)
    {
        // Check if already processed (idempotency via Inbox)
        if (await IsEventProcessedAsync(eventEnvelope.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "Event {EventId} ({EventType}) already processed. Skipping.",
                eventEnvelope.EventId,
                eventEnvelope.EventType);
            return;
        }

        var data = eventEnvelope.Data;

        _logger.LogInformation(
            "Processing ProspectUpdated event {EventId} for Prospect {ProspectId}",
            eventEnvelope.EventId,
            data.ProspectId);

        // Find existing prospect
        var prospectSummary = await _dbContext.ProspectSummary
            .FirstOrDefaultAsync(p => p.ProspectId == data.ProspectId, cancellationToken);

        if (prospectSummary == null)
        {
            _logger.LogWarning(
                "Prospect {ProspectId} not found in read model. Creating from update event.",
                data.ProspectId);

            // Create if not exists (out-of-order event handling)
            prospectSummary = new ProspectSummary
            {
                ProspectId = data.ProspectId,
                CreatedAt = eventEnvelope.OccurredAt, // Use event time as fallback
                Version = 1
            };

            _dbContext.ProspectSummary.Add(prospectSummary);
        }

        // Update projection
        UpdateProspectSummary(prospectSummary, data);
        prospectSummary.UpdatedAt = eventEnvelope.OccurredAt;
        prospectSummary.Version++;

        // Record event in Inbox
        await RecordInboxMessageAsync(eventEnvelope, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully processed ProspectUpdated event {EventId} for Prospect {ProspectId}",
            eventEnvelope.EventId,
            data.ProspectId);
    }

    /// <summary>
    /// Handles ProspectMerged event (future implementation).
    /// </summary>
    public async Task HandleProspectMergedAsync(ProspectMerged eventEnvelope, CancellationToken cancellationToken = default)
    {
        // Check if already processed
        if (await IsEventProcessedAsync(eventEnvelope.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "Event {EventId} ({EventType}) already processed. Skipping.",
                eventEnvelope.EventId,
                eventEnvelope.EventType);
            return;
        }

        var data = eventEnvelope.Data;

        _logger.LogInformation(
            "Processing ProspectMerged event {EventId}: Prospect {SourceProspectId} merged into {TargetProspectId}",
            eventEnvelope.EventId,
            data.SourceProspectId,
            data.TargetProspectId);

        // Remove source prospect from read model
        var sourceProspect = await _dbContext.ProspectSummary
            .FirstOrDefaultAsync(p => p.ProspectId == data.SourceProspectId, cancellationToken);

        if (sourceProspect != null)
        {
            _dbContext.ProspectSummary.Remove(sourceProspect);
            _logger.LogInformation("Removed source Prospect {ProspectId} from read model", data.SourceProspectId);
        }

        // Record event in Inbox
        await RecordInboxMessageAsync(eventEnvelope, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully processed ProspectMerged event {EventId}",
            eventEnvelope.EventId);
    }

    /// <summary>
    /// Checks if an event has already been processed (Inbox pattern).
    /// </summary>
    private async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.Inbox.AnyAsync(i => i.EventId == eventId, cancellationToken);
    }

    /// <summary>
    /// Records event in Inbox table for deduplication.
    /// </summary>
    private async Task RecordInboxMessageAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken)
    {
        var inboxMessage = new InboxMessage
        {
            EventId = eventEnvelope.EventId,
            EventType = eventEnvelope.EventType,
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = eventEnvelope.CorrelationId,
            Subject = eventEnvelope.Subject
        };

        _dbContext.Inbox.Add(inboxMessage);
    }

    /// <summary>
    /// Updates ProspectSummary fields from event data.
    /// Shared logic for Create and Update handlers.
    /// </summary>
    private void UpdateProspectSummary(ProspectSummary summary, dynamic data)
    {
        summary.FirstName = data.FirstName;
        summary.LastName = data.LastName;
        summary.Email = data.Email;
        summary.Phone = data.Phone;
        summary.Address = null; // Address fields not in event yet
        summary.Status = data.Status ?? "New";
        summary.Notes = data.Notes; 
        summary.Source = data.Source; 
    }

}
