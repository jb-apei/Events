using Azure.Messaging.ServiceBus;
using ProspectService.Commands;
using ProspectService.Handlers;
using ProspectService.Models;
using System.Text.Json;

namespace ProspectService.Services;

/// <summary>
/// Background service that consumes commands from Azure Service Bus.
/// Listens to "identity-commands" queue.
/// </summary>
public class ServiceBusCommandConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceBusCommandConsumer> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public ServiceBusCommandConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ServiceBusCommandConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["Azure:ServiceBus:ConnectionString"];
        var queueName = _configuration["Azure:ServiceBus:CommandQueue"] ?? "identity-commands";

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Service Bus connection string not configured. Command consumer disabled.");
            return;
        }

        _logger.LogInformation("Starting Service Bus command consumer for queue: {QueueName}", queueName);

        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        // Keep running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        _logger.LogInformation("Received message: MessageId={MessageId}, Body={Body}",
            args.Message.MessageId, messageBody);

        try
        {
            // Extract command type from application properties or message label
            var commandType = args.Message.Subject ?? args.Message.ApplicationProperties.GetValueOrDefault("CommandType")?.ToString();

            if (string.IsNullOrEmpty(commandType))
            {
                _logger.LogWarning("Message missing CommandType. MessageId={MessageId}", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "MissingCommandType", "Message is missing CommandType property");
                return;
            }

            // Create scope for scoped dependencies (DbContext)
            using var scope = _serviceProvider.CreateScope();

            var success = commandType switch
            {
                "CreateProspect" => await HandleCreateProspectAsync(messageBody, scope, args.CancellationToken),
                "UpdateProspect" => await HandleUpdateProspectAsync(messageBody, scope, args.CancellationToken),
                _ => throw new InvalidOperationException($"Unknown command type: {commandType}")
            };

            if (success)
            {
                await args.CompleteMessageAsync(args.Message);
                _logger.LogInformation("Successfully processed message: MessageId={MessageId}", args.Message.MessageId);
            }
            else
            {
                // Business validation failure - dead letter it
                await args.DeadLetterMessageAsync(args.Message, "ValidationFailed", "Command validation failed");
                _logger.LogWarning("Message dead-lettered due to validation failure: MessageId={MessageId}",
                    args.Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: MessageId={MessageId}, Error={Error}",
                args.Message.MessageId, ex.Message);

            // For transient errors, abandon the message (it will be retried)
            if (IsTransientError(ex))
            {
                await args.AbandonMessageAsync(args.Message);
            }
            else
            {
                await args.DeadLetterMessageAsync(args.Message, ex.GetType().Name, ex.Message);
            }
        }
    }

    private async Task<bool> HandleCreateProspectAsync(string messageBody, IServiceScope scope, CancellationToken cancellationToken)
    {
        // Deserialize the command message envelope
        var envelope = JsonSerializer.Deserialize<CommandMessage>(messageBody);
        if (envelope?.Payload == null)
        {
            _logger.LogError("Failed to deserialize command envelope");
            return false;
        }

        var command = envelope.AsCreateProspectCommand();
        if (command == null)
        {
            _logger.LogError("Failed to deserialize CreateProspectCommand from payload");
            return false;
        }

        var handler = scope.ServiceProvider.GetRequiredService<CreateProspectCommandHandler>();
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess;
    }

    private async Task<bool> HandleUpdateProspectAsync(string messageBody, IServiceScope scope, CancellationToken cancellationToken)
    {
        // Deserialize the command message envelope
        var envelope = JsonSerializer.Deserialize<CommandMessage>(messageBody);
        if (envelope?.Payload == null)
        {
            _logger.LogError("Failed to deserialize command envelope");
            return false;
        }

        var command = envelope.AsUpdateProspectCommand();
        if (command == null)
        {
            _logger.LogError("Failed to deserialize UpdateProspectCommand from payload");
            return false;
        }

        var handler = scope.ServiceProvider.GetRequiredService<UpdateProspectCommandHandler>();
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess;
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error: Source={ErrorSource}, Entity={EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    private static bool IsTransientError(Exception ex)
    {
        // Add more transient error types as needed
        return ex is TimeoutException
            || ex is ServiceBusException { IsTransient: true };
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping Service Bus command consumer");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}
