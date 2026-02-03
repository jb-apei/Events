using ApiGateway.EventHandlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<IActionResult> WebhookAsync()
    {
        try
        {
            // Enable buffering so we can read the stream multiple times if needed
            Request.EnableBuffering();

            // 1. Try to validate and parse using the robust Webhook Handler (CloudEvents + EventGrid support)
            // This handles the official Event Grid Subscription Validation handshake too
            Request.Body.Position = 0;
            var (isValid, events) = await _webhookHandler.ValidateAndParseAsync(Request);

            if (isValid && events != null)
            {
                // Handle subscription validation response
                if (events.Length == 1 && events[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
                {
                    var validationData = events[0].Data;
                    if (validationData.ValueKind == JsonValueKind.Object && 
                        validationData.TryGetProperty("validationCode", out var codeProp))
                    {
                        var validationCode = codeProp.GetString();
                        _logger.LogInformation("Responding to Event Grid subscription validation");
                        return Ok(new { validationResponse = validationCode });
                    }
                }

                // Process events and push to WebSocket clients
                foreach (var parsedEvent in events)
                {
                    try
                    {
                        var eventType = parsedEvent.EventType;
                        var eventData = parsedEvent.Data;

                        // Broadcast to all subscribed WebSocket clients
                        await _webSocketManager.BroadcastEventAsync(new
                        {
                            eventType,
                            subject = parsedEvent.Subject,
                            eventTime = parsedEvent.EventTime,
                            data = eventData
                        }, eventType);

                        _logger.LogInformation("Event {EventType} pushed to WebSocket clients", eventType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process event {EventType}", parsedEvent.EventType);
                    }
                }

                return Ok(new { message = "Events processed", count = events.Length });
            }
            
            // 2. Fallback: Manual JSON parsing for direct development POSTs (Legacy/Dev mode)
            // If the handler couldn't parse it, maybe it's a simple JSON pushed manually
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            if (!string.IsNullOrEmpty(body))
            {
                try 
                {
                    var bodyJson = JsonSerializer.Deserialize<JsonElement>(body);
                    if (bodyJson.TryGetProperty("eventType", out var eventTypeProp))
                    {
                        _logger.LogInformation("Processing manual JSON event in development mode");
                        var eventType = eventTypeProp.GetString() ?? "Unknown";
                        
                        // Extract minimal envelope
                        var eventEnvelope = new
                        {
                            eventId = Guid.NewGuid().ToString(),
                            eventType,
                            data = bodyJson.TryGetProperty("data", out var dataEl) ? dataEl : new JsonElement()
                        };

                        await _webSocketManager.BroadcastEventAsync(eventEnvelope, eventType);
                        return Ok(new { message = "Manual event processed" });
                    }
                }
                catch { /* Ignore JSON parse errors here, strictly fallback */ }
            }

            _logger.LogWarning("Invalid webhook request - neither EventGrid nor CloudEvent format detected");
            return BadRequest(new { message = "Invalid webhook request" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// CloudEvents Abuse Protection Handshake (OPTIONS)
    /// Required/Recommended for CloudEvents subscriptions in Azure Event Grid
    /// </summary>
    [HttpOptions("webhook")]
    public IActionResult WebhookOptions()
    {
        // Event Grid sends "WebHook-Request-Origin" header
        if (Request.Headers.TryGetValue("WebHook-Request-Origin", out var origin))
        {
            // We must echo it back in "WebHook-Allowed-Origin"
            Response.Headers.Append("WebHook-Allowed-Origin", origin);

            // Allow POST requests and a reasonable rate
            Response.Headers.Append("WebHook-Allowed-Rate", "120");

            _logger.LogInformation("Handled CloudEvents OPTIONS handshake for origin: {Origin}", origin);
            return Ok();
        }

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
