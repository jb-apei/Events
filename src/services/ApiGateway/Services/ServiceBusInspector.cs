using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ApiGateway.Services;

public class ServiceBusInspector
{
    private readonly ServiceBusClient? _client;
    private readonly string _queueName;
    private readonly ILogger<ServiceBusInspector> _logger;
    private readonly bool _isEnabled;

    public ServiceBusInspector(IConfiguration configuration, ILogger<ServiceBusInspector> logger)
    {
        _logger = logger;
        var connectionString = configuration["ServiceBus:ConnectionString"] ?? configuration["Azure:ServiceBus:ConnectionString"];
        _queueName = configuration["ServiceBus:QueueName"] ?? "identity-commands";

        if (!string.IsNullOrEmpty(connectionString))
        {
            _client = new ServiceBusClient(connectionString);
            _isEnabled = true;
        }
        else
        {
            _logger.LogWarning("Service Bus connection string not found. Inspector disabled.");
            _isEnabled = false;
        }
    }

    public async Task<List<object>> PeekQueueMessagesAsync(int maxMessages = 10)
    {
        return await PeekMessagesInternalAsync(_queueName, maxMessages);
    }

    public async Task<List<object>> PeekDlqMessagesAsync(int maxMessages = 10)
    {
        // Path to DLQ is "queueName/$DeadLetterQueue"
        // But ServiceBusReceiver options handle subqueue
        return await PeekMessagesInternalAsync(_queueName, maxMessages, true);
    }

    private async Task<List<object>> PeekMessagesInternalAsync(string queueName, int maxMessages, bool isDlq = false)
    {
        if (!_isEnabled || _client == null)
        {
            return new List<object>();
        }

        try
        {
            var options = new ServiceBusReceiverOptions 
            { 
                SubQueue = isDlq ? SubQueue.DeadLetter : SubQueue.None 
            };
            
            await using var receiver = _client.CreateReceiver(queueName, options);
            
            var messages = await receiver.PeekMessagesAsync(maxMessages);
            var result = new List<object>();

            foreach (var msg in messages)
            {
                string body = "";
                try 
                {
                    body = msg.Body.ToString();
                    // Try to pretty print JSON if possible
                    try {
                        var je = JsonSerializer.Deserialize<JsonElement>(body);
                        body = JsonSerializer.Serialize(je, new JsonSerializerOptions { WriteIndented = true });
                    } catch {}
                }
                catch { body = "[Binary/Unreadable]"; }

                result.Add(new
                {
                    MessageId = msg.MessageId,
                    SequenceNumber = msg.SequenceNumber,
                    EnqueuedTime = msg.EnqueuedTime,
                    Subject = msg.Subject,
                    ContentType = msg.ContentType,
                    CorrelationId = msg.CorrelationId,
                    DeliveryCount = msg.DeliveryCount,
                    DeadLetterReason = isDlq ? msg.DeadLetterReason : null,
                    DeadLetterErrorDescription = isDlq ? msg.DeadLetterErrorDescription : null,
                    ApplicationProperties = msg.ApplicationProperties,
                    Body = body
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to peek messages from {Queue} (DLQ={IsDlq})", queueName, isDlq);
            throw;
        }
    }
}
