# ADR-001: Event-Driven Architecture

**Status:** Accepted  
**Date:** 2026-01-15  
**Deciders:** Architecture Team

## Context

The Events project needs to manage identity information for Prospects, Students, and Instructors across multiple microservices. We need a way to keep data consistent while maintaining service independence and scalability.

## Decision

We will use **Event-Driven Architecture** where:
- Services publish domain events when state changes occur
- Other services subscribe to relevant events
- Event Grid acts as the central event distribution mechanism
- Events are immutable facts about what happened

## Rationale

### Pros
- **Loose Coupling**: Services don't need direct knowledge of each other
- **Scalability**: Services can scale independently based on their workload
- **Resilience**: Failures in one service don't cascade to others
- **Audit Trail**: Events provide natural audit log of all changes
- **Real-time Updates**: UI can subscribe to events for instant updates
- **Temporal Decoupling**: Producers and consumers don't need to be online simultaneously

### Cons
- **Eventual Consistency**: Data may be temporarily out of sync across services
- **Complexity**: More moving parts than synchronous HTTP calls
- **Debugging**: Harder to trace request flows across services
- **Testing**: Requires integration tests to verify event flows

## Alternatives Considered

### 1. Synchronous HTTP API Calls
- **Rejected**: Creates tight coupling and cascading failures
- Services would need to know about each other's APIs
- Performance bottleneck when chaining multiple calls

### 2. Shared Database
- **Rejected**: Violates service autonomy principle
- Schema changes impact all services
- Cannot use different database technologies per service

### 3. Message Queue (Service Bus Only)
- **Partially Used**: We use Service Bus for commands, Event Grid for events
- Event Grid provides better fan-out for broadcast scenarios

## Implementation Details

### Technology Choices
- **Event Grid**: Event distribution to multiple subscribers
- **Service Bus**: Command queues for reliable message processing
- **WebSockets**: Real-time event push to React UI

### Event Schema
All events follow this envelope structure:
```json
{
  "eventId": "guid",
  "eventType": "ProspectCreated",
  "schemaVersion": "1.0",
  "occurredAt": "2026-01-29T10:30:00Z",
  "producer": "ProspectService",
  "correlationId": "trace-id",
  "subject": "prospect/123",
  "data": { /* event payload */ }
}
```

## Consequences

### Positive
- Services can evolve independently
- New features can subscribe to existing events without modifying publishers
- Natural support for CQRS pattern (separate read/write models)
- Built-in support for event sourcing if needed in future

### Negative
- Developers must understand eventual consistency
- Need robust monitoring to track event flows
- Requires idempotent event handlers
- More infrastructure components to manage

## Compliance

- Events must be versioned (`schemaVersion` field)
- All events must include correlation ID for tracing
- Events are immutable once published
- Backwards compatibility required when changing event schemas

## References

- [Microsoft: Event-driven architecture style](https://docs.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
- [Martin Fowler: Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
- Project: `docs/Architecture.md` - Event Flow Diagrams
