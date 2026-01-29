# ApiGateway Service

## Overview
API Gateway for the Events microservices project. Handles:
- REST API endpoints for commands
- JWT authentication
- Event Grid webhook receiver
- WebSocket hub for real-time event push to clients

## Architecture

### Command Flow
```
Client → POST /api/prospects → Validate JWT → Publish Command to Service Bus → Return 202 Accepted
```

### Event Subscription Flow
```
Event Grid → POST /api/events/webhook → Validate Signature → Push to WebSocket Clients
```

### WebSocket Flow
```
Client → Connect to ws://localhost:5000/ws/events?access_token={jwt}
Client → Send {"eventTypes": ["ProspectCreated", "ProspectUpdated"]}
Server → Push matching events to client
```

## Endpoints

### Authentication
- `POST /api/auth/login` - Login and receive JWT token
- `POST /api/auth/validate` - Validate token (debug endpoint)

### Prospects (Protected - requires JWT)
- `POST /api/prospects` - Create prospect (publishes CreateProspect command)
- `PUT /api/prospects/{id}` - Update prospect (publishes UpdateProspect command)
- `GET /api/prospects/{id}` - Get prospect (placeholder for read model)
- `GET /api/prospects` - List prospects (placeholder for read model)

### Events
- `POST /api/events/webhook` - Event Grid webhook receiver (public, validated via signature)
- `GET /api/events/health` - Health check with WebSocket connection count

### WebSocket
- `WS /ws/events?access_token={jwt}` - WebSocket connection for real-time events

## Configuration

### appsettings.json
```json
{
  "ServiceBus": {
    "ConnectionString": "<service-bus-connection-string>",
    "QueueName": "identity-commands"
  },
  "EventGrid": {
    "WebhookValidationKey": "<optional-additional-validation>"
  },
  "Jwt": {
    "SecretKey": "<jwt-secret-from-key-vault>",
    "Issuer": "EventsApiGateway",
    "Audience": "EventsClients",
    "ExpiryHours": 24
  },
  "KeyVault": {
    "VaultUri": "https://<keyvault-name>.vault.azure.net/"
  }
}
```

### Environment Variables (Development)
For local development without Azure resources:
```bash
Jwt__SecretKey=your-local-secret-key-min-32-chars
ServiceBus__ConnectionString=Endpoint=sb://localhost...
```

## Running Locally

### Prerequisites
- .NET 9.0 SDK
- Azurite (for local Service Bus emulation) OR Azure Service Bus namespace

### Start Service
```bash
cd src/services/ApiGateway
dotnet restore
dotnet run
```

Service runs on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Test Authentication
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test123"}'
```

### Test Prospect Creation
```bash
TOKEN="<jwt-from-login>"
curl -X POST http://localhost:5000/api/prospects \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"John",
    "lastName":"Doe",
    "email":"john.doe@example.com",
    "phone":"555-0100"
  }'
```

### Test WebSocket (JavaScript)
```javascript
const token = '<jwt-from-login>';
const ws = new WebSocket(`ws://localhost:5000/ws/events?access_token=${token}`);

ws.onopen = () => {
  console.log('Connected');
  ws.send(JSON.stringify({
    eventTypes: ['ProspectCreated', 'ProspectUpdated']
  }));
};

ws.onmessage = (event) => {
  console.log('Received:', event.data);
};
```

## Implementation Notes

### JWT Authentication
- **MVP**: Simplified auth - accepts any email/password in login
- **Production**: Replace with proper identity provider (Azure AD, Auth0, etc.)
- Token expiry: 24 hours (configurable)
- WebSocket auth via query parameter `access_token`

### Command Publishing
- All commands published to Service Bus queue: `identity-commands`
- Commands include: `commandId`, `correlationId`, `userId`, `timestamp`, `payload`
- Returns 202 Accepted immediately (async processing)

### Event Grid Webhook
- Validates Event Grid subscription via `SubscriptionValidationEvent`
- Parses `EventGridEvent` array from request body
- Extracts event data and pushes to WebSocket clients
- Filters by client subscriptions (event type matching)

### WebSocket Management
- Maintains active connections in `WebSocketManager`
- Clients subscribe by sending event type array
- Broadcasts events only to subscribed clients
- Auto-cleanup on disconnect

### Error Handling
- Controllers return standard HTTP status codes
- WebSocket errors logged but don't crash service
- Command publishing failures return 500 (will implement retry in production)

### Security Considerations
- **JWT Secret**: Must be stored in Azure Key Vault (production)
- **CORS**: Currently allows all origins (restrict in production)
- **Event Grid**: Validates webhook signature via `aeg-event-type` header
- **WebSocket**: Requires valid JWT token for connection

## Future Enhancements
1. Implement read model queries (GET endpoints currently return placeholders)
2. Add rate limiting middleware
3. Implement proper user management (registration, password reset)
4. Add Azure Application Insights integration
5. Add OpenTelemetry distributed tracing
6. Implement circuit breaker for Service Bus publishing
7. Add WebSocket reconnection with event replay
8. Implement tenant isolation for multi-tenant scenarios
