using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ApiGateway.Models;

namespace ApiGateway.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketManager _manager;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(WebSocketManager manager, ILogger<WebSocketHandler> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(HttpContext context, string userId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        var connection = new WebSocketConnection(connectionId, webSocket, userId);

        if (!_manager.AddConnection(connection))
        {
            _logger.LogWarning("Connection rejected for user {UserId} - resource limit reached", userId);
            await webSocket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Insufficient resources - connection limit reached",
                CancellationToken.None);
            webSocket.Dispose();
            return;
        }

        // Limit default subscriptions to critical events only
        // Clients should explicitly subscribe to what they need using the "subscribe" message
        var defaultSubscriptions = Array.Empty<string>();

        // Only if development environment or legacy mode is enabled
        // _manager.UpdateSubscriptions(connectionId, defaultSubscriptions);

        _logger.LogInformation("WebSocket connection {ConnectionId} established for user {UserId}", connectionId, userId);

        try
        {
            await ReceiveMessagesAsync(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket connection {ConnectionId}", connectionId);
        }
        finally
        {
            await _manager.RemoveConnectionAsync(connectionId);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocketConnection connection)
    {
        var buffer = new byte[1024 * 4];
        var idleTimeout = TimeSpan.FromMinutes(5); // Close idle connections after 5 minutes
        var lastActivity = DateTime.UtcNow;

        using var cts = new CancellationTokenSource();

        // Send periodic pings to keep connection alive and detect disconnects
        var pingTask = Task.Run(async () =>
        {
            // Increase ping interval to 45 seconds (TCP timeout is usually 60s) to reduce chatter
            while (connection.Socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(45), cts.Token);

                // Check if connection is idle
                // REMOVED: Idle timeout disconnects active listeners.
                /*
                if (DateTime.UtcNow - lastActivity > idleTimeout)
                {
                    _logger.LogInformation("Closing idle WebSocket connection {ConnectionId}", connection.ConnectionId);
                    await connection.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection idle timeout",
                        CancellationToken.None);
                    break;
                }
                */

                // Send ping to keep connection alive
                if (connection.Socket.State == WebSocketState.Open)
                {
                    await connection.Socket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\":\"ping\"}")),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
        }, cts.Token);

        try
        {
            while (connection.Socket.State == WebSocketState.Open)
            {
                var result = await connection.Socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                lastActivity = DateTime.UtcNow; // Update activity timestamp

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client requested close",
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleClientMessageAsync(connection, message);
                }
            }
        }
        finally
        {
            cts.Cancel(); // Stop ping task
            try { await pingTask; } catch { /* Ignore cancellation */ }
        }
    }

    private async Task HandleClientMessageAsync(WebSocketConnection connection, string message)
    {
        try
        {
            var subscription = JsonSerializer.Deserialize<WebSocketSubscription>(message);
            if (subscription?.EventTypes != null && subscription.EventTypes.Length > 0)
            {
                _manager.UpdateSubscriptions(connection.ConnectionId, subscription.EventTypes);

                // Send acknowledgment
                var ack = new { type = "subscription_updated", eventTypes = subscription.EventTypes };
                await _manager.SendMessageAsync(connection.ConnectionId, JsonSerializer.Serialize(ack));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse client message: {Message}", message);
        }
    }
}
