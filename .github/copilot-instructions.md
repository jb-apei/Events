# Copilot Instructions for Events Project

## Project Overview

**Purpose**: Event-driven identity management system (MVP) for Prospects, Students, and Instructors using Azure Event Grid and Service Bus.

**Tech Stack**:
- Frontend: React with TypeScript, React Query (state management), WebSocket subscriptions
- Backend: .NET Core microservices (Azure Container Apps)
- Messaging: Azure Service Bus (commands), Azure Event Grid (events)
- Data: Azure SQL (transactional + read models)
- Auth: Custom identity (JWT tokens)
- Observability: Application Insights, OpenTelemetry

**Architecture**: Event-driven microservices with CQRS pattern
- Commands flow through Service Bus
- Events published via Event Grid after DB commit
- Transactional Outbox pattern for reliability
- React UI subscribes to events for real-time updates

## Domain Model

**Identities Bounded Context** (MVP Focus: Prospects)
- **Prospect**: Create, Update, Merge
- **Student**: Create, Update, Changed
- **Instructor**: Create, Update, Deactivate

## Event Standards

All events MUST include this envelope:
```json
{
  "eventId": "guid",
  "eventType": "ProspectCreated | ProspectUpdated | ...",
  "schemaVersion": "1.0",
  "occurredAt": "2026-01-29T10:30:00Z",
  "producer": "service-name",
  "correlationId": "trace-id",
  "causationId": "parent-event-id",
  "subject": "prospect/12345",
  "data": { /* event-specific payload */ }
}
```

## Architecture Patterns

### Command Flow (Write Path)
1. React UI → POST `/api/prospects` → API Gateway
2. API → `CreateProspectCommand` → Service Bus Queue
3. Command Handler → Validate → Save to DB + Outbox (single transaction)
4. Outbox Relay → Publish `ProspectCreated` → Event Grid
5. Subscribers consume event → Update read models / trigger workflows

### Event Subscription (Read Path) - Real-time UI Updates
**Recommended Architecture (Clean + Robust)**:
1. Event Grid → API Gateway (webhook endpoint `/api/events/webhook`)
2. API Gateway validates, filters, authorizes event
3. API Gateway pushes event via WebSocket to connected React clients
4. React receives event → invalidates React Query cache → UI refreshes

**With Durability/Replay (Optional)**:
- Event Grid → Service Bus Topic → Subscriber → API Gateway → WebSocket → React
- Provides buffering, retries, and allows clients to replay missed events on reconnect

**Why Backend Intermediary**:
- Security: Validate Event Grid signatures, authorize which events reach which clients
- Filtering: Only push relevant events to specific users (e.g., tenant isolation)
- Transformation: Map internal events to UI-friendly format
- Durability: Buffer events if clients are temporarily disconnected

### Idempotency
- All handlers MUST check idempotency store using `eventId` or `commandId`
- Dedupe window: 7 days
- Store in `Inbox` table before processing

## Project Structure

```
/
├── src/ (TypeScript)
│   │   ├── components/
│   │   │   ├── ProspectForm.tsx     # Smart form based on event type
│   │   │   ├── ProspectList.tsx     # Displays prospects, listens to WebSocket
│   │   │   └── EventTypePicker.tsx  # Select event type → load form
│   │   ├── hooks/
│   │   │   ├── useWebSocket.ts      # WebSocket connection management
│   │   │   └── useProspects.ts      # React Query hooks for prospect data
│   │   └── api/
│   │       └── prospects.ts         # API client using fetch/axios
│   ├── services/ (.NET Core)
│   │   ├── ProspectService/         # Main write service
│   │   │   ├── Commands/            # CreateProspect, UpdateProspect
│   │   │   ├── Handlers/            # Command handlers
│   │   │   ├── Domain/              # Prospect aggregate
│   │   │   └── Infrastructure/      # DB, Outbox, Service Bus
│   │   ├── ApiGateway/              # WebSocket hub + Event Grid webhook receiver
│   │   │   ├── WebSockets/          # WebSocket connection manager
│   │   │   ├── EventHandlers/       # Event Grid webhook validation + routing
│   │   │   └── Controllers/         # REST API endpoints
│   │   ├── EventRelay/              # Outbox → Event Grid publisher
│   │   └── ProjectionService/       # Event subscribers for read models
│   ├── shared/
│   │   ├── Events/                  # Event schemas (shared contracts)
│   │   └── Infrastructure/          # Shared Azure SDK helpers
│   └── infrastructure/              # Bicep/Terraform for Azure resources
└── docs/
    └── event_based_azure_reference.md  # Architecture decisions
```

## Getting Started

### Local Development

```bash
# Start React frontend
cd src/frontend
npm install
npm run dev

# Run ProspectService (requires Azurite for local Service Bus/Storage emulation)
cd src/services/ProspectService
dotnet run

# Start Azurite (local Azure emulator)
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

### Testing

```bash
# Frontend tests
npm test

# Backend tests (unit + integration)
dotnet test

# E2E test: Create Prospect → Verify event published → Verify UI updates
dotnet test --filter Category=E2E
```

## Key Workflows

### Adding a New Event Type

1. **Define event schema** in `shared/Events/` (e.g., `ProspectMerged.cs`)
2. **Add command** in `ProspectService/Commands/MergeProspectCommand.cs`
3. **Create handler** that saves domain event to Outbox
4. **Update React form** in `ProspectForm.tsx` to handle new event type
5. **Add subscription** in `ProjectionService` if read model needs updates

### Event Envelope Requirements

- Generate `eventId` using `Guid.NewGuid()` before publishing
- Set `correlationId` from incoming request header or generate new one
- Set `causationId` to the ID of the command that caused this event
- Always use UTC for `occurredAt`
- Include `subject` for partitioning (e.g., `prospect/{prospectId}`)

### Debugging Event Flow

1. Check Application Insights for correlation ID across all hops
2. Query Outbox table: `SELECT * FROM Outbox WHERE Published = 0`
3. Check Event Grid dead-letter queue for failed deliveries
4. Use Service Bus Explorer to inspect DLQ messages
5. Verify Event Grid webhook delivery in API Gateway logs: `POST /api/events/webhook`
6. Check WebSocket connection status and active subscriptions in API Gateway

## Coding Conventions

### Naming
- Events: Past tense (e.g., `ProspectCreated`, `ProspectUpdated`)
- Commands: Imperative (e.g., `CreateProspect`, `UpdateProspect`)
- Files: PascalCase for C#, kebab-case for React components

### Error Handling
- Command handlers: Return `Result<T>` pattern, never throw for business validation
- Event handlers: Throw exceptions to trigger Service Bus retry (transient errors only)
- UI: Display validation errors inline, show toast for system errors

### Transactional Outbox Pattern
```csharp
using (var transaction = await dbContext.Database.BeginTransactionAsync())
{
    // 1. Save domain entity
    await dbContext.Prospects.AddAsync(prospect);
    
    // 2. Save event to Outbox
    await dbContext.Outbox.AddAsync(new OutboxMessage 
    { 
        EventType = "ProspectCreated",
        Payload = JsonSerializer.Serialize(prospectCreatedEvent)
    });
    
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
// Separate process publishes from Outbox to Event Grid
```

## External Dependencies

- **Azure Service Bus**: Command queues (topic: `identity-commands`)
- **Azure Event Grid**: Event fan-out (topics: `prospect-events`, `student-events`, `instructor-events`)
  - Webhooks configured to POST to API Gateway `/api/events/webhook`
- **Azure SQL**: Transactional DB (free tier), separate read model DB
- **WebSocket Hub**: In-process WebSocket server in ApiGateway service for real-time event push
  - Clients connect to `wss://api.example.com/ws/events`
  - Optional: Service Bus subscription for durable event buffering (reconnect replay)

## Important Notes

- **Never publish events directly** from API controllers—always go through Outbox
- **At-least-once delivery**: All event handlers must be idempotent
- **React Query patterns**: Use mutations for commands, queries for read models; invalidate queries on WebSocket event receipt
- **WebSocket reconnection**: Implement exponential backoff in `useWebSocket` hook; resync state on reconnect
- **Event Grid webhook security**: API Gateway MUST validate Event Grid signature in `aeg-event-type` header before processing
- **Client-side event filtering**: WebSocket clients should subscribe to specific event types; API Gateway filters before pushing
- **React form intelligence**: Use `EventTypePicker` component to dynamically render forms based on selected event type (ProspectCreated needs firstName/lastName/email; ProspectUpdated needs prospectId + fields to update)
- **MVP Scope**: Focus on Prospect Create/Update flow first; Student/Instructor events come later
- **Custom Auth**: JWT tokens issued by `/api/auth/login`, validated via middleware in ApiGateway
