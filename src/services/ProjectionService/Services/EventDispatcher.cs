using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using ProjectionService.EventHandlers;
using Shared.Events;
using Shared.Events.Prospects;
using System.Text.Json;

namespace ProjectionService.Services;

/// <summary>
/// Dispatches Event Grid events to appropriate event handlers.
/// Handles Event Grid webhook validation and event routing.
/// </summary>
public class EventDispatcher
{
    private readonly ProspectEventHandler _prospectEventHandler;
    private readonly StudentEventHandler _studentEventHandler;
    private readonly InstructorEventHandler _instructorEventHandler;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(
        ProspectEventHandler prospectEventHandler,
        StudentEventHandler studentEventHandler,
        InstructorEventHandler instructorEventHandler,
        ILogger<EventDispatcher> logger)
    {
        _prospectEventHandler = prospectEventHandler;
        _studentEventHandler = studentEventHandler;
        _instructorEventHandler = instructorEventHandler;
        _logger = logger;
    }

    /// <summary>
    /// Processes Event Grid webhook events.
    /// Handles subscription validation and routes events to handlers.
    /// </summary>
    public async Task<EventGridValidationResponse?> ProcessEventGridWebhookAsync(
        string requestBody,
        CancellationToken cancellationToken = default)
    {
        var events = EventGridEvent.ParseMany(BinaryData.FromString(requestBody));

        foreach (var egEvent in events)
        {
            // Handle subscription validation
            if (egEvent.EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationData = egEvent.Data?.ToObjectFromJson<SubscriptionValidationEventData>();
                if (validationData?.ValidationCode != null)
                {
                    _logger.LogInformation("Event Grid subscription validation received. Code: {ValidationCode}",
                        validationData.ValidationCode);

                    return new EventGridValidationResponse
                    {
                        ValidationResponse = validationData.ValidationCode
                    };
                }
            }

            // Route domain events to handlers
            try
            {
                await RouteEventAsync(egEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process event {EventId} ({EventType}). Will retry on next webhook delivery.",
                    egEvent.Id,
                    egEvent.EventType);

                // Re-throw to trigger Event Grid retry
                throw;
            }
        }

        return null; // No validation response needed for regular events
    }

    /// <summary>
    /// Routes an Event Grid event to the appropriate handler based on event type.
    /// </summary>
    private async Task RouteEventAsync(EventGridEvent egEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Routing event {EventId} ({EventType}) from subject {Subject}",
            egEvent.Id,
            egEvent.EventType,
            egEvent.Subject);

        switch (egEvent.EventType)
        {
            // Prospect events
            case "ProspectCreated":
                var prospectCreated = DeserializeEvent<ProspectCreated>(egEvent);
                await _prospectEventHandler.HandleProspectCreatedAsync(prospectCreated, cancellationToken);
                break;

            case "ProspectUpdated":
                var prospectUpdated = DeserializeEvent<ProspectUpdated>(egEvent);
                await _prospectEventHandler.HandleProspectUpdatedAsync(prospectUpdated, cancellationToken);
                break;

            case "ProspectMerged":
                var prospectMerged = DeserializeEvent<ProspectMerged>(egEvent);
                await _prospectEventHandler.HandleProspectMergedAsync(prospectMerged, cancellationToken);
                break;

            // Student events (stubs)
            case "StudentCreated":
            case "StudentUpdated":
            case "StudentChanged":
                await _studentEventHandler.HandleStudentCreatedAsync(
                    DeserializeEventEnvelope(egEvent), cancellationToken);
                break;

            // Instructor events (stubs)
            case "InstructorCreated":
            case "InstructorUpdated":
            case "InstructorDeactivated":
                await _instructorEventHandler.HandleInstructorCreatedAsync(
                    DeserializeEventEnvelope(egEvent), cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}. Ignoring.", egEvent.EventType);
                break;
        }
    }

    /// <summary>
    /// Deserializes Event Grid event data to typed event envelope.
    /// </summary>
    private T DeserializeEvent<T>(EventGridEvent egEvent) where T : EventEnvelope
    {
        try
        {
            var eventData = egEvent.Data?.ToString();
            if (string.IsNullOrEmpty(eventData))
            {
                throw new InvalidOperationException($"Event {egEvent.Id} has no data payload");
            }

            var eventEnvelope = JsonSerializer.Deserialize<T>(eventData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (eventEnvelope == null)
            {
                throw new InvalidOperationException($"Failed to deserialize event {egEvent.Id} to type {typeof(T).Name}");
            }

            return eventEnvelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event {EventId} to type {EventType}", egEvent.Id, typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Deserializes Event Grid event to base EventEnvelope (for unknown event types).
    /// </summary>
    private EventEnvelope DeserializeEventEnvelope(EventGridEvent egEvent)
    {
        var eventData = egEvent.Data?.ToString();
        if (string.IsNullOrEmpty(eventData))
        {
            throw new InvalidOperationException($"Event {egEvent.Id} has no data payload");
        }

        var eventEnvelope = JsonSerializer.Deserialize<GenericEventEnvelope>(eventData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (eventEnvelope == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event {egEvent.Id}");
        }

        return eventEnvelope;
    }

    /// <summary>
    /// Generic event envelope for unknown event types.
    /// </summary>
    private class GenericEventEnvelope : EventEnvelope
    {
        public override string EventType => "Generic";
    }
}

/// <summary>
/// Response for Event Grid subscription validation.
/// </summary>
public class EventGridValidationResponse
{
    public string ValidationResponse { get; set; } = string.Empty;
}
