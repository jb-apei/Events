namespace ApiGateway.Models;

public class WebSocketSubscription
{
    public string[] EventTypes { get; set; } = Array.Empty<string>();
}
