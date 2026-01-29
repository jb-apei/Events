using Azure;
using Azure.Messaging.EventGrid;
using Shared.Events;
using System.Text.Json;

namespace EventRelay.OutboxRelay;

/// <summary>
/// Publishes events to Azure Event Grid topics with proper routing and retry logic.
/// </summary>
public class EventGridPublisher : IEventPublisher
{
    private readonly ILogger<EventGridPublisher> _logger;
    private readonly Dictionary<string, EventGridPublisherClient> _topicClients;
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(2);

    public EventGridPublisher(
        IConfiguration configuration,
        ILogger<EventGridPublisher> logger)
    {
        _logger = logger;
        _topicClients = new Dictionary<string, EventGridPublisherClient>();

        // Initialize Event Grid publisher clients for each topic
        var prospectTopicEndpoint = configuration["Azure:EventGrid:ProspectTopicEndpoint"];
        var prospectTopicKey = configuration["Azure:EventGrid:ProspectTopicKey"];
        var studentTopicEndpoint = configuration["Azure:EventGrid:StudentTopicEndpoint"];
        var studentTopicKey = configuration["Azure:EventGrid:StudentTopicKey"];
        var instructorTopicEndpoint = configuration["Azure:EventGrid:InstructorTopicEndpoint"];
        var instructorTopicKey = configuration["Azure:EventGrid:InstructorTopicKey"];

        if (!string.IsNullOrEmpty(prospectTopicEndpoint) && !string.IsNullOrEmpty(prospectTopicKey))
        {
            _topicClients["prospect"] = new EventGridPublisherClient(
                new Uri(prospectTopicEndpoint),
                new AzureKeyCredential(prospectTopicKey));
        }

        if (!string.IsNullOrEmpty(studentTopicEndpoint) && !string.IsNullOrEmpty(studentTopicKey))
        {
            _topicClients["student"] = new EventGridPublisherClient(
                new Uri(studentTopicEndpoint),
                new AzureKeyCredential(studentTopicKey));
        }

        if (!string.IsNullOrEmpty(instructorTopicEndpoint) && !string.IsNullOrEmpty(instructorTopicKey))
        {
            _topicClients["instructor"] = new EventGridPublisherClient(
                new Uri(instructorTopicEndpoint),
                new AzureKeyCredential(instructorTopicKey));
        }

        _logger.LogInformation("EventGridPublisher initialized with {TopicCount} topics", _topicClients.Count);
    }

    /// <summary>
    /// Publish an event to the appropriate Event Grid topic based on event type.
    /// Implements exponential backoff retry logic for transient failures.
    /// </summary>
    public async Task<bool> PublishAsync(
        EventEnvelope eventEnvelope,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var topicKey = GetTopicKeyFromEventType(eventType);

        if (!_topicClients.TryGetValue(topicKey, out var client))
        {
            _logger.LogError(
                "No Event Grid client configured for topic '{TopicKey}' (event type: {EventType})",
                topicKey,
                eventType);
            return false;
        }

        var eventGridEvent = new EventGridEvent(
            subject: eventEnvelope.Subject,
            eventType: eventEnvelope.EventType,
            dataVersion: eventEnvelope.SchemaVersion,
            data: BinaryData.FromObjectAsJson(eventEnvelope))
        {
            Id = eventEnvelope.EventId,
            EventTime = eventEnvelope.OccurredAt
        };

        // Retry with exponential backoff
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                await client.SendEventAsync(eventGridEvent, cancellationToken);

                _logger.LogInformation(
                    "Successfully published event {EventId} ({EventType}) to topic '{TopicKey}' (correlation: {CorrelationId})",
                    eventEnvelope.EventId,
                    eventType,
                    topicKey,
                    eventEnvelope.CorrelationId);

                return true;
            }
            catch (RequestFailedException ex) when (IsTransientError(ex))
            {
                if (attempt < _maxRetries - 1)
                {
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));

                    _logger.LogWarning(
                        ex,
                        "Transient error publishing event {EventId} to topic '{TopicKey}', attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms",
                        eventEnvelope.EventId,
                        topicKey,
                        attempt + 1,
                        _maxRetries,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish event {EventId} to topic '{TopicKey}' after {MaxRetries} attempts",
                        eventEnvelope.EventId,
                        topicKey,
                        _maxRetries);

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Non-transient error publishing event {EventId} to topic '{TopicKey}'",
                    eventEnvelope.EventId,
                    topicKey);

                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Determine which Event Grid topic to route to based on event type.
    /// </summary>
    private static string GetTopicKeyFromEventType(string eventType)
    {
        return eventType switch
        {
            // Prospect events
            "ProspectCreated" or "ProspectUpdated" or "ProspectMerged" => "prospect",

            // Student events
            "StudentCreated" or "StudentUpdated" or "StudentChanged" => "student",

            // Instructor events
            "InstructorCreated" or "InstructorUpdated" or "InstructorDeactivated" => "instructor",

            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }

    /// <summary>
    /// Determine if an exception is a transient error that should be retried.
    /// </summary>
    private static bool IsTransientError(RequestFailedException ex)
    {
        // Retry on throttling (429) or server errors (5xx)
        return ex.Status is 429 or >= 500 and < 600;
    }
}
