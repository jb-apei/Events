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

### ApiGateway
- **Purpose**: API entry point, real-time communication hub, and outbox management
- **Responsibilities**:
  - REST API endpoints for commands (`POST /api/prospects`, `/api/students`, `/api/instructors`)
  - JWT authentication middleware (optional in development mode)
  - Event Grid webhook receiver (`POST /api/events/webhook`)
  - WebSocket connection manager (`/ws/events`) - up to 50 connections per user
  - Filter and push events to authorized WebSocket clients
  - Outbox management API (`GET /api/outbox`, `POST /api/outbox/{id}/retry`)
  - CORS configuration for frontend
- **Tech**: .NET Core 8.0, ASP.NET Core WebSockets, JWT middleware
- **Configuration**: Development mode allows unauthenticated WebSocket access

### ProspectService
- **Purpose**: Write model for Prospect aggregate
- **Responsibilities**:
  - Process CreateProspect, UpdateProspect, MergeProspect commands from Service Bus
  - Enforce business rules and validation
  - Save Prospect entities and events to Outbox in single transaction
  - Implement transactional outbox pattern
- **Tech**: .NET Core 8.0, EF Core, Azure Service Bus SDK, Azure SQL

### StudentService
- **Purpose**: Write model for Student aggregate
- **Responsibilities**:
  - Process CreateStudent, UpdateStudent, StudentChanged commands
  - Enforce business rules and validation
  - Save Student entities and events to Outbox in single transaction
- **Tech**: .NET Core 8.0, EF Core, Azure Service Bus SDK, Azure SQL

### InstructorService
- **Purpose**: Write model for Instructor aggregate
- **Responsibilities**:
  - Process CreateInstructor, UpdateInstructor, DeactivateInstructor commands
  - Enforce business rules and validation
  - Save Instructor entities and events to Outbox in single transaction
- **Tech**: .NET Core 8.0, EF Core, Azure Service Bus SDK, Azure SQL

### EventRelay
- **Purpose**: Reliable event publishing from Outbox to Event Grid
- **Responsibilities**:
  - Poll Outbox table for unpublished events (configurable interval, default 5 seconds)
  - Publish events to appropriate Event Grid topics
  - Mark events as published after successful delivery
  - Handle Event Grid throttling and retries
  - Support multiple outbox sources (Prospect, Student, Instructor databases)
- **Tech**: .NET Core 8.0 background service, Azure Event Grid SDK, Polly for retries

### ProjectionService
- **Purpose**: Build and maintain read models from domain events
- **Responsibilities**:
  - Subscribe to Event Grid topics via webhook subscriptions
  - Consume ProspectCreated, ProspectUpdated, StudentCreated, StudentUpdated, InstructorCreated, InstructorUpdated events
  - Update read model tables (ProspectSummary, StudentSummary, InstructorSummary)
  - Implement Inbox pattern for idempotency (7-day dedupe window)
  - Query API for read models (`GET /api/prospects`, `/api/students`, `/api/instructors`)
- **Tech**: .NET Core 8.0, EF Core, Azure Event Grid webhooks, Azure SQL

### Frontend
- **Purpose**: User interface for identity management
- **Responsibilities**:
  - Display and manage Prospects, Students, and Instructors
  - Real-time updates via WebSocket connection
  - React Query for state management and caching
  - Cache invalidation on WebSocket event receipt
  - Form validation and error handling
  - Event type selection (`EventTypePicker`)
  - Dynamic form rendering based on selected event
  - Real-time list updates
- **Tech**: React 18, TypeScript, Vite, React Query, Axios
- **WebSocket**: Connects to `/ws/events` endpoint for real-time event streaming

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

**Hosting:**
- **Azure Container Apps Environment**: `cae-events-dev` (shared environment for all services)
- **Container Apps** (7 total):
  - `ca-events-api-gateway-dev` (external ingress, HTTP/WebSocket)
  - `ca-events-prospect-service-dev` (internal only)
  - `ca-events-student-service-dev` (internal only)
  - `ca-events-instructor-service-dev` (internal only)
  - `ca-events-event-relay-dev` (internal only, background job)
  - `ca-events-projection-service-dev` (internal only, webhook receiver)
  - `ca-events-frontend-dev` (external ingress, HTTPS)

**Messaging:**
- **Azure Service Bus**: `sb-events-dev-rcwv3i`
  - Queue: `identity-commands`
  - Topics: `prospect-dlq` (dead-letter queue)
- **Azure Event Grid**: 
  - Topics: `evgt-events-prospect-events-dev`, `evgt-events-student-events-dev`, `evgt-events-instructor-events-dev`
  - Subscriptions: Webhook to ProjectionService

**Data:**
- **Azure SQL Server**: `sql-events-dev`
  - Database: `db-events-transactional-dev` (write models + Outbox)
  - Database: `db-events-readmodel-dev` (projections)
  - Tier: Basic (5 DTU, 2 GB storage)
  - Firewall: Allow Azure services

**Observability:**
- **Application Insights**: `appi-events-dev` (all services configured)
- **Log Analytics Workspace**: `log-events-dev` (centralized logging)

**Security:**
- **Azure Key Vault**: `kv-events-dev-rcwv3i`
  - Secrets: Service Bus connection, SQL connections, App Insights keys
  - Access: Managed identities for each container app
- **Azure Container Registry**: `acreventsdevrcwv3i`
  - All service images stored here
  - ACR pull permissions via managed identities

### Infrastructure as Code

- **Terraform**: All resources defined in `/infrastructure` directory
- **Modules**: Custom modules for Container Apps and Event Grid Topics
- **Azure Verified Modules (AVM)**: Used for Service Bus, SQL, Key Vault, Log Analytics
- **State**: Terraform state stored locally (consider Azure Storage for production)
- **Environments**: Currently dev only (resource names include `dev` suffix)

### Deployment Pipeline

**GitHub Actions** (`.github/workflows/deploy-azure.yml`):
1. **Build Job**: Builds all 7 images in ACR from GitHub source
2. **Terraform Job**: Applies infrastructure changes
3. **Restart Job**: Updates container apps to pull latest images

**Authentication**: Service principal with client secret (not OIDC)

**Secrets Required**:
- `ARM_CLIENT_ID`
- `ARM_CLIENT_SECRET`
- `ARM_SUBSCRIPTION_ID`
- `ARM_TENANT_ID`

## Secrets & Configuration Workflow

The system uses a strict "Zero Trust" approach where secrets are generated proactively and injected at runtime.

### 1. Build Time (GitHub Actions)
- **Source**: GitHub Repository Secrets (`SQL_ADMIN_PASSWORD`, `ARM_CLIENT_SECRET`).
- **Consumer**: Terraform (via `deploy-azure.yml`).
- **Action**: Terraform receives these initial credentials to provision the Azure infrastructure (SQL Server, Service Bus, etc.).

### 2. Provisioning Time (Terraform)
- **Action**: Terraform creates resources (e.g., SQL Database, Service Bus Namespace).
- **Generation**: Terraform constructs connection strings using the resource outputs (e.g., `Server=tcp:sql-server...`).
- **Storage**: Terraform writes these connection strings directly into **Azure Key Vault** as secrets (e.g., `sql-transactional-connection`).
- **Mapping**: Terraform configures **Container Apps** with environment variables that reference the Key Vault URIs (e.g., `secretref:sql-transactional-connection`).

### 3. Usage Time (Microservices)
- **Identity**: Each microservice runs with a **System-Assigned Managed Identity**.
- **Access**: The Managed Identity is granted `Key Vault Secrets User` permission via Terraform.
- **Boot**: When the container starts, the Azure platform automatically resolves the Key Vault references and injects the actual values as environment variables.
- **Result**: No secrets are ever stored in source code, Docker images, or plain text configuration files.

### 4. Local Development
- **Source**: User Secrets (`secrets.json`) managed by `.NET CLI`.
- **Setup**: `setup-user-secrets.ps1` script mimics the Key Vault keys locally.
- **Workflow**: Developers run the script to initialize their local environment with safe default credentials (e.g., for local SQL Docker container).

### Resource URLs

- **Frontend**: https://ca-events-frontend-dev.icyhill-68ffa719.westus2.azurecontainerapps.io
- **API Gateway**: https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io
- **WebSocket**: wss://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/ws/events

## Implementation Status

### Completed (Production Ready)

**Services:**
- ✅ ApiGateway (REST API + WebSocket hub + Outbox management)
- ✅ ProspectService (Create, Update commands)
- ✅ StudentService (Create, Update, Changed commands)
- ✅ InstructorService (Create, Update, Deactivate commands)
- ✅ EventRelay (Outbox publisher)
- ✅ ProjectionService (Read model updates)
- ✅ Frontend (React UI with real-time updates)

**Infrastructure:**
- ✅ Azure Container Apps deployment
- ✅ Service Bus queue for commands
- ✅ Event Grid topics for all three aggregates
- ✅ SQL databases (transactional + read models)
- ✅ Key Vault for secrets
- ✅ Application Insights for monitoring
- ✅ GitHub Actions CI/CD pipeline

**Features:**
- ✅ Transactional Outbox pattern
- ✅ WebSocket real-time updates (50 connections/user)
- ✅ Inbox pattern for idempotency (7-day dedupe)
- ✅ React Query state management
- ✅ Development mode (unauthenticated WebSocket)
- ✅ Outbox management API
- ✅ Automated deployment scripts

### Planned (Future Enhancements)

- 🔜 JWT authentication implementation (structure in place, not enforced)
- 🔜 RBAC/multi-tenancy authorization
- 🔜 ProspectMerged event and command
- 🔜 Advanced query capabilities in ProjectionService
- 🔜 Event replay functionality
- 🔜 Service Bus DLQ monitoring and retry
- 🔜 Comprehensive integration tests
- 🔜 Production environment configuration
- 🔜 Azure Storage backend for Terraform state
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
