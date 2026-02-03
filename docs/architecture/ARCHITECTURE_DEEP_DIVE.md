# Architecture Deep Dive

This document provides a detailed technical view of the implemented architecture for the Events project, reflecting the deployed state as of February 2026.

## 1. System Architecture Diagram

```mermaid
graph TD
    User[User / Browser]
    
    subgraph "Azure Cloud"
        subgraph "Container Apps Environment"
            Frontend[Frontend React App]
            ApiGateway[API Gateway (BFF)]
            ProspectService[Prospect Service]
            EventRelay[Event Relay Service]
        end
        
        subgraph "Data & Messaging"
            SQL[Azure SQL Database]
            EventGrid[Azure Event Grid]
            ServiceBus[Azure Service Bus]
        end
    end

    User -->|HTTPS / WSS| Frontend
    Frontend -->|REST API| ApiGateway
    Frontend -->|WebSocket| ApiGateway
    
    ApiGateway -->|Commands| ServiceBus
    ApiGateway -->|Queries| SQL
    
    ServiceBus -->|Consume| ProspectService
    ProspectService -->|Write Transaction| SQL
    
    SQL -->|Poll Outbox| EventRelay
    EventRelay -->|Publish| EventGrid
    
    EventGrid -->|Webhook| ApiGateway
    ApiGateway -->|Push Event| User
```

## 2. Component Implementation Details

### 2.1 Frontend (React + Vite)
**Location**: `src/frontend`

The frontend is a Single Page Application (SPA) built with React 18, TypeScript, and Vite. It serves as the primary user interface.

*   **Hosting**: Deployed as a Docker container in Azure Container Apps. Nginx serves static assets.
*   **Authentication**:
    *   Uses **JWT (JSON Web Tokens)** stored in `localStorage`.
    *   `App.tsx` contains a lightweight JWT parser to extract user claims (Email) for the **User Greeting** in the header.
*   **Real-time Updates**:
    *   **`useWebSocket` Hook**: Manages the WebSocket connection to the API Gateway.
    *   **Logic**:
        *   Auto-reconnects with exponential backoff.
        *   Handles `ping`/`pong` to keep connections alive through load balancers.
        *   Listens for `ProspectCreated`/`ProspectUpdated` events.
    *   **React Query Integration**: Upon receiving an event, the hook calls `invalidateQueries(['prospects'])`, triggering an automatic refetch of the list.
*   **Versioning**:
    *   Displays the build version (Time-based tag, e.g., `v20260202-234220`) in the header.
    *   Injected at build time via `ARG BUILD_VERSION` and `ENV VITE_APP_VERSION`.

### 2.2 API Gateway (Backend for Frontend)
**Location**: `src/services/ApiGateway`

The API Gateway acts as a BFF, handling cross-cutting concerns and routing.

*   **Authentication**: Validates JWTs on incoming requests.
*   **WebSocket Hub**:
    *   **Endpoint**: `/ws/events`
    *   **Handler**: `WebSocketHandler.cs`
    *   **Connection Management**: Maintains a thread-safe dictionary of active WebSocket connections mapped to User IDs.
    *   **Keep-Alive**: Server sends periodic pings (every 45s) to prevent idle timeouts from Azure Load Balancer (default 4 mins).
*   **Event Ingestion (Webhook)**:
    *   **Endpoint**: `POST /api/events/webhook`
    *   **Controller**: `EventsController.cs`
    *   **Function**: Receives events from Azure Event Grid.
    *   **Validation**: Accepts both CloudEvents (Dev) and Event Grid schema (Prod).
    *   **Broadcasting**: Pushes the received event payload to all connected WebSocket clients via the `WebSocketHandler`.

### 2.3 Event Relay (Outbox Processor)
**Location**: `src/services/EventRelay`

Implements the **Transactional Outbox** pattern to guarantee event delivery.

*   **Operation**: Background worker service (IHostedService).
*   **Polling**: Periodically queries the `Outbox` table in the SQL Database for records where `Published = 0`.
*   **Publishing**: Sends events to the corresponding **Azure Event Grid Topic**.
*   **Reliability**: Updates the `Outbox` record to `Published = 1` only after successful confirmation from Event Grid.

### 2.4 Prospect Service
**Location**: `src/services/ProspectService`

The core domain service for managing prospects.

*   **Pattern**: CQRS (Command Query Responsibility Segregation).
*   **Write Model**:
    *   Accepts commands via Service Bus (or direct API call in MVP).
    *   Writes to `Prospects` table.
    *   Writes `ProspectCreated` event to `Outbox` table **in the same database transaction**.

## 3. Deployment & Infrastructure

### 3.1 Containerization
All services are Dockerized using multi-stage builds (`mcr.microsoft.com/dotnet/sdk:9.0` for build, `aspnet:9.0-alpine` or `node:20-alpine` for runtime).

### 3.2 Deployment Pipeline
Deployments are managed via PowerShell scripts (`scripts/deploy.ps1`) or GitHub Actions.

1.  **Tagging**: `v{yyyyMMdd-HHmmss}` (e.g., `v20260202-234220`).
2.  **Build**: Docker images built and pushed to **Azure Container Registry (ACR)** `acreventsdevrcwv3i`.
3.  **Update**: Azure Container Apps revision updated with the new image tag.
4.  **Config**: Environment variables (`VITE_APP_VERSION`, connection strings) injected during update.

### 3.3 Infrastructure as Code.
Managed via **Terraform** in `infrastructure/`.
*   Resources: Resource Group, Container Apps Environment, SQL Server, Service Bus Namespace, Event Grid Topics.

## 4. Current Configuration (Dev Environment)

| Setting | Value |
| :--- | :--- |
| **Resource Group** | `rg-events-dev` |
| **Container App Env** | `cae-events-dev` |
| **SQL Database** | `db-events-transactional-dev` |
| **Event Grid Topic** | `prospect-events` |
| **Frontend URL** | `https://ca-events-frontend-dev...` |
| **API Gateway URL** | `https://ca-events-api-gateway-dev...` |

## 5. Recent Architecture Decisions (ADR Ref)

*   **WebSocket Keep-Alive**: Implemented server-side pings to handle Azure Load Balancer idle timeouts.
*   **Client-Side Versioning**: Baked build timestamp into frontend assets for easier debugging.
*   **User Context**: JWT claims used for lightweight user personalization (Greeting).
