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

        // Auto-subscribe to all identity events (Prospects, Students, Instructors)
        var defaultSubscriptions = new[] {
            "ProspectCreated", "ProspectUpdated", "ProspectMerged",
            "StudentCreated", "StudentUpdated", "StudentChanged",
            "InstructorCreated", "InstructorUpdated", "InstructorDeactivated"
        };
        _manager.UpdateSubscriptions(connectionId, defaultSubscriptions);
        _logger.LogInformation("WebSocket connection {ConnectionId} auto-subscribed to identity events", connectionId);

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

        while (connection.Socket.State == WebSocketState.Open)
        {
            var result = await connection.Socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

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
