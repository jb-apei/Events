using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ApiGateway.WebSockets;

public class WebSocketConnection
{
    public string ConnectionId { get; }
    public WebSocket Socket { get; }
    public string UserId { get; }
    public HashSet<string> SubscribedEventTypes { get; } = new();
    public DateTime ConnectedAt { get; }

    public WebSocketConnection(string connectionId, WebSocket socket, string userId)
    {
        ConnectionId = connectionId;
        Socket = socket;
        UserId = userId;
        ConnectedAt = DateTime.UtcNow;
    }
}

public class WebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ILogger<WebSocketManager> _logger;
    private const int MaxConnectionsPerUser = 5;
    private const int MaxTotalConnections = 1000;

    public WebSocketManager(ILogger<WebSocketManager> logger)
    {
        _logger = logger;
    }

    public bool AddConnection(WebSocketConnection connection)
    {
        // Check total connection limit
        if (_connections.Count >= MaxTotalConnections)
        {
            _logger.LogWarning("Maximum total connections ({Max}) reached, rejecting connection {ConnectionId}",
                MaxTotalConnections, connection.ConnectionId);
            return false;
        }

        // Check per-user connection limit
        var userConnections = _connections.Values.Count(c => c.UserId == connection.UserId);
        if (userConnections >= MaxConnectionsPerUser)
        {
            _logger.LogWarning("User {UserId} has reached maximum connections ({Max}), rejecting connection {ConnectionId}",
                connection.UserId, MaxConnectionsPerUser, connection.ConnectionId);
            return false;
        }

        _connections[connection.ConnectionId] = connection;
        _logger.LogInformation("WebSocket connection added: {ConnectionId} for user {UserId} (total: {Total}, user: {UserCount})",
            connection.ConnectionId, connection.UserId, _connections.Count, userConnections + 1);
        return true;
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            if (connection.Socket.State == WebSocketState.Open)
            {
                await connection.Socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed by server",
                    CancellationToken.None);
            }
            connection.Socket.Dispose();
            _logger.LogInformation("WebSocket connection removed: {ConnectionId}", connectionId);
        }
    }

    public WebSocketConnection? GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var connection);
        return connection;
    }

    public IEnumerable<WebSocketConnection> GetAllConnections()
    {
        return _connections.Values;
    }

    public async Task SendMessageAsync(string connectionId, string message)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            if (connection.Socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await connection.Socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }

    public async Task BroadcastEventAsync(object eventData, string eventType)
    {
        var json = JsonSerializer.Serialize(eventData);
        var tasks = new List<Task>();

        foreach (var connection in _connections.Values)
        {
            // Only send if client is subscribed to this event type
            if (connection.SubscribedEventTypes.Count == 0 ||
                connection.SubscribedEventTypes.Contains(eventType))
            {
                if (connection.Socket.State == WebSocketState.Open)
                {
                    tasks.Add(SendMessageAsync(connection.ConnectionId, json));
                }
            }
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Broadcasted event {EventType} to {Count} connections",
            eventType, tasks.Count);
    }

    public void UpdateSubscriptions(string connectionId, string[] eventTypes)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.SubscribedEventTypes.Clear();
            foreach (var eventType in eventTypes)
            {
                connection.SubscribedEventTypes.Add(eventType);
            }
            _logger.LogInformation("Updated subscriptions for {ConnectionId}: {EventTypes}",
                connectionId, string.Join(", ", eventTypes));
        }
    }

    public int GetConnectionCount() => _connections.Count;
}
