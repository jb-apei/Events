using Azure.Messaging;
using Azure.Messaging.EventGrid;
using System.Text.Json;
using ApiGateway.Models;

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

    public async Task<(bool IsValid, ParsedEvent[]? Events)> ValidateAndParseAsync(HttpRequest request)
    {
        try
        {
            // Efficiently read using BinaryData instead of StreamReader string allocation
            var binaryData = await BinaryData.FromStreamAsync(request.Body);

            // Attempt to parse as CloudEvents (Standard V1.0)
            try
            {
                var cloudEvents = CloudEvent.ParseMany(binaryData);
                if (cloudEvents.Any())
                {
                    _logger.LogInformation("Parsed {Count} CloudEvents", cloudEvents.Length);

                    var parsedEvents = cloudEvents.Select(e => new ParsedEvent
                    {
                        EventId = e.Id,
                        EventType = e.Type,
                        Subject = e.Subject,
                        EventTime = e.Time ?? DateTimeOffset.UtcNow,
                        Data = e.Data != null ? e.Data.ToObjectFromJson<JsonElement>() : default
                    }).ToArray();

                    // Check for validation handshake in CloudEvents format
                    if (parsedEvents.Length == 1 && parsedEvents[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
                    {
                        return (true, parsedEvents);
                    }

                    return (true, parsedEvents);
                }
            }
            catch
            {
                // Fallback to Event Grid Schema
            }

            // Parse Event Grid events
            var events = EventGridEvent.ParseMany(binaryData);

            // Validate Event Grid subscription (webhook validation handshake)
            if (events.Length == 1 && events[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                _logger.LogInformation("Received Event Grid validation request");
                // Convert to ParsedEvent for consistency
                var validationEvent = new ParsedEvent
                {
                    EventType = events[0].EventType,
                    Data = events[0].Data.ToObjectFromJson<JsonElement>()
                };
                return (true, new[] { validationEvent });
            }

            var mappedEvents = events.Select(e => new ParsedEvent
            {
                EventId = e.Id,
                EventType = e.EventType,
                Subject = e.Subject,
                EventTime = e.EventTime,
                Data = e.Data.ToObjectFromJson<JsonElement>()
            }).ToArray();

            // Validate webhook key (optional additional security layer)
            var validationKey = _configuration["EventGrid:WebhookValidationKey"];
            if (!string.IsNullOrEmpty(validationKey))
            {
                if (!request.Headers.TryGetValue("aeg-event-type", out var headerKey) ||
                    !headerKey.ToString().Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid Event Grid header: aeg-event-type");
                    return (false, null);
                }
            }

            _logger.LogInformation("Received {Count} Event Grid events", events.Length);
            return (true, mappedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Event Grid webhook");
            return (false, null);
        }
    }

    public Task<object?> ParseEventDataAsync(ParsedEvent parsedEvent)
    {
        return Task.FromResult<object?>(parsedEvent.Data);
    }
}
