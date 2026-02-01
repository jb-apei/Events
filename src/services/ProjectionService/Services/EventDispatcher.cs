using Azure.Messaging;
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
        BinaryData requestBody,
        CancellationToken cancellationToken = default)
    {
        // Parse CloudEvents (Event Grid uses CloudEvents v1.0 schema)
        var cloudEvents = CloudEvent.ParseMany(requestBody);

        foreach (var cloudEvent in cloudEvents)
        {
            // Handle Subscription Validation Handshake
            if (cloudEvent.Type == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var eventData = cloudEvent.Data?.ToObjectFromJson<SubscriptionValidationEventData>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new EventGridValidationResponse 
                { 
                    ValidationResponse = eventData?.ValidationCode ?? string.Empty 
                };
            }

            // Route domain events to handlers
            try
            {
                await RouteCloudEventAsync(cloudEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process event {EventId} ({EventType}). Will retry on next webhook delivery.",
                    cloudEvent.Id,
                    cloudEvent.Type);

                // Re-throw to trigger Event Grid retry
                throw;
            }
        }

        return null;
    }

    /// <summary>
    /// Routes an Event Grid event to the appropriate handler based on event type.
    /// </summary>
    private async Task RouteCloudEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Routing event {EventId} ({EventType}) from subject {Subject}",
            cloudEvent.Id,
            cloudEvent.Type,
            cloudEvent.Subject);

        switch (cloudEvent.Type)
        {
            // Prospect events
            case "ProspectCreated":
                var prospectCreated = DeserializeCloudEvent<ProspectCreated>(cloudEvent);
                await _prospectEventHandler.HandleProspectCreatedAsync(prospectCreated, cancellationToken);
                break;

            case "ProspectUpdated":
                var prospectUpdated = DeserializeCloudEvent<ProspectUpdated>(cloudEvent);
                await _prospectEventHandler.HandleProspectUpdatedAsync(prospectUpdated, cancellationToken);
                break;

            case "ProspectMerged":
                var prospectMerged = DeserializeCloudEvent<ProspectMerged>(cloudEvent);
                await _prospectEventHandler.HandleProspectMergedAsync(prospectMerged, cancellationToken);
                break;

            // Student events (stubs)
            case "StudentCreated":
            case "StudentUpdated":
            case "StudentChanged":
                await _studentEventHandler.HandleStudentCreatedAsync(
                    DeserializeCloudEventEnvelope(cloudEvent), cancellationToken);
                break;

            // Instructor events (stubs)
            case "InstructorCreated":
            case "InstructorUpdated":
            case "InstructorDeactivated":
                await _instructorEventHandler.HandleInstructorCreatedAsync(
                    DeserializeCloudEventEnvelope(cloudEvent), cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}. Ignoring.", cloudEvent.Type);
                break;
        }
    }

    /// <summary>
    /// Deserializes Event Grid event data to typed event envelope.
    /// </summary>
    private T DeserializeCloudEvent<T>(CloudEvent cloudEvent) where T : EventEnvelope
    {
        try
        {
            if (cloudEvent.Data == null)
            {
                throw new InvalidOperationException($"CloudEvent {cloudEvent.Id} has no data payload");
            }

            var eventEnvelope = cloudEvent.Data.ToObjectFromJson<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (eventEnvelope == null)
            {
                throw new InvalidOperationException($"Failed to deserialize event {cloudEvent.Id} to type {typeof(T).Name}");
            }

            return eventEnvelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event {EventId} to type {EventType}", cloudEvent.Id, typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Deserializes Event Grid event to base EventEnvelope (for unknown event types).
    /// </summary>
    private EventEnvelope DeserializeCloudEventEnvelope(CloudEvent cloudEvent)
    {
        if (cloudEvent.Data == null)
        {
            throw new InvalidOperationException($"CloudEvent {cloudEvent.Id} has no data payload");
        }

        var eventEnvelope = cloudEvent.Data.ToObjectFromJson<GenericEventEnvelope>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (eventEnvelope == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event {cloudEvent.Id}");
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
