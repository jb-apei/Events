# ProjectionService

Event-driven read model projection service for the Events microservices platform.

## Overview

ProjectionService subscribes to Event Grid topics and builds denormalized read models optimized for fast queries. It implements the **Inbox pattern** for idempotency and uses a separate database from the write model (CQRS pattern).

## Architecture

### Event Flow
```
Event Grid (prospect-events topic)
  ↓ HTTP POST /events/webhook
EventGridWebhookController
  ↓
EventDispatcher (routes by event type)
  ↓
ProspectEventHandler (processes event)
  ├─ Check Inbox (dedupe)
  ├─ Update read model projection
  └─ Record in Inbox
```

### Key Patterns

- **Event Grid Push Subscription**: Azure Event Grid calls our webhook endpoint with events
- **Inbox Pattern**: Stores processed `eventId` in Inbox table for 7-day dedupe window
- **Idempotent Handlers**: All event handlers check Inbox before processing
- **Eventually Consistent Read Models**: Projections are built asynchronously from events
- **Separate Database**: Read models use a different database than write models

## Database Schema

### Inbox Table
```sql
CREATE TABLE Inbox (
    EventId NVARCHAR(100) PRIMARY KEY,
    EventType NVARCHAR(100) NOT NULL,
    ProcessedAt DATETIME2 NOT NULL,
    CorrelationId NVARCHAR(100),
    Subject NVARCHAR(200)
)
```

### ProspectSummary Table (Read Model)
```sql
CREATE TABLE ProspectSummary (
    ProspectId INT PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    Status NVARCHAR(50) NOT NULL DEFAULT 'New',
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    Version INT NOT NULL
)
```

## Event Handlers

### ProspectEventHandler
- **ProspectCreated**: Creates new ProspectSummary projection
- **ProspectUpdated**: Updates existing ProspectSummary projection
- **ProspectMerged**: Removes source prospect from read model

### StudentEventHandler (Stub)
- Placeholder for future Student event handling

### InstructorEventHandler (Stub)
- Placeholder for future Instructor event handling

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "ProjectionDatabase": "Server=localhost;Database=EventsReadModel;..."
  },
  "EventGrid": {
    "WebhookEndpoint": "/events/webhook",
    "Topics": {
      "ProspectEvents": "prospect-events"
    }
  },
  "InboxCleanup": {
    "RetentionDays": 7,
    "CleanupIntervalHours": 6
  }
}
```

### Environment Variables (Production)
```bash
ConnectionStrings__ProjectionDatabase="Server=tcp:events-read.database.windows.net,1433;Initial Catalog=EventsReadModel;..."
ApplicationInsights__InstrumentationKey="your-app-insights-key"
```

## Local Development

### Prerequisites
- .NET 9.0 SDK
- SQL Server or LocalDB
- Azure Event Grid emulator (optional for local testing)

### Setup

1. **Restore packages**
   ```bash
   cd src/services/ProjectionService
   dotnet restore
   ```

2. **Update connection string** in `appsettings.Development.json`
   ```json
   "ConnectionStrings": {
     "ProjectionDatabase": "Server=(localdb)\\mssqllocaldb;Database=EventsReadModel;Integrated Security=true;TrustServerCertificate=true"
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the service**
   ```bash
   dotnet run
   ```

   Service will start on `http://localhost:5002`

### Testing Event Grid Webhook

1. **Test subscription validation** (simulates Event Grid handshake)
   ```bash
   curl -X POST http://localhost:5002/events/webhook \
     -H "Content-Type: application/json" \
     -d '[
       {
         "id": "validation-123",
         "eventType": "Microsoft.EventGrid.SubscriptionValidationEvent",
         "subject": "",
         "data": {
           "validationCode": "test-code-123"
         },
         "eventTime": "2026-01-29T10:00:00Z",
         "dataVersion": "1.0"
       }
     ]'
   ```

   Expected response:
   ```json
   {
     "validationResponse": "test-code-123"
   }
   ```

2. **Send test ProspectCreated event**
   ```bash
   curl -X POST http://localhost:5002/events/webhook \
     -H "Content-Type: application/json" \
     -d '[
       {
         "id": "event-123",
         "eventType": "ProspectCreated",
         "subject": "prospect/42",
         "data": {
           "eventId": "evt-001",
           "eventType": "ProspectCreated",
           "schemaVersion": "1.0",
           "occurredAt": "2026-01-29T10:00:00Z",
           "producer": "ProspectService",
           "correlationId": "corr-123",
           "subject": "prospect/42",
           "data": {
             "prospectId": 42,
             "firstName": "John",
             "lastName": "Doe",
             "email": "john.doe@example.com",
             "phone": "555-1234",
             "status": "New"
           }
         },
         "eventTime": "2026-01-29T10:00:00Z",
         "dataVersion": "1.0"
       }
     ]'
   ```

3. **Verify projection** in database
   ```sql
   SELECT * FROM ProspectSummary WHERE ProspectId = 42
   SELECT * FROM Inbox WHERE EventId = 'evt-001'
   ```

## Database Migrations

### Create a new migration
```bash
dotnet ef migrations add MigrationName --project ProjectionService.csproj
```

### Apply migrations
```bash
dotnet ef database update --project ProjectionService.csproj
```

### Generate SQL script (for production deployment)
```bash
dotnet ef migrations script --project ProjectionService.csproj --output migration.sql
```

## Health Checks

- **HTTP**: `GET /health` - Overall service health
- **Database**: Included in `/health` response

Example response:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

## Monitoring

### Application Insights Queries

**Event processing latency**
```kusto
traces
| where message contains "Successfully processed"
| extend eventId = tostring(customDimensions.EventId)
| extend duration = todouble(customDimensions.DurationMs)
| summarize avg(duration), max(duration), count() by bin(timestamp, 5m)
```

**Duplicate events (caught by Inbox)**
```kusto
traces
| where message contains "already processed. Skipping"
| summarize count() by bin(timestamp, 1h)
```

**Failed event processing**
```kusto
exceptions
| where outerMessage contains "Failed to process event"
| summarize count() by bin(timestamp, 1h)
```

## Deployment

### Azure Container Apps
```bash
az containerapp create \
  --name projection-service \
  --resource-group events-rg \
  --environment events-env \
  --image events/projection-service:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5 \
  --env-vars \
    ConnectionStrings__ProjectionDatabase="@Microsoft.KeyVault(SecretUri=...)" \
    ApplicationInsights__InstrumentationKey="@Microsoft.KeyVault(SecretUri=...)"
```

### Event Grid Subscription
```bash
az eventgrid event-subscription create \
  --name projection-service-subscription \
  --source-resource-id /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.EventGrid/topics/prospect-events \
  --endpoint https://projection-service.azurecontainerapps.io/events/webhook \
  --included-event-types ProspectCreated ProspectUpdated ProspectMerged \
  --max-delivery-attempts 10 \
  --event-delivery-schema eventgridschema
```

## Troubleshooting

### Event not appearing in read model
1. Check Event Grid delivery status in Azure Portal
2. Check service logs for errors
3. Query Inbox table to see if event was processed
4. Verify Event Grid subscription is active

### Duplicate processing
- Should be prevented by Inbox pattern
- Check logs for "already processed" messages
- Verify Inbox table has unique constraint on EventId

### Slow query performance
- Check indexes on ProspectSummary table
- Analyze query execution plans
- Consider adding materialized views for complex queries

## Future Enhancements

- [ ] Implement Student and Instructor event handlers
- [ ] Add GraphQL query API for read models
- [ ] Implement read model snapshots for faster rebuilds
- [ ] Add event replay capability for read model reconstruction
- [ ] Implement multi-tenant filtering
- [ ] Add caching layer (Redis) for frequently accessed projections
