# Events Project - Architecture Documentation

## Overview

Event-driven identity management system for managing Prospects, Students, and Instructors using Azure Event Grid and Azure Service Bus with CQRS pattern.

## Technology Stack

### Frontend
- **React** with TypeScript
- **React Query** for state management and caching
- **WebSocket** for real-time event subscriptions
- **Axios/Fetch** for REST API calls

### Backend
- **.NET Core** microservices
- **Azure Container Apps** for hosting
- **Azure SQL** (free tier) for transactional and read models
- **Azure Service Bus** for command queues
- **Azure Event Grid** for event fan-out

### Cross-Cutting
- **Application Insights** for observability
- **OpenTelemetry** for distributed tracing
- **Custom JWT** authentication
- **Azure Key Vault** for secrets management

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)
- **Write Model**: Commands processed by domain services, stored in transactional DB
- **Read Model**: Events consumed to build optimized query models
- Clear separation enables independent scaling and optimization

### Transactional Outbox Pattern
```
┌─────────────────────────────────────────┐
│   Command Handler (ProspectService)    │
│                                         │
│   BEGIN TRANSACTION                     │
│   ├─ Save Domain Entity                │
│   ├─ Save Event to Outbox              │
│   └─ COMMIT                             │
└─────────────────────────────────────────┘
           ↓
┌─────────────────────────────────────────┐
│   EventRelay Service                    │
│   ├─ Poll Outbox (unpublished events)  │
│   ├─ Publish to Event Grid              │
│   └─ Mark as Published                  │
└─────────────────────────────────────────┘
```

**Benefits**:
- Guarantees event publication (no lost events)
- Atomic consistency between DB writes and event publishing
- Resilient to Event Grid downtime

### Idempotency Pattern
- **Inbox Table**: Dedupe events by `eventId` before processing
- **Dedupe Window**: 7 days
- **Strategy**: Check inbox → Process if new → Record in inbox
- **Critical**: All event handlers MUST be idempotent (at-least-once delivery)

## Data Flow

### Write Path (Command Flow)
```
React UI
  ↓ HTTP POST /api/prospects
API Gateway
  ↓ Publish CreateProspectCommand
Service Bus Queue (identity-commands)
  ↓ Consume
ProspectService Command Handler
  ├─ Validate business rules
  ├─ Save Prospect entity to DB
  └─ Save ProspectCreated event to Outbox
  ↓
EventRelay (background job)
  ├─ Poll Outbox table
  └─ Publish ProspectCreated to Event Grid
```

### Read Path (Event Subscription)
```
Event Grid (prospect-events topic)
  ↓ Webhook POST /api/events/webhook
API Gateway
  ├─ Validate Event Grid signature (aeg-event-type header)
  ├─ Authorize event (tenant/user filtering)
  └─ Push to WebSocket clients
  ↓
React UI (WebSocket connection)
  ├─ Receive ProspectCreated event
  ├─ Invalidate React Query cache
  └─ Refresh ProspectList component
```

**Optional Durability Layer** (for reconnect replay):
```
Event Grid → Service Bus Topic → Processor → API Gateway → WebSocket → React
```

### Projection Path (Read Model Updates)
```
Event Grid
  ↓ Push subscription
ProjectionService
  ├─ Check Inbox (dedupe by eventId)
  ├─ Update read model (e.g., ProspectSummary view)
  └─ Record in Inbox
```

## Service Responsibilities

### ProspectService
- **Purpose**: Write model for Prospect aggregate
- **Responsibilities**:
  - Process CreateProspect, UpdateProspect commands from Service Bus
  - Enforce business rules and validation
  - Save domain entities and events to Outbox in single transaction
- **Tech**: .NET Core, EF Core, Azure Service Bus SDK

### EventRelay
- **Purpose**: Reliable event publishing
- **Responsibilities**:
  - Poll Outbox table for unpublished events (e.g., every 5 seconds)
  - Publish events to Event Grid
  - Mark events as published
  - Handle Event Grid throttling and retries
- **Tech**: .NET Core background service, Azure Event Grid SDK

### ApiGateway
- **Purpose**: API entry point and real-time communication hub
- **Responsibilities**:
  - REST API endpoints for commands (`POST /api/prospects`)
  - JWT authentication middleware
  - Event Grid webhook receiver (`POST /api/events/webhook`)
  - WebSocket connection manager (`wss://api.example.com/ws/events`)
  - Filter and push events to authorized WebSocket clients
- **Tech**: .NET Core, ASP.NET Core WebSockets, JWT middleware

### ProjectionService
- **Purpose**: Build and maintain read models
- **Responsibilities**:
  - Subscribe to Event Grid topics
  - Consume ProspectCreated, ProspectUpdated events
  - Update read model tables (e.g., ProspectSummary, ProspectSearch)
  - Implement inbox pattern for idempotency
- **Tech**: .NET Core, Azure Event Grid SDK, EF Core

### React Frontend
- **Purpose**: User interface for event-driven operations
- **Responsibilities**:
  - Event type selection (`EventTypePicker`)
  - Dynamic form rendering (`ProspectForm`)
  - Real-time list updates (`ProspectList`)
  - WebSocket subscription management (`useWebSocket` hook)
  - React Query cache management (`useProspects` hook)
- **Tech**: React, TypeScript, React Query, native WebSocket API

## Event Standards

### Event Envelope (Required Fields)
```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "ProspectCreated",
  "schemaVersion": "1.0",
  "occurredAt": "2026-01-29T10:30:00Z",
  "producer": "ProspectService",
  "correlationId": "trace-abc123",
  "causationId": "command-xyz789",
  "subject": "prospect/12345",
  "data": {
    "prospectId": 12345,
    "firstName": "Jane",
    "lastName": "Doe",
    "email": "jane.doe@example.com"
  }
}
```

### Naming Conventions
- **Events**: Past tense (e.g., `ProspectCreated`, `ProspectUpdated`, `ProspectMerged`)
- **Commands**: Imperative (e.g., `CreateProspect`, `UpdateProspect`, `MergeProspect`)
- **Subjects**: Use format `{entity-type}/{entity-id}` for partitioning

### Versioning
- Use `schemaVersion` field for backward compatibility
- Additive changes only (new optional fields)
- Breaking changes require new event type or version bump with migration strategy

## Security

### Authentication & Authorization
- **Custom JWT tokens**: Issued by `/api/auth/login`
- **API Gateway middleware**: Validates JWT on every request
- **WebSocket auth**: JWT token passed during WebSocket handshake
- **Event Grid webhook**: Validate `aeg-event-type` header signature

### Event Filtering
- API Gateway filters events based on user permissions/tenant
- Only authorized events pushed to WebSocket clients
- No PII in event subjects or metadata (only in `data` payload)

## Observability

### Correlation & Tracing
- **correlationId**: Propagated across all service hops (HTTP headers, Service Bus properties)
- **causationId**: Links events to originating commands
- **Application Insights**: Automatic request/dependency tracking
- **OpenTelemetry**: Distributed trace spans across services

### Monitoring
- **Outbox lag**: Alert if unpublished events > 1 minute old
- **Event Grid DLQ**: Alert on dead-letter queue depth > 10
- **WebSocket connections**: Track active connections and errors
- **Command processing time**: P95 latency metrics

## Data Stores

### Transactional Database (Azure SQL)
- **Tables**: Prospects, Students, Instructors, Outbox, Inbox
- **Schema**: Write model (normalized for command processing)
- **Free Tier**: 32 GB storage, 5 DTUs

### Read Model Database (Azure SQL)
- **Tables**: ProspectSummary, ProspectSearch (denormalized views)
- **Purpose**: Optimized for query performance
- **Updated by**: ProjectionService consuming events

## Deployment

### Azure Resources
- **Azure Container Apps**: ProspectService, EventRelay, ApiGateway, ProjectionService
- **Azure Service Bus**: Namespace with `identity-commands` queue
- **Azure Event Grid**: Custom topics: `prospect-events`, `student-events`, `instructor-events`
- **Azure SQL**: Two databases (transactional, read model)
- **Azure Key Vault**: JWT signing keys, connection strings

### Infrastructure as Code
- **Bicep/Terraform**: All resources defined in `/infrastructure`
- **Environments**: Dev, Test, Prod (isolated resources)

## MVP Scope (Phase 1)

Focus: **Prospect Create and Update only**

### In Scope
- ProspectCreated event
- ProspectUpdated event
- React UI with smart forms
- Real-time WebSocket updates
- Transactional Outbox pattern
- Basic authentication (JWT)

### Out of Scope (Future)
- Student events (Create, Update, Changed)
- Instructor events (Create, Update, Deactivate)
- ProspectMerged event
- Advanced authorization (RBAC)
- Event replay UI
- Multi-tenant isolation

## Key Architectural Decisions

### Why Event Grid + Service Bus?
- **Event Grid**: Pub/sub fan-out, HTTP webhooks, native Azure integration
- **Service Bus**: Durable command queues, DLQ, message ordering (sessions)
- **Separation**: Commands (Service Bus) vs Events (Event Grid)

### Why WebSocket Instead of SignalR?
- **Simplicity**: No SignalR Service dependency for MVP
- **Control**: Direct WebSocket management in ApiGateway
- **Future**: Can migrate to SignalR Service for scale-out later

### Why Custom Auth?
- **MVP Speed**: Avoid Azure AD B2C setup complexity
- **Learning**: Full control over auth flow
- **Migration Path**: Can swap to Azure AD later without changing architecture

### Why Outbox Pattern?
- **Reliability**: No lost events (DB commit = guaranteed event publish)
- **Consistency**: Atomic writes (entity + event)
- **Resilience**: Works even if Event Grid is temporarily unavailable

## Future Enhancements

- **Event Sourcing**: Store full event log per aggregate (optional)
- **Saga Pattern**: Multi-step workflows with compensation
- **Event Replay**: UI to replay events for debugging/testing
- **Multi-tenancy**: Tenant isolation at data and event level
- **GraphQL**: Unified query layer for read models
- **CDC (Change Data Capture)**: Alternative to Outbox pattern
