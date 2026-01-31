using ApiGateway.EventHandlers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WsManager = ApiGateway.WebSockets.WebSocketManager;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventGridWebhookHandler _webhookHandler;
    private readonly WsManager _webSocketManager;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        EventGridWebhookHandler webhookHandler,
        WsManager webSocketManager,
        ILogger<EventsController> logger)
    {
        _webhookHandler = webhookHandler;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint for Event Grid notifications (Production)
    /// Also accepts CloudEvents format directly for development mode
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WebhookAsync([FromBody] JsonElement bodyJson)
    {
        try
        {
            // Check if it's CloudEvents format (has eventType, data properties)
            if (bodyJson.TryGetProperty("eventType", out var eventTypeProp))
            {
                _logger.LogInformation("Processing CloudEvent in development mode");

                var eventType = eventTypeProp.GetString() ?? "Unknown";

                // Extract all event envelope properties
                var eventEnvelope = new
                {
                    eventId = bodyJson.TryGetProperty("eventId", out var evtId) ? evtId.GetString() : Guid.NewGuid().ToString(),
                    eventType,
                    schemaVersion = bodyJson.TryGetProperty("schemaVersion", out var ver) ? ver.GetString() : "1.0",
                    occurredAt = bodyJson.TryGetProperty("occurredAt", out var time) ? time.GetString() : DateTime.UtcNow.ToString("O"),
                    producer = bodyJson.TryGetProperty("producer", out var prod) ? prod.GetString() : "prospect-service",
                    correlationId = bodyJson.TryGetProperty("correlationId", out var corrId) ? corrId.GetString() : "",
                    causationId = bodyJson.TryGetProperty("causationId", out var causeId) ? causeId.GetString() : "",
                    subject = bodyJson.TryGetProperty("subject", out var subj) ? subj.GetString() : "",
                    data = bodyJson.TryGetProperty("data", out var dataEl) ? dataEl : new JsonElement()
                };

                // Broadcast complete envelope to WebSocket clients
                await _webSocketManager.BroadcastEventAsync(eventEnvelope, eventType);

                _logger.LogInformation("CloudEvent {EventType} (ID: {EventId}) pushed to {ConnectionCount} WebSocket clients",
                    eventType, eventEnvelope.eventId, _webSocketManager.GetConnectionCount());
                return Ok(new { message = "Event processed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse as CloudEvent, trying Event Grid format");
        }

        // If not CloudEvents, treat as Event Grid format
        var (isValid, events) = await _webhookHandler.ValidateAndParseAsync(Request);

        if (!isValid || events == null)
        {
            _logger.LogWarning("Invalid Event Grid webhook request");
            return BadRequest(new { message = "Invalid webhook request" });
        }

        // Handle subscription validation
        if (events.Length == 1 && events[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
        {
            var validationData = JsonSerializer.Deserialize<JsonElement>(events[0].Data?.ToString() ?? "{}");
            var validationCode = validationData.GetProperty("validationCode").GetString();

            _logger.LogInformation("Responding to Event Grid subscription validation");

            return Ok(new { validationResponse = validationCode });
        }

        // Process events and push to WebSocket clients
        foreach (var eventGridEvent in events)
        {
            try
            {
                var eventData = await _webhookHandler.ParseEventDataAsync(eventGridEvent);

                if (eventData != null)
                {
                    // Extract event type from the EventGrid event
                    // The event type might be in the format "Prospects.ProspectCreated" or just "ProspectCreated"
                    var eventType = eventGridEvent.EventType;

                    // Broadcast to all subscribed WebSocket clients
                    await _webSocketManager.BroadcastEventAsync(new
                    {
                        eventType,
                        subject = eventGridEvent.Subject,
                        eventTime = eventGridEvent.EventTime,
                        data = eventData
                    }, eventType);

                    _logger.LogInformation("Event {EventType} pushed to WebSocket clients", eventType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventType}", eventGridEvent.EventType);
            }
        }

        return Ok(new { message = "Events processed", count = events.Length });
    }

    /// <summary>
    /// Handles HTTP OPTIONS requests for the webhook endpoint (required for Event Grid validation)
    /// </summary>
    [HttpOptions("webhook")]
    [AllowAnonymous]
    public IActionResult WebhookOptions()
    {
        // Optionally set CORS headers if needed
        Response.Headers.Add("Allow", "POST, OPTIONS");
        return Ok();
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            connections = _webSocketManager.GetConnectionCount(),
            timestamp = DateTime.UtcNow
        });
    }
}
