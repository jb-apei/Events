# EventRelay Service

## Overview
EventRelay is a background worker service that implements the relay component of the **Transactional Outbox Pattern**. It polls the Outbox table for unpublished events and publishes them to Azure Event Grid topics.

## Architecture Pattern

### Transactional Outbox Pattern (Relay Side)
```
┌──────────────────────────────────────────┐
│   Outbox Table (ProspectService DB)     │
│   ├─ Unpublished Events (Published=0)   │
│   └─ Published Events (Published=1)     │
└──────────────────────────────────────────┘
           ↓ Poll every 5 seconds
┌──────────────────────────────────────────┐
│   OutboxRelayService                     │
│   ├─ Fetch up to 100 unpublished events │
│   ├─ Deserialize event payload          │
│   ├─ Publish to Event Grid topic        │
│   └─ Mark as Published on success       │
└──────────────────────────────────────────┘
           ↓
┌──────────────────────────────────────────┐
│   Azure Event Grid Topics                │
│   ├─ prospect-events                     │
│   ├─ student-events                      │
│   └─ instructor-events                   │
└──────────────────────────────────────────┘
```

## Key Features

### 1. Poll-Based Relay
- Polls Outbox table every **5 seconds** for unpublished events
- Processes events in **FIFO order** (oldest first)
- Batch size: **100 events per cycle** for efficiency

### 2. Event Routing
Events are routed to the correct Event Grid topic based on event type:

| Event Type | Event Grid Topic |
|-----------|------------------|
| ProspectCreated, ProspectUpdated, ProspectMerged | `prospect-events` |
| StudentCreated, StudentUpdated, StudentChanged | `student-events` |
| InstructorCreated, InstructorUpdated, InstructorDeactivated | `instructor-events` |

### 3. Retry Logic with Exponential Backoff
- **Transient errors** (429 throttling, 5xx server errors): Retry with exponential backoff
  - Base delay: 2 seconds
  - Max retries: 3 attempts
  - Backoff formula: `delay = 2^attempt * baseDelay`
- **Non-transient errors**: Log and skip (mark as published to avoid infinite retry)

### 4. Idempotency
- Events are marked as `Published=true` only after successful Event Grid publish
- Unpublished events remain in queue for retry on next polling cycle
- Failed deserialization is marked as published to prevent infinite retry loop

### 5. Observability
- Structured logging with **correlation IDs** for distributed tracing
- Logs include:
  - Event ID, Event Type, Topic, Correlation ID
  - Success/failure counts per batch
  - Retry attempts and delays
  - Event Grid response errors
- Ready for Application Insights integration

## Project Structure

```
EventRelay/
├── Infrastructure/
│   ├── OutboxMessage.cs         # Outbox entity (matches ProspectService schema)
│   └── OutboxDbContext.cs       # EF Core context for Outbox table
├── OutboxRelay/
│   ├── IEventPublisher.cs       # Event publisher interface
│   ├── EventGridPublisher.cs   # Azure Event Grid implementation
│   └── OutboxRelayService.cs   # Background service (polling logic)
├── Program.cs                   # Startup configuration
├── appsettings.json             # Configuration (Azure SQL, Event Grid)
└── EventRelay.csproj            # Project dependencies
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "ProspectDb": "Server=...;Database=EventsDb;..."
  },
  "Azure": {
    "EventGrid": {
      "ProspectTopicEndpoint": "https://<topic>.eventgrid.azure.net/api/events",
      "ProspectTopicKey": "<access-key>",
      "StudentTopicEndpoint": "https://<topic>.eventgrid.azure.net/api/events",
      "StudentTopicKey": "<access-key>",
      "InstructorTopicEndpoint": "https://<topic>.eventgrid.azure.net/api/events",
      "InstructorTopicKey": "<access-key>"
    },
    "KeyVault": {
      "VaultUri": "https://<vault-name>.vault.azure.net/"
    },
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=..."
    }
  }
}
```

**Production**: Use Azure Key Vault references:
```json
"ProspectTopicKey": "@Microsoft.KeyVault(SecretUri=https://...)"
```

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.4 | Access Outbox table via EF Core |
| `Azure.Messaging.EventGrid` | 4.28.0 | Publish events to Event Grid |
| `Azure.Identity` | 1.14.1 | Azure authentication (Key Vault) |
| `Shared.Events` | (project ref) | Event envelope contracts |

## Running Locally

### Prerequisites
- .NET 9.0 SDK
- Azure SQL Database (or local SQL Server)
- Azure Event Grid topics (or local emulator)
- ProspectService running (to populate Outbox table)

### Start Service
```bash
cd src/services/EventRelay
dotnet run
```

### Output (Expected Logs)
```
info: EventRelay.OutboxRelay.OutboxRelayService[0]
      OutboxRelayService started. Polling every 5 seconds
info: EventRelay.OutboxRelay.OutboxRelayService[0]
      Processing 3 unpublished events
info: EventRelay.OutboxRelay.EventGridPublisher[0]
      Successfully published event abc-123 (ProspectCreated) to topic 'prospect' (correlation: xyz-789)
info: EventRelay.OutboxRelay.OutboxRelayService[0]
      Batch complete: 3 published, 0 failed
```

## Testing

### Unit Tests
Test individual components in isolation:
- `EventGridPublisher`: Mock Azure Event Grid client
- `OutboxRelayService`: Mock `IEventPublisher` and `OutboxDbContext`

### Integration Tests
Test end-to-end flow:
1. Insert unpublished event into Outbox table
2. Start EventRelay service
3. Verify event is marked as `Published=true`
4. Verify event appears in Event Grid topic (or mock webhook)

### Manual Testing
```bash
# 1. Create a prospect via ProspectService API
POST /api/prospects
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com"
}

# 2. Check Outbox table
SELECT * FROM Outbox WHERE Published = 0;

# 3. Start EventRelay
dotnet run

# 4. Verify event is published
SELECT * FROM Outbox WHERE Published = 1;
```

## Deployment (Azure Container Apps)

### Environment Variables
```bash
ConnectionStrings__ProspectDb=@Microsoft.KeyVault(...)
Azure__EventGrid__ProspectTopicEndpoint=https://...
Azure__EventGrid__ProspectTopicKey=@Microsoft.KeyVault(...)
```

### Health Checks (Optional)
Add health endpoint to monitor service status:
- Check: Can connect to SQL database
- Check: Can reach Event Grid topics
- Expose: `/health` endpoint for Kubernetes probes

## Error Handling

### Transient Errors
- **429 Throttling**: Exponential backoff, max 3 retries
- **5xx Server Errors**: Exponential backoff, max 3 retries
- **Network Timeouts**: Caught and logged, retry on next polling cycle

### Non-Transient Errors
- **Malformed Event Payload**: Log error, mark as published (skip)
- **Unknown Event Type**: Log error, mark as published (skip)
- **Invalid Topic Configuration**: Log error, mark as published (skip)

### Circuit Breaker (Future Enhancement)
Consider adding Polly circuit breaker for Event Grid calls to prevent cascading failures.

## Performance Tuning

### Batch Size
- Default: **100 events per cycle**
- Increase for higher throughput (trade-off: larger DB transactions)
- Decrease for faster feedback loop (trade-off: more DB queries)

### Polling Interval
- Default: **5 seconds**
- Decrease for real-time requirements (trade-off: higher DB load)
- Increase for cost savings (trade-off: higher latency)

### Scaling
- **Horizontal Scaling**: Deploy multiple EventRelay instances
  - Requires distributed locking (e.g., Azure Storage Blob lease) to prevent duplicate publishing
  - Or partition Outbox table by event type and assign instances to partitions

## Monitoring

### Key Metrics (Application Insights)
- Events published per minute
- Publish success/failure rate
- Average batch processing time
- Outbox backlog size (unpublished events)
- Event Grid latency (p50, p95, p99)

### Alerts
- **Outbox Backlog > 1000**: Event Grid may be down or overloaded
- **Publish Failure Rate > 5%**: Investigate Event Grid connectivity
- **Batch Processing Time > 30s**: Consider increasing batch size or scaling horizontally

## Future Enhancements
1. **Change Data Capture (CDC)**: Replace polling with SQL Server CDC for near-zero latency
2. **Dead Letter Queue**: Route permanently failed events to DLQ for manual investigation
3. **Distributed Locking**: Enable safe horizontal scaling with multiple instances
4. **Metrics Dashboard**: Grafana dashboard for real-time monitoring
5. **Event Grid Batch Publishing**: Publish multiple events in single API call for efficiency
