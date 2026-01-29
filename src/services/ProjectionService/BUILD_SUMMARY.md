# ProjectionService Build Summary

**Date**: January 29, 2026  
**Service**: ProjectionService (Read Model Projections)  
**Status**: ✅ Successfully Built

---

## 📋 Overview

ProjectionService is an event-driven read model projection service that subscribes to Azure Event Grid and builds denormalized read models optimized for fast queries. It implements the **Inbox pattern** for idempotency and follows CQRS architecture with a separate database from the write model.

---

## 🏗️ Architecture

### Event Flow
```
Azure Event Grid (prospect-events topic)
    ↓ HTTP POST /events/webhook
EventGridWebhookController
    ↓
EventDispatcher (routes by event type)
    ↓
ProspectEventHandler (check Inbox → process → record)
    ↓
ProjectionDbContext (update read model)
```

### Key Patterns Implemented
- **Event Grid Push Subscription**: Webhook-based event delivery (not polling)
- **Inbox Pattern**: 7-day deduplication window using eventId
- **Idempotent Handlers**: All handlers check Inbox before processing
- **Eventually Consistent**: Read models built asynchronously from events
- **Separate Database**: Read model DB isolated from write model DB

---

## 📁 Files Created

### Core Infrastructure
- ✅ `Data/ProjectionDbContext.cs` - EF Core DbContext for read models and Inbox
- ✅ `Data/InboxMessage.cs` - Inbox table entity for event deduplication
- ✅ `Projections/ProspectSummary.cs` - Read model for Prospect list queries

### Event Handlers
- ✅ `EventHandlers/ProspectEventHandler.cs` - Handles ProspectCreated, ProspectUpdated, ProspectMerged
- ✅ `EventHandlers/StudentEventHandler.cs` - Stub for future Student events
- ✅ `EventHandlers/InstructorEventHandler.cs` - Stub for future Instructor events

### Services
- ✅ `Services/EventDispatcher.cs` - Routes Event Grid events to appropriate handlers
- ✅ `Services/InboxCleanupService.cs` - Background service to clean old Inbox entries (6-hour intervals)

### API Controllers
- ✅ `Controllers/EventGridWebhookController.cs` - Webhook endpoint for Event Grid push subscriptions

### Configuration
- ✅ `Program.cs` - Application startup with DI, EF Core, controllers, health checks
- ✅ `appsettings.json` - Production configuration template
- ✅ `appsettings.Development.json` - Local development settings
- ✅ `ProjectionService.csproj` - NuGet packages and project references
- ✅ `README.md` - Comprehensive documentation

### Database Migrations
- ✅ `Migrations/20260129_InitialCreate.cs` - Initial database schema (Inbox + ProspectSummary)

---

## 📦 NuGet Packages Added

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | SQL Server provider for EF Core |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | EF Core migration tools |
| Azure.Messaging.EventGrid | 4.30.0 | Event Grid client for webhook processing |
| Microsoft.Extensions.Hosting | 9.0.4 | Background service hosting |

### Project References
- ✅ `Shared.Events` - Event contract definitions (ProspectCreated, ProspectUpdated, etc.)

---

## 🗄️ Database Schema

### Inbox Table (Idempotency)
```sql
CREATE TABLE Inbox (
    EventId NVARCHAR(100) PRIMARY KEY,        -- Unique event identifier
    EventType NVARCHAR(100) NOT NULL,         -- e.g., "ProspectCreated"
    ProcessedAt DATETIME2 NOT NULL,           -- Processing timestamp
    CorrelationId NVARCHAR(100),              -- Distributed tracing
    Subject NVARCHAR(200),                    -- e.g., "prospect/123"
    INDEX IX_Inbox_ProcessedAt (ProcessedAt), -- For cleanup queries
    INDEX IX_Inbox_EventType (EventType)      -- For analytics
)
```

### ProspectSummary Table (Read Model)
```sql
CREATE TABLE ProspectSummary (
    ProspectId INT PRIMARY KEY,               -- Domain entity ID
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    Status NVARCHAR(50) NOT NULL DEFAULT 'New',
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    Version INT NOT NULL,                      -- Optimistic concurrency
    UNIQUE INDEX IX_ProspectSummary_Email (Email),
    INDEX IX_ProspectSummary_Status (Status),
    INDEX IX_ProspectSummary_CreatedAt (CreatedAt),
    INDEX IX_ProspectSummary_UpdatedAt (UpdatedAt)
)
```

---

## 🎯 Event Handlers Implemented

### ProspectEventHandler
| Event | Handler Method | Behavior |
|-------|---------------|----------|
| ProspectCreated | `HandleProspectCreatedAsync()` | Creates new ProspectSummary projection |
| ProspectUpdated | `HandleProspectUpdatedAsync()` | Updates existing ProspectSummary |
| ProspectMerged | `HandleProspectMergedAsync()` | Removes source prospect from read model |

**Idempotency Strategy**:
1. Check if `eventId` exists in Inbox
2. If found → Skip processing (already handled)
3. If not found → Process event + insert into Inbox (atomic transaction)

**Out-of-Order Handling**:
- If ProspectUpdated arrives before ProspectCreated → Create projection from update event
- Defensive checks for missing entities

### StudentEventHandler (Stub)
- Logs "Not implemented yet" for all Student events
- Ready for future implementation

### InstructorEventHandler (Stub)
- Logs "Not implemented yet" for all Instructor events
- Ready for future implementation

---

## 🔌 API Endpoints

### Event Grid Webhook
**POST** `/events/webhook`
- Receives Event Grid push subscriptions
- Handles subscription validation handshake
- Routes events to appropriate handlers
- Returns 200 OK on success, 500 on failure (triggers retry)

### Health Check
**GET** `/health`
- Returns service health status
- Includes database connectivity check

---

## ⚙️ Configuration

### Connection Strings
```json
{
  "ConnectionStrings": {
    "ProjectionDatabase": "Server=localhost;Database=EventsReadModel;..."
  }
}
```

**Development**: Uses `(localdb)\\mssqllocaldb`  
**Production**: Should use Azure SQL connection string from Key Vault

### Event Grid Settings
```json
{
  "EventGrid": {
    "WebhookEndpoint": "/events/webhook",
    "Topics": {
      "ProspectEvents": "prospect-events",
      "StudentEvents": "student-events",
      "InstructorEvents": "instructor-events"
    }
  }
}
```

### Inbox Cleanup
```json
{
  "InboxCleanup": {
    "RetentionDays": 7,
    "CleanupIntervalHours": 6
  }
}
```

---

## 🚀 Running Locally

### 1. Prerequisites
- .NET 9.0 SDK
- SQL Server or LocalDB
- Azure Event Grid emulator (optional)

### 2. Setup Database
```bash
cd src/services/ProjectionService
dotnet ef database update
```

This creates:
- `EventsReadModel` database
- `Inbox` table
- `ProspectSummary` table

### 3. Run Service
```bash
dotnet run
```

Service starts on `http://localhost:5002` (configurable in `launchSettings.json`)

### 4. Test Webhook

**Test subscription validation**:
```bash
curl -X POST http://localhost:5002/events/webhook \
  -H "Content-Type: application/json" \
  -d '[{
    "id": "validation-123",
    "eventType": "Microsoft.EventGrid.SubscriptionValidationEvent",
    "subject": "",
    "data": {"validationCode": "test-code-123"},
    "eventTime": "2026-01-29T10:00:00Z",
    "dataVersion": "1.0"
  }]'
```

**Expected Response**:
```json
{"validationResponse": "test-code-123"}
```

**Send test ProspectCreated event**:
```bash
curl -X POST http://localhost:5002/events/webhook \
  -H "Content-Type: application/json" \
  -d '[{
    "id": "event-123",
    "eventType": "ProspectCreated",
    "subject": "prospect/42",
    "data": {
      "eventId": "evt-001",
      "eventType": "ProspectCreated",
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
  }]'
```

**Verify in database**:
```sql
SELECT * FROM ProspectSummary WHERE ProspectId = 42
SELECT * FROM Inbox WHERE EventId = 'evt-001'
```

---

## 📊 Monitoring & Observability

### Logging
- Console output (structured logging)
- Debug provider for development
- Application Insights ready (add instrumentation key)

### Key Log Events
- `"Processing {EventType} event {EventId} for Prospect {ProspectId}"`
- `"Event {EventId} ({EventType}) already processed. Skipping."` ← Inbox dedupe
- `"Deleted {Count} old inbox entries"` ← Cleanup service

### Health Checks
- `/health` endpoint for orchestrator probes
- Database connectivity check

---

## 🔧 Next Steps

### Immediate (Production Readiness)
1. **Deploy to Azure Container Apps**
   ```bash
   az containerapp create \
     --name projection-service \
     --resource-group events-rg \
     --environment events-env \
     --image events/projection-service:latest \
     --target-port 8080 \
     --ingress external
   ```

2. **Create Event Grid Subscription**
   ```bash
   az eventgrid event-subscription create \
     --name projection-service-subscription \
     --source-resource-id /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.EventGrid/topics/prospect-events \
     --endpoint https://projection-service.azurecontainerapps.io/events/webhook \
     --included-event-types ProspectCreated ProspectUpdated ProspectMerged \
     --max-delivery-attempts 10
   ```

3. **Configure Key Vault**
   - Store `ProjectionDatabase` connection string
   - Update `appsettings.json` to use `@Microsoft.KeyVault(...)` syntax

4. **Enable Application Insights**
   - Add instrumentation key to configuration
   - Verify distributed tracing with `correlationId`

### Future Enhancements
- [ ] Implement StudentEventHandler and InstructorEventHandler
- [ ] Add GraphQL query API for read models
- [ ] Implement read model snapshots for faster rebuilds
- [ ] Add event replay capability for full read model reconstruction
- [ ] Implement multi-tenant filtering in EventDispatcher
- [ ] Add Redis caching layer for frequently accessed projections
- [ ] Create dashboards in Application Insights for event processing metrics

---

## ⚠️ Important Implementation Notes

### Idempotency
- **Critical**: All event handlers MUST check Inbox before processing
- **Dedupe Window**: 7 days (configurable via `InboxCleanup.RetentionDays`)
- **Cleanup Interval**: Every 6 hours (configurable via `InboxCleanup.CleanupIntervalHours`)
- Old Inbox entries are automatically deleted by `InboxCleanupService`

### Event Grid Webhook
- **Subscription Validation**: Handled automatically in `EventGridWebhookController`
- **Retry Policy**: Returns 500 on failure to trigger Event Grid retry (up to 10 attempts)
- **Timeout**: Event Grid expects response within 30 seconds

### Transactional Consistency
- Inbox insert and projection update are in **same EF Core transaction**
- If projection fails → Inbox not inserted → Event Grid retries entire operation
- Atomic guarantee: either both succeed or both fail

### Error Handling
- Transient errors (SQL timeout) → Throw exception → Event Grid retries
- Business errors (invalid data) → Log + skip (no retry) - **NOT IMPLEMENTED YET**
- Out-of-order events → Defensive handling (create if missing)

### Performance Considerations
- **Indexes**: Created on Email (unique), Status, CreatedAt, UpdatedAt
- **Optimistic Concurrency**: `Version` column prevents concurrent update conflicts
- **Connection Pooling**: Enabled by default in EF Core
- **Retry Logic**: SQL Server retry policy (5 attempts, 30-second max delay)

---

## 📝 Build Warnings

### Resolved
- ✅ NETSDK1086: AspNetCore.App framework reference (removed, implicit in SDK)
- ✅ CS1998: Async method without await (acceptable for stubs)

---

## ✅ Checklist

- [x] DbContext with Inbox and ProspectSummary
- [x] ProspectEventHandler with all 3 events (Created, Updated, Merged)
- [x] Student and Instructor event handler stubs
- [x] Event Grid webhook controller with validation
- [x] EventDispatcher for routing events
- [x] Inbox cleanup background service
- [x] EF Core migrations (InitialCreate)
- [x] NuGet packages and project references
- [x] Configuration files (appsettings.json)
- [x] Comprehensive README
- [x] Builds successfully ✅
- [x] Ready for local testing
- [ ] Deployed to Azure (pending)
- [ ] Event Grid subscription configured (pending)

---

**Status**: ✅ ProjectionService is fully implemented and ready for testing.
