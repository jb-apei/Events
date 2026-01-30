# ADR-003: Transactional Outbox Pattern

**Status:** Accepted  
**Date:** 2026-01-16  
**Deciders:** Architecture Team

## Context

When a service updates its database and publishes an event, we need both operations to succeed atomically. Network failures, service crashes, or Event Grid unavailability could cause the database to commit while the event fails to publish, leading to data inconsistency.

## Decision

We will implement the **Transactional Outbox Pattern** where:
- Domain events are saved to an `Outbox` table in the same database transaction as the domain entity
- A separate background process (EventRelay) reads unpublished events from the Outbox
- Events are published to Event Grid only after database commit succeeds
- Outbox records are marked as published after successful Event Grid delivery

## Rationale

### Pros
- **Atomicity**: Event persistence guaranteed with database transaction
- **Reliability**: Events never lost, even if Event Grid is down
- **Retry Logic**: Failed events automatically retried by EventRelay
- **At-Least-Once Delivery**: Guaranteed event delivery (subscribers must be idempotent)
- **Audit Trail**: Outbox table serves as event history

### Cons
- **Eventual Consistency**: Slight delay between database commit and event publication
- **Complexity**: Requires EventRelay service and Outbox table management
- **Storage**: Outbox table grows over time (requires cleanup strategy)
- **Idempotency Required**: Subscribers must handle duplicate events

## Alternatives Considered

### 1. Direct Event Grid Publishing
```csharp
// ❌ PROBLEM: Event may publish but DB transaction could still fail
await dbContext.SaveChangesAsync();
await eventGridClient.PublishAsync(@event);
```
**Rejected**: No atomicity guarantee, data inconsistency possible

### 2. Two-Phase Commit (2PC)
**Rejected**: Event Grid doesn't support 2PC transactions, complex to implement

### 3. Event Sourcing
**Rejected**: Too invasive for current design, events become source of truth (not needed for this use case)

## Implementation Details

### Outbox Table Schema
```sql
CREATE TABLE Outbox (
    Id BIGINT PRIMARY KEY IDENTITY,
    EventId NVARCHAR(36) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Subject NVARCHAR(200),
    Payload NVARCHAR(MAX) NOT NULL,
    Published BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PublishedAt DATETIME2 NULL
);

CREATE INDEX IX_Outbox_Published ON Outbox(Published, CreatedAt);
```

### Write Pattern (Command Handler)
```csharp
using (var transaction = await dbContext.Database.BeginTransactionAsync())
{
    // 1. Save domain entity
    await dbContext.Prospects.AddAsync(prospect);
    
    // 2. Save event to Outbox (same transaction)
    var outboxMessage = new OutboxMessage
    {
        EventId = prospectCreatedEvent.EventId,
        EventType = "ProspectCreated",
        Subject = $"prospect/{prospect.Id}",
        Payload = JsonSerializer.Serialize(prospectCreatedEvent)
    };
    await dbContext.Outbox.AddAsync(outboxMessage);
    
    // 3. Commit atomically
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
// EventRelay picks up unpublished events in separate process
```

### EventRelay Service
- Polls Outbox table every 5 seconds
- Publishes events to Event Grid in batches
- Marks events as published on success
- Exponential backoff on failures
- Dead-letter queue for events that fail after 10 retries

## Consequences

### Positive
- Zero data loss guarantee
- Services remain available even if Event Grid is down
- Natural rate limiting (batch publishing prevents Event Grid throttling)
- Easy to replay events by resetting `Published` flag

### Negative
- Additional latency (5-10 seconds) between commit and event delivery
- EventRelay is a single point of failure (mitigated by Container Apps auto-restart)
- Need cleanup job to archive old Outbox records

## Cleanup Strategy

### Retention Policy
- Keep published events for 30 days (compliance/debugging)
- Archive to Azure Blob Storage for long-term retention
- Cleanup job runs daily

### Implementation
```sql
-- Daily cleanup job
DELETE FROM Outbox 
WHERE Published = 1 
  AND PublishedAt < DATEADD(DAY, -30, GETUTCDATE());
```

## Monitoring

### Key Metrics
- **Outbox Lag**: Time between event creation and publication
- **Unpublished Count**: Number of events waiting to publish
- **Failure Rate**: Percentage of events failing to publish
- **Retry Count**: Events requiring multiple publish attempts

### Alerts
- Alert if unpublished count > 1000 (backlog building)
- Alert if average lag > 60 seconds (EventRelay slow/failing)
- Alert if failure rate > 5% (Event Grid connectivity issues)

## Testing Strategy

- **Unit Tests**: Verify Outbox record created in same transaction
- **Integration Tests**: Kill EventRelay mid-publish, verify event still publishes on restart
- **Chaos Tests**: Simulate Event Grid downtime, verify events queue up and replay
- **Performance Tests**: Measure throughput with 10,000 events/minute

## References

- [Microservices.io: Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html)
- [Chris Richardson: Reliable Event Publishing](https://www.chrisrichardson.net/post/microservices/2020/02/21/events-are-the-problem.html)
- Project: `src/services/EventRelay` - Implementation
