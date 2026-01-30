# ADR-002: CQRS Pattern

**Status:** Accepted  
**Date:** 2026-01-15  
**Deciders:** Architecture Team

## Context

The system needs to handle both transactional writes (creating/updating prospects) and complex read queries (listing prospects with filters, analytics). Traditional CRUD models struggle when read and write requirements differ significantly.

## Decision

We will implement **Command Query Responsibility Segregation (CQRS)** where:
- **Write Side**: Commands modify state (CreateProspectCommand, UpdateProspectCommand)
- **Read Side**: Queries return data from optimized read models
- Separate databases/schemas for write and read models
- Events synchronize write model to read models

## Rationale

### Pros
- **Optimized Performance**: Read models can be denormalized for specific queries
- **Scalability**: Read and write sides can scale independently
- **Flexibility**: Use different storage technologies (SQL for writes, NoSQL for reads)
- **Simplified Domain Logic**: Write model focuses on business rules, not query complexity
- **Event-Driven Synergy**: Natural fit with event-driven architecture

### Cons
- **Eventual Consistency**: Read models lag behind writes
- **Complexity**: Two models to maintain instead of one
- **Development Overhead**: Need to handle projections from events to read models

## Alternatives Considered

### 1. Traditional CRUD with Single Model
- **Rejected**: Complex queries would slow down writes
- ORM performance issues with large result sets
- Difficult to optimize for both read and write patterns

### 2. Read Replicas Only
- **Rejected**: Doesn't address query complexity
- Still requires joins and filtering on transactional schema
- Limited optimization opportunities

## Implementation Details

### Write Side (Command Side)
- **Services**: ProspectService, StudentService, InstructorService
- **Storage**: Azure SQL (normalized schema)
- **Pattern**: Domain-driven design with aggregates
- **Validation**: Business rules enforced before persisting

### Read Side (Query Side)
- **Service**: ProjectionService
- **Storage**: Azure SQL (denormalized read models)
- **Updates**: Event handlers rebuild read models from events
- **Queries**: Simple SQL queries, no complex joins

### Command Flow
```
UI → ApiGateway → Service Bus Queue → Command Handler
    ↓
  Write DB + Outbox
    ↓
  Event Grid → ProjectionService → Read DB
```

### Query Flow
```
UI → ApiGateway → ProjectionService → Read DB → UI
```

## Consequences

### Positive
- API Gateway can query read models directly (fast response)
- Write services focus on business logic, not query optimization
- Can add new read models without changing write side
- Read models can be cached aggressively

### Negative
- Users may see stale data (eventual consistency)
- Need to rebuild read models if projections change
- More code to maintain (commands, events, projections)
- Debugging requires understanding both sides

## Read Model Strategy

### Current Read Models
1. **ProspectListModel**: Optimized for listing/searching prospects
2. **StudentListModel**: Student search and filtering
3. **InstructorListModel**: Instructor lookup

### Future Considerations
- Analytics models for reporting
- Graph models for relationship queries
- Time-series models for audit history

## Testing Strategy

- **Write Side**: Unit tests for command handlers and domain logic
- **Read Side**: Integration tests verifying events → projections
- **E2E Tests**: Verify command → event → projection → query flow

## Compliance

- Read models must handle out-of-order events (idempotency)
- Projections must be rebuildable from event history
- Version read models independently from write models
- Document eventual consistency SLA (expected lag time)

## References

- [Microsoft: CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Martin Fowler: CQRS](https://martinfowler.com/bliki/CQRS.html)
- Project: `src/services/ProjectionService` - Read model implementations
