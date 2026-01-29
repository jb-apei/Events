using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ApiGateway.Services;

public class CommandPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<CommandPublisher> _logger;
    private readonly HttpClient? _httpClient;
    private readonly bool _useDevelopmentMode;
    private readonly string _prospectServiceUrl = string.Empty;

    public CommandPublisher(IConfiguration configuration, ILogger<CommandPublisher> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        var connectionString = configuration["ServiceBus:ConnectionString"];

        // Development mode: Use HTTP calls to ProspectService instead of Service Bus
        if (string.IsNullOrEmpty(connectionString))
        {
            _useDevelopmentMode = true;
            _prospectServiceUrl = configuration["ProspectService:Url"] ?? "http://localhost:5110";
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            _logger.LogWarning("Service Bus not configured. Using development mode with direct HTTP calls to ProspectService at {Url}", _prospectServiceUrl);
        }
        else
        {
            _useDevelopmentMode = false;
            var queueName = configuration["ServiceBus:QueueName"] ?? "identity-commands";
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueName);
            _logger.LogInformation("Using Service Bus queue {QueueName}", queueName);
        }
    }

    public async Task PublishCommandAsync<T>(T command, string correlationId, CancellationToken cancellationToken = default)
    {
        if (_useDevelopmentMode && _httpClient != null)
        {
            // Development mode: Call ProspectService directly via HTTP
            var commandType = typeof(T).GetProperty("CommandType")?.GetValue(command)?.ToString() ?? typeof(T).Name;
            var payload = typeof(T).GetProperty("Payload")?.GetValue(command);

            if (payload == null)
            {
                throw new InvalidOperationException($"Command {commandType} has no Payload property");
            }

            var json = JsonSerializer.Serialize(payload);
            HttpMethod method;
            string endpoint;

            switch (commandType)
            {
                case "CreateProspect":
                    method = HttpMethod.Post;
                    endpoint = "api/prospects";
                    break;
                case "UpdateProspect":
                    // Extract ProspectId from payload for URL
                    var prospectId = payload.GetType().GetProperty("ProspectId")?.GetValue(payload);
                    method = HttpMethod.Put;
                    endpoint = $"api/prospects/{prospectId}";
                    break;
                default:
                    throw new NotSupportedException($"Command type {commandType} not supported in development mode");
            }

            var request = new HttpRequestMessage(method, $"{_prospectServiceUrl}/{endpoint}")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Correlation-Id", correlationId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Called {Method} {Endpoint} directly with CorrelationId {CorrelationId} (development mode)",
                method, endpoint, correlationId);
        }
        else if (_sender != null)
        {
            // Production mode: Use Service Bus
            var json = JsonSerializer.Serialize(command);
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                CorrelationId = correlationId,
                MessageId = Guid.NewGuid().ToString()
            };

            await _sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Published command {CommandType} with MessageId {MessageId} and CorrelationId {CorrelationId}",
                typeof(T).Name, message.MessageId, correlationId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null)
            await _sender.DisposeAsync();
        if (_client != null)
            await _client.DisposeAsync();
        _httpClient?.Dispose();
    }
}
