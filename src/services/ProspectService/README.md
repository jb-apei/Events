# ProspectService - Write Model

## Overview
ProspectService is the write model microservice for managing Prospect entities in the event-driven identity management system. It implements the Command side of the CQRS pattern.

## Architecture Patterns

### Transactional Outbox Pattern
All domain events are saved to an Outbox table in the same database transaction as the entity changes. This guarantees:
- No lost events (atomic consistency)
- At-least-once delivery semantics
- Resilience to Event Grid downtime

### Command Processing Flow
```
Service Bus Queue (identity-commands)
  ↓
ServiceBusCommandConsumer (background service)
  ↓
Command Handler (CreateProspectCommandHandler, UpdateProspectCommandHandler)
  ├─ Validate business rules
  ├─ BEGIN TRANSACTION
  │  ├─ Save/Update Prospect entity
  │  └─ Save event to Outbox table
  └─ COMMIT TRANSACTION
  ↓
EventRelay (separate service) polls Outbox
  → Publishes to Event Grid
```

## Project Structure

```
ProspectService/
├── Domain/
│   ├── Prospect.cs              # Aggregate root with business rules
│   └── Result.cs                # Result pattern for error handling
├── Commands/
│   ├── CreateProspectCommand.cs # Create prospect command
│   └── UpdateProspectCommand.cs # Update prospect command
├── Handlers/
│   ├── CreateProspectCommandHandler.cs  # Creates prospect + saves event
│   └── UpdateProspectCommandHandler.cs  # Updates prospect + saves event
├── Infrastructure/
│   ├── ProspectDbContext.cs     # EF Core context (Prospects + Outbox)
│   └── OutboxMessage.cs         # Outbox entity
├── Controllers/
│   └── ProspectsController.cs   # REST API (for testing)
├── Services/
│   └── ServiceBusCommandConsumer.cs  # Background service for commands
└── Program.cs                   # Startup configuration
```

## Database Schema

### Prospects Table
- `Id` (int, PK, identity)
- `FirstName` (nvarchar(100), required)
- `LastName` (nvarchar(100), required)
- `Email` (nvarchar(255), required, unique)
- `Phone` (nvarchar(50), nullable)
- `Source` (nvarchar(100), nullable)
- `Status` (nvarchar(50), required) - Values: New, Contacted, Qualified, Converted, Lost
- `Notes` (nvarchar(2000), nullable)
- `CreatedAt` (datetime2, required)
- `UpdatedAt` (datetime2, required)

### Outbox Table
- `Id` (bigint, PK, identity)
- `EventId` (nvarchar(100), required, unique)
- `EventType` (nvarchar(100), required)
- `Payload` (nvarchar(max), required) - JSON serialized event
- `CreatedAt` (datetime2, required)
- `Published` (bit, required)
- `PublishedAt` (datetime2, nullable)
- Index on `(Published, CreatedAt)` for efficient polling

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "ProspectDb": "Server=your-server.database.windows.net;Database=ProspectDb;..."
  },
  "Azure": {
    "ServiceBus": {
      "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/...",
      "CommandQueue": "identity-commands"
    }
  }
}
```

### Environment Variables (recommended for production)
- `ConnectionStrings__ProspectDb` - Azure SQL connection string
- `Azure__ServiceBus__ConnectionString` - Service Bus connection string

## Running the Service

### Local Development
```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run with in-memory database (for quick testing)
dotnet run

# Run with SQL Server LocalDB
# Update appsettings.Development.json with connection string first
dotnet run --environment Development
```

### Docker
```bash
# Build Docker image
docker build -t prospect-service:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__ProspectDb="..." \
  -e Azure__ServiceBus__ConnectionString="..." \
  prospect-service:latest
```

## API Endpoints

### Create Prospect
```
POST /api/prospects
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phone": "+1-555-0123",
  "source": "Website",
  "notes": "Interested in Data Science program"
}
```

### Update Prospect
```
PUT /api/prospects/{id}
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phone": "+1-555-0123",
  "source": "Website",
  "status": "Contacted",
  "notes": "Follow-up scheduled for next week"
}
```

### Health Check
```
GET /health
```

## Service Bus Command Format

Commands sent to the `identity-commands` queue must include:
- **Subject**: "CreateProspect" or "UpdateProspect"
- **Body**: JSON serialized command
- **ApplicationProperties**: `CommandType` = "CreateProspect" or "UpdateProspect"

Example:
```json
{
  "commandId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "correlationId": "trace-abc123",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com"
}
```

## Events Published

### ProspectCreated
Published when a new prospect is created. Contains:
- `prospectId`
- `firstName`, `lastName`, `email`
- `phone`, `source`, `status`, `notes`
- `createdAt`

### ProspectUpdated
Published when a prospect is updated. Contains:
- `prospectId`
- `firstName`, `lastName`, `email`
- `phone`, `source`, `status`, `notes`
- `updatedAt`

All events follow the EventEnvelope structure defined in `Shared.Events`.

## Business Rules

1. **Email must be unique** - No two prospects can have the same email
2. **Required fields**: firstName, lastName, email
3. **Valid statuses**: New, Contacted, Qualified, Converted, Lost
4. **Email validation**: Must be valid email format
5. **Case-insensitive email**: Emails are stored lowercase

## Error Handling

- **Business validation failures**: Return 400 Bad Request with error list
- **Not found errors**: Return 404 Not Found
- **Transient Service Bus errors**: Message is abandoned (automatic retry)
- **Non-transient errors**: Message is dead-lettered

## Monitoring

- **Health endpoint**: `/health` - Checks database connectivity
- **Logs**: Structured logging with correlation IDs
- **Metrics**: Application Insights integration (configure in appsettings)

## Development Notes

1. **In-memory database**: Used by default if no connection string configured
2. **EF Core migrations**: Not implemented yet - using `EnsureCreated()` for development
3. **Service Bus**: Consumer is disabled if connection string is missing
4. **CORS**: Currently allows all origins - configure for production

## Next Steps

1. Implement EF Core migrations for production deployments
2. Add authentication/authorization middleware
3. Implement idempotency check for commands (to prevent duplicate processing)
4. Add distributed tracing with OpenTelemetry
5. Configure Application Insights for monitoring
6. Add integration tests for command handlers
