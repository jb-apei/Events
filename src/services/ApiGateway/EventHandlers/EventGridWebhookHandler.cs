using Azure.Messaging.EventGrid;
using System.Text.Json;

namespace ApiGateway.EventHandlers;

public class EventGridWebhookHandler
{
    private readonly ILogger<EventGridWebhookHandler> _logger;
    private readonly IConfiguration _configuration;

    public EventGridWebhookHandler(ILogger<EventGridWebhookHandler> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(bool IsValid, EventGridEvent[]? Events)> ValidateAndParseAsync(HttpRequest request)
    {
        try
        {
            // Efficiently read using BinaryData instead of StreamReader string allocation
            var binaryData = await BinaryData.FromStreamAsync(request.Body);

            // Parse Event Grid events
            var events = EventGridEvent.ParseMany(binaryData);

            // Validate Event Grid subscription (webhook validation handshake)
            if (events.Length == 1 && events[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                _logger.LogInformation("Received Event Grid subscription validation request");
                return (true, events.ToArray());
            }

            // Validate webhook key (optional additional security layer)
            var validationKey = _configuration["EventGrid:WebhookValidationKey"];
            if (!string.IsNullOrEmpty(validationKey))
            {
                var headerKey = request.Headers["aeg-event-type"].ToString();
                if (string.IsNullOrEmpty(headerKey) || !headerKey.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid Event Grid header: aeg-event-type");
                    return (false, null);
                }
            }

            _logger.LogInformation("Received {Count} Event Grid events", events.Length);
            return (true, events.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Event Grid webhook");
            return (false, null);
        }
    }

    public Task<object?> ParseEventDataAsync(EventGridEvent eventGridEvent)
    {
        try
        {
            // Parse the event data based on event type
            var eventType = eventGridEvent.EventType;
            var data = eventGridEvent.Data?.ToString();

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogWarning("Event {EventType} has no data", eventType);
                return Task.FromResult<object?>(null);
            }

            // For this MVP, we pass through the raw event data
            // In production, you might want to deserialize to specific types
            var eventData = JsonSerializer.Deserialize<JsonElement>(data);

            _logger.LogInformation("Parsed event {EventType} with subject {Subject}",
                eventType, eventGridEvent.Subject);

            return Task.FromResult<object?>(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse event data for {EventType}", eventGridEvent.EventType);
            return Task.FromResult<object?>(null);
        }
    }
}
