# Development Mode Implementation Patterns

## Overview
This document captures the working patterns for building event-driven microservices that run without Azure dependencies (Service Bus, Event Grid) during development.

---

## Architecture Patterns

### 1. Event Flow (Development Mode)

```
Frontend → ApiGateway → Service (Command Handler)
                          ↓
                    Save to DB + Outbox (Transaction)
                          ↓
                    HTTP POST Event to ApiGateway Webhook
                          ↓
                    ApiGateway Broadcasts via WebSocket
                          ↓
                    Frontend Receives Event → Invalidates Cache → UI Updates
```

**Key Points:**
- Command handlers push events directly to ApiGateway via HTTP POST after transaction commit
- Uses `Task.Run()` to avoid blocking the response
- ApiGateway receives CloudEvents format and broadcasts to WebSocket clients
- Frontend uses React Query cache invalidation for instant UI updates

---

## Service Implementation Patterns

### 2. Command Handler Pattern (Write Service)

**File:** `[Service]/Handlers/Create[Entity]CommandHandler.cs`

```csharp
public class CreateProspectCommandHandler : ICommandHandler<CreateProspectCommand, Result<int>>
{
    private readonly ProspectDbContext _dbContext;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateProspectCommandHandler> _logger;

    public CreateProspectCommandHandler(
        ProspectDbContext dbContext,
        IConfiguration configuration,
        ILogger<CreateProspectCommandHandler> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> HandleAsync(CreateProspectCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Validate and create domain entity
        var prospectResult = Prospect.Create(...);
        if (!prospectResult.IsSuccess) return Result<int>.Failure(prospectResult.Errors);

        var prospect = prospectResult.Value!;

        // 2. Transactional Outbox: Save entity + event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Save entity
            await _dbContext.Prospects.AddAsync(prospect, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Create event envelope
            var prospectCreatedEvent = new ProspectCreated
            {
                EventId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"prospect/{prospect.Id}",
                OccurredAt = DateTime.UtcNow,
                Data = new ProspectCreatedData { ... }
            };

            // Save to Outbox
            var outboxMessage = new OutboxMessage
            {
                EventId = prospectCreatedEvent.EventId,
                EventType = prospectCreatedEvent.EventType,
                Payload = EventSerializer.Serialize(prospectCreatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 3. Development mode: Push event to ApiGateway (non-blocking)
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiGatewayUrl = _configuration["ApiGateway:Url"];
                        var httpClient = _httpClientFactory.CreateClient();
                        var json = JsonSerializer.Serialize(prospectCreatedEvent);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{apiGatewayUrl}/api/events/webhook", content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Event pushed to ApiGateway: {EventId}", prospectCreatedEvent.EventId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to push event: {StatusCode}", response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error pushing event to ApiGateway");
                    }
                });
            }

            return Result<int>.Success(prospect.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create prospect");
            return Result<int>.Failure("An error occurred while creating the prospect.");
        }
    }
}
```

**Critical Details:**
- Inject `IHttpClientFactory?` as optional (for backward compatibility)
- Use `Task.Run()` for fire-and-forget event pushing
- Check `ApiGateway:PushEvents` configuration flag
- Log success/failure for debugging

---

### 3. Service Configuration Pattern

**File:** `[Service]/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register HTTP client for development mode event pushing
builder.Services.AddHttpClient();

// Conditionally register Service Bus consumer (only in production)
var serviceBusConnectionString = builder.Configuration["Azure:ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Services.AddHostedService<ServiceBusCommandConsumer>();
}
else
{
    Console.WriteLine("INFO: Service Bus command consumer disabled (development mode)");
}

// Use in-memory database when no connection string provided
var prospectDbConnectionString = builder.Configuration.GetConnectionString("ProspectDb");
if (string.IsNullOrEmpty(prospectDbConnectionString))
{
    builder.Services.AddDbContext<ProspectDbContext>(options =>
        options.UseInMemoryDatabase("ProspectDb"));
}
else
{
    builder.Services.AddDbContext<ProspectDbContext>(options =>
        options.UseSqlServer(prospectDbConnectionString));
}
```

**File:** `[Service]/appsettings.json`

```json
{
  "ConnectionStrings": {
    "ProspectDb": ""
  },
  "ApiGateway": {
    "Url": "http://localhost:5037",
    "PushEvents": true
  },
  "Azure": {
    "ServiceBus": {
      "ConnectionString": "",
      "CommandQueue": "identity-commands"
    }
  }
}
```

**Key Points:**
- Empty connection strings trigger development mode
- `PushEvents: true` enables HTTP-based event flow
- Services run independently without Azure dependencies

---

## API Gateway Patterns

### 4. Event Webhook Endpoint (CloudEvents Support)

**File:** `ApiGateway/Controllers/EventsController.cs`

```csharp
[HttpPost("webhook")]
public async Task<IActionResult> WebhookAsync([FromBody] JsonElement bodyJson)
{
    try
    {
        // Check if it's CloudEvents format (development mode)
        if (bodyJson.TryGetProperty("eventType", out var eventTypeProp))
        {
            _logger.LogInformation("Processing CloudEvent in development mode");
            
            var eventType = eventTypeProp.GetString() ?? "Unknown";
            
            // Extract all event envelope properties
            var eventEnvelope = new
            {
                eventId = bodyJson.TryGetProperty("eventId", out var evtId) ? evtId.GetString() : Guid.NewGuid().ToString(),
                eventType,
                schemaVersion = bodyJson.TryGetProperty("schemaVersion", out var ver) ? ver.GetString() : "1.0",
                occurredAt = bodyJson.TryGetProperty("occurredAt", out var time) ? time.GetString() : DateTime.UtcNow.ToString("O"),
                producer = bodyJson.TryGetProperty("producer", out var prod) ? prod.GetString() : "service-name",
                correlationId = bodyJson.TryGetProperty("correlationId", out var corrId) ? corrId.GetString() : "",
                causationId = bodyJson.TryGetProperty("causationId", out var causeId) ? causeId.GetString() : "",
                subject = bodyJson.TryGetProperty("subject", out var subj) ? subj.GetString() : "",
                data = bodyJson.TryGetProperty("data", out var dataEl) ? dataEl : new JsonElement()
            };
            
            // Broadcast complete envelope to WebSocket clients
            await _webSocketManager.BroadcastEventAsync(eventEnvelope, eventType);
            
            _logger.LogInformation("CloudEvent {EventType} (ID: {EventId}) pushed to {ConnectionCount} WebSocket clients",
                eventType, eventEnvelope.eventId, _webSocketManager.GetConnectionCount());
            return Ok(new { message = "Event processed" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to parse as CloudEvent, trying Event Grid format");
    }
    
    // Fallback to Event Grid format (production)
    // ... existing Event Grid handling code
}
```

**Key Points:**
- Accept `[FromBody] JsonElement` to handle both CloudEvents and Event Grid formats
- Extract complete event envelope (all properties)
- Broadcast to WebSocket clients with proper structure
- Log connection count for debugging

---

### 5. API Gateway Proxy Pattern (Read Operations)

**File:** `ApiGateway/Controllers/[Entity]Controller.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProspectsController : ControllerBase
{
    private readonly CommandPublisher _commandPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProspectsController> _logger;

    public ProspectsController(
        CommandPublisher commandPublisher,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ProspectsController> logger)
    {
        _commandPublisher = commandPublisher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetProspects()
    {
        // Development mode: Proxy to ProspectService directly
        var prospectServiceUrl = _configuration["ProspectService:Url"] ?? "http://localhost:5110";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{prospectServiceUrl}/api/prospects");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else
            {
                _logger.LogWarning("Failed to fetch prospects from service: {StatusCode}", response.StatusCode);
                return Ok(Array.Empty<object>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prospects from service");
            return Ok(Array.Empty<object>());
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProspect([FromBody] CreateProspectRequest request)
    {
        // Commands still go through Service Bus (or direct API call in dev mode)
        // ... existing command publishing code
    }
}
```

**Key Points:**
- Read operations proxy to write service (query write model in dev mode)
- Write operations publish commands (Service Bus or direct API)
- Configuration defaults to `http://localhost:[port]` for development

---

## Frontend Patterns

### 6. WebSocket Hook (Auto-Subscribe Pattern)

**File:** `frontend/src/hooks/useWebSocket.ts`

```typescript
export const useWebSocket = (options: UseWebSocketOptions) => {
  const connect = useCallback(() => {
    const token = localStorage.getItem('jwt_token')
    const wsUrl = `${url}${token ? `?token=${encodeURIComponent(token)}` : ''}`
    const ws = new WebSocket(wsUrl)

    ws.onopen = () => {
      console.log('[WebSocket] Connected')
      setStatus('connected')
      reconnectAttemptsRef.current = 0
      onConnect?.()
    }

    ws.onmessage = (event) => {
      try {
        const envelope: EventEnvelope = JSON.parse(event.data)
        console.log('[WebSocket] Received event:', envelope.eventType, envelope)
        onMessage?.(envelope)
      } catch (error) {
        console.error('[WebSocket] Failed to parse message:', error)
      }
    }

    // ... error and close handlers with reconnection logic
  }, [url, onMessage, onConnect, onDisconnect])
}
```

**Key Points:**
- Pass JWT token as query parameter (`?token=...`)
- ApiGateway auto-subscribes clients (no subscription message needed)
- Reconnection with exponential backoff
- Parse complete EventEnvelope structure

---

### 7. Component Pattern (Real-time Updates)

**File:** `frontend/src/components/[Entity]Page.tsx`

```typescript
const ProspectPage = () => {
  const invalidateProspects = useInvalidateProspects()

  const { status } = useWebSocket({
    url: 'ws://localhost:3000/ws/events',
    onMessage: (event) => {
      console.log('[ProspectPage] Received WebSocket event:', event)
      if (event.eventType === 'ProspectCreated' || event.eventType === 'ProspectUpdated') {
        console.log('[ProspectPage] Invalidating prospects cache for:', event.eventType)
        invalidateProspects()
      }
    },
    onConnect: () => {
      console.log('[ProspectPage] WebSocket connected successfully!')
    },
    onDisconnect: () => {
      console.log('[ProspectPage] WebSocket disconnected')
    },
  })

  return (
    <div>
      <div className={`websocket-status ${status}`}>
        <span className="status-indicator"></span>
        WebSocket: {status.charAt(0).toUpperCase() + status.slice(1)}
      </div>
      {/* ... UI components */}
    </div>
  )
}
```

**Key Points:**
- Hook calls `invalidateProspects()` on relevant events
- React Query automatically refetches data
- UI updates instantly without manual refresh
- Display WebSocket status to user

---

### 8. Authentication Flow (401 Handling)

**File:** `frontend/src/api/[entity].ts`

```typescript
// Response interceptor - handle 401 errors
this.client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      console.warn('[API] 401 Unauthorized - Token expired or invalid. Logging out.')
      localStorage.removeItem('jwt_token')
      sessionStorage.removeItem('auth_redirect')
      // Dispatch custom event to trigger app logout
      window.dispatchEvent(new CustomEvent('auth:logout'))
    }
    return Promise.reject(error)
  }
)
```

**File:** `frontend/src/App.tsx`

```typescript
useEffect(() => {
  const token = localStorage.getItem('jwt_token')
  if (token) {
    setIsAuthenticated(true)
  }

  // Listen for auth:logout events (triggered by 401 responses)
  const handleLogout = () => {
    console.log('[App] Logout event received - clearing authentication')
    setIsAuthenticated(false)
  }

  window.addEventListener('auth:logout', handleLogout)
  return () => window.removeEventListener('auth:logout', handleLogout)
}, [])
```

**Key Points:**
- Custom event (`auth:logout`) for cross-component logout
- No infinite reload loops
- Automatic redirect to login page on 401
- Clear all auth state (localStorage + sessionStorage)

---

## WebSocket Infrastructure

### 9. API Gateway WebSocket Configuration

**File:** `ApiGateway/Program.cs`

```csharp
// Configure WebSocket support
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};
app.UseWebSockets(webSocketOptions);

// WebSocket endpoint with JWT authentication
app.Map("/ws/events", async context =>
{
    // Try to authenticate via query parameter token
    var token = context.Request.Query["token"].ToString();
    if (!string.IsNullOrEmpty(token))
    {
        var jwtService = context.RequestServices.GetRequiredService<JwtService>();
        var principal = jwtService.ValidateToken(token);
        if (principal != null)
        {
            context.User = principal;
        }
    }

    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var handler = context.RequestServices.GetRequiredService<WsHandler>();
    await handler.HandleWebSocketAsync(context, userId);
});
```

**File:** `ApiGateway/WebSockets/WebSocketHandler.cs`

```csharp
public async Task HandleWebSocketAsync(HttpContext context, string userId)
{
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var connectionId = Guid.NewGuid().ToString();
    var connection = new WebSocketConnection(connectionId, webSocket, userId);

    _manager.AddConnection(connection);

    // Auto-subscribe to all prospect events (MVP simplification)
    var defaultSubscriptions = new[] { "ProspectCreated", "ProspectUpdated", "ProspectMerged" };
    _manager.UpdateSubscriptions(connectionId, defaultSubscriptions);
    _logger.LogInformation("WebSocket connection {ConnectionId} auto-subscribed to prospect events", connectionId);

    // ... message receiving loop
}
```

**Key Points:**
- Authenticate via query parameter token
- Auto-subscribe clients (no client-side subscription messages)
- Log connection ID and subscriptions for debugging

---

### 10. Vite Proxy Configuration

**File:** `frontend/vite.config.ts`

```typescript
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5037',
        changeOrigin: true,
      },
      '/ws': {
        target: 'ws://localhost:5037',
        ws: true,
        changeOrigin: true,
      },
    },
  },
})
```

**Key Points:**
- Separate proxy for WebSocket (`ws: true`)
- `changeOrigin: true` for both HTTP and WebSocket
- Frontend connects to `ws://localhost:3000/ws/events`, proxied to ApiGateway

---

## Service Ports Convention

| Service           | Port  | Purpose                          |
|-------------------|-------|----------------------------------|
| ApiGateway        | 5037  | REST API + WebSocket hub         |
| ProspectService   | 5110  | Prospect command/query endpoints |
| StudentService    | 5120  | Student command/query endpoints  |
| InstructorService | 5130  | Instructor command/query endpoints |
| EventRelay        | 5200  | Outbox → Event Grid publisher    |
| ProjectionService | 5300  | Event subscribers (read models)  |
| Frontend          | 3000  | React + Vite dev server          |

---

## Checklist for New Services

When implementing StudentService or InstructorService, ensure:

### Backend Service:
- [ ] Add `IHttpClientFactory` to command handlers (optional parameter)
- [ ] Implement Transactional Outbox pattern (entity + event in same transaction)
- [ ] Add `Task.Run()` for non-blocking event push to ApiGateway
- [ ] Check `ApiGateway:PushEvents` configuration flag
- [ ] Configure in-memory DB when connection string is empty
- [ ] Conditionally register Service Bus consumer
- [ ] Add GET endpoints for querying entities (for ApiGateway proxy)
- [ ] Use port 5120 (Student) or 5130 (Instructor)

### ApiGateway Updates:
- [ ] Add controller for new entity (StudentController, InstructorController)
- [ ] Implement proxy pattern for GET operations
- [ ] Add configuration: `[Service]:Url` in appsettings.json
- [ ] Inject `IHttpClientFactory` and `IConfiguration` in controller

### Frontend Updates:
- [ ] Create `[Entity]Page.tsx` component with WebSocket subscription
- [ ] Add event type handlers in `onMessage` callback
- [ ] Implement React Query hooks (`use[Entity]s`, `useInvalidate[Entity]s`)
- [ ] Add API client in `api/[entity].ts` with 401 interceptor
- [ ] Create form component with event type picker
- [ ] Add list component with real-time updates

### WebSocket:
- [ ] Update `WebSocketHandler.cs` auto-subscribe array to include new event types
- [ ] Example: `["ProspectCreated", "StudentCreated", "InstructorCreated"]`
- [ ] No changes needed in frontend WebSocket hook (reusable)

### Event Schemas:
- [ ] Define events in `Shared.Events` library
- [ ] Follow EventEnvelope structure (eventId, eventType, data, etc.)
- [ ] Include all required properties for serialization

### Testing:
- [ ] Test event flow: Create entity → Event pushed → WebSocket broadcasts → UI updates
- [ ] Verify 401 handling: Restart services → Old token invalidated → Auto-logout
- [ ] Check WebSocket reconnection on disconnect
- [ ] Confirm data persists in in-memory database during service lifetime

---

## Debugging Tips

### WebSocket Not Connecting:
1. Check browser console for connection errors
2. Verify JWT token in localStorage is valid
3. Confirm ApiGateway is running on port 5037
4. Check Vite proxy configuration (`ws: true`)
5. Look for 401 Unauthorized in network tab

### Events Not Broadcasting:
1. Check ApiGateway logs: "CloudEvent {EventType} pushed to {ConnectionCount} clients"
2. Verify `ApiGateway:PushEvents` is `true` in service appsettings.json
3. Check service logs: "Event pushed to ApiGateway: {EventId}"
4. Confirm WebSocket connection count > 0 in logs

### UI Not Updating:
1. Open browser console and check for event reception logs
2. Verify React Query cache invalidation is triggered
3. Check that event type matches handler conditions
4. Confirm WebSocket status shows "Connected"

### 401 Errors After Restart:
- Expected behavior! ApiGateway generates new JWT secret on restart
- Simply refresh browser - auto-logout should trigger
- Login again with any credentials (MVP accepts all)

---

## Future Enhancements (Post-MVP)

### Production Azure Integration:
- Replace HTTP event pushing with Azure Event Grid publishing
- Enable Service Bus command consumers
- Use Azure SQL Database instead of in-memory DB
- Configure Azure Key Vault for secrets

### CQRS Read Models:
- Implement ProjectionService to build read-optimized databases
- ApiGateway queries ProjectionService instead of write services
- Event Grid → ProjectionService → Update read models

### Advanced Features:
- Event replay for debugging
- Dead-letter queue handling
- Event versioning and schema evolution
- Multi-tenancy with tenant isolation
- Saga pattern for distributed transactions
