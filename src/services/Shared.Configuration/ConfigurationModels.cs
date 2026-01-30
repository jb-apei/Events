namespace Shared.Configuration;

/// <summary>
/// Service Bus configuration settings.
/// Used by command consumers (ProspectService, StudentService, InstructorService).
/// </summary>
public class ServiceBusOptions
{
    public const string SectionName = "Azure:ServiceBus";

    /// <summary>
    /// Service Bus connection string.
    /// Local: Set via user-secrets (dotnet user-secrets set "Azure:ServiceBus:ConnectionString" "...")
    /// Azure: Retrieved from Key Vault (Azure-ServiceBus-ConnectionString)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Command queue name (e.g., "prospect-commands", "student-commands")
    /// </summary>
    public string CommandQueue { get; set; } = string.Empty;
}

/// <summary>
/// Event Grid configuration settings.
/// Used by EventRelay service to publish events.
/// </summary>
public class EventGridOptions
{
    public const string SectionName = "Azure:EventGrid";

    /// <summary>
    /// Prospect events topic endpoint
    /// </summary>
    public string ProspectTopicEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Prospect events topic access key (sensitive!)
    /// </summary>
    public string ProspectTopicKey { get; set; } = string.Empty;

    /// <summary>
    /// Student events topic endpoint
    /// </summary>
    public string StudentTopicEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Student events topic access key (sensitive!)
    /// </summary>
    public string StudentTopicKey { get; set; } = string.Empty;

    /// <summary>
    /// Instructor events topic endpoint
    /// </summary>
    public string InstructorTopicEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Instructor events topic access key (sensitive!)
    /// </summary>
    public string InstructorTopicKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook validation key for Event Grid subscriptions
    /// Used by ApiGateway and ProjectionService
    /// </summary>
    public string WebhookValidationKey { get; set; } = string.Empty;
}

/// <summary>
/// JWT authentication configuration.
/// Used by ApiGateway for token generation and validation.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing JWT tokens (minimum 32 characters)
    /// CRITICAL: Must be same value across all services!
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer (default: "EventsApiGateway")
    /// </summary>
    public string Issuer { get; set; } = "EventsApiGateway";

    /// <summary>
    /// JWT audience (default: "EventsClients")
    /// </summary>
    public string Audience { get; set; } = "EventsClients";

    /// <summary>
    /// Token expiration in hours (default: 24)
    /// </summary>
    public int ExpiryHours { get; set; } = 24;
}

/// <summary>
/// API Gateway configuration.
/// Used by all services to push events via WebSocket.
/// </summary>
public class ApiGatewayOptions
{
    public const string SectionName = "ApiGateway";

    /// <summary>
    /// API Gateway URL (e.g., "http://localhost:5037" or "https://ca-events-api-gateway-prod...")
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Whether to push events to API Gateway WebSocket hub (default: true)
    /// Set to false to disable real-time event push (testing only)
    /// </summary>
    public bool PushEvents { get; set; } = true;
}

/// <summary>
/// Database connection strings.
/// Used by all services for SQL Server connections.
/// </summary>
public class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Transactional database connection (write model) for Prospect aggregate
    /// </summary>
    public string? ProspectDb { get; set; }

    /// <summary>
    /// Transactional database connection (write model) for Student aggregate
    /// </summary>
    public string? StudentDb { get; set; }

    /// <summary>
    /// Transactional database connection (write model) for Instructor aggregate
    /// </summary>
    public string? InstructorDb { get; set; }

    /// <summary>
    /// Read model database connection (projections)
    /// </summary>
    public string? ProjectionDatabase { get; set; }
}
