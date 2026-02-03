# Architecture Deep Dive: Event-Driven CQRS Flow

This document details the architectural components, their responsibilities, and the data flow within the Events project. The system follows an **Event-Driven Microservices** architecture using the **CQRS** (Command Query Responsibility Segregation) pattern.

## 1. High-Level Architecture

The system is divided into two distinct responsibilities:
1.  **Write Side (Command)**: Handling business logic, validation, and state changes (e.g., "Create Student").
2.  **Read Side (Query)**: serving data efficiently to the UI (e.g., "Get Student List").

These two sides are decoupled using **Azure Service Bus** (for commands) and **Azure Event Grid** (for events).

---

## 2. Component Deep Dive

### A. API Gateway (`src/services/ApiGateway`)
The entry point for all frontend traffic. It is **not** just a proxy; it acts as an intelligent orchestrator.
*   **Controllers**:
    *   `AuthController`: Issues JWT tokens.
    *   `StudentsController` / `ProspectsController`: Receives Write requests (POST/PUT) and dispatches Commands to Service Bus.
    *   `EventsController`: Webhook endpoint that receives events from Event Grid and pushes them to WebSockets.
*   **WebSockets**: Maintains live connections with the React Frontend to push real-time updates when data changes.
*   **Read Proxy**: Queries the Read Model directly (or proxies to ProjectionService) to serve GET requests.

### B. Domain Services (Write Side)
These services own the business rules and the "Source of Truth" transactional database.
*   **Services**: `ProspectService`, `StudentService`, `InstructorService`.
*   **Responsibilities**:
    *   Consume **Commands** from Azure Service Bus (e.g., `CreateStudentCommand`).
    *   Validate business rules (e.g., "Email must be unique").
    *   **Transactional Outbox**: Saves the entity (Student) AND the event (`StudentCreated`) to the database in a single atomic transaction.
*   **Controllers**: Mostly internal or for debugging. Production traffic arrives via Service Bus.

### C. Event Relay (`src/services/EventRelay`)
A dedicated background worker ensures reliability.
*   **Role**: Polls the `Outbox` table of the Domain Services.
*   **Action**: Publishes unsent events to **Azure Event Grid**.
*   **Why**: Ensures that even if the network fails during a DB save, the event is eventually published (At-Least-Once Delivery).

### D. Projection Service (`src/services/ProjectionService`)
The "Read Side" processor.
*   **Role**: Subscribes to Azure Event Grid topics.
*   **Action**: "Projects" complex events into simple, flat tables (`ProspectSummary`, `StudentSummary`) in the **Read Model Database**.
*   **Why**: allows the UI to query complex data relations instantly without complex SQL joins across microservices.

---

## 3. The Data Flow (Step-by-Step)

### Scenario: Creating a Student

#### Phase 1: The Command (Write)
1.  **User Action**: User fills out the form on the React Frontend and clicks "Submit".
2.  **Request**: Frontend sends `POST /api/students` to **API Gateway**.
3.  **Dispatch**: API Gateway wraps this in a `CreateStudentCommand` and sends it to the **Azure Service Bus** queue (`identity-commands`).
4.  **Processing**:
    *   **StudentService** picks up the message.
    *   Validates the data.
    *   Saves the new Student to the `StudentDb`.
    *   Saves a `StudentCreated` event to the `Outbox` table.
    *   *(Transaction Commits)*.

#### Phase 2: The Propagation (Async)
5.  **Relay**: **EventRelay** service sees the new row in the `Outbox` table.
6.  **Publish**: Relay sends the `StudentCreated` event to the **Azure Event Grid** topic (`evgt-events-student-events-dev`).

#### Phase 3: The Consuming (Read & Notify)
The Event Grid "Fans Out" the event to multiple subscribers in parallel:

*   **Path A: The Projection (Storage)**
    1.  **ProjectionService** receives the webhook from Event Grid.
    2.  It processes `StudentCreated`.
    3.  It inserts a new row into the `StudentSummary` table in the `ReadModelDb`.
    4.  *The data is now ready to be queried.*

*   **Path B: The Notification (Real-time)**
    1.  **API Gateway** receives the webhook from Event Grid.
    2.  It identifies which WebSocket clients care about students.
    3.  It pushes a JSON message: `{ "type": "StudentCreated", "id": 123 }`.
    4.  **React Frontend** receives the message, invalidates its cache, and re-fetches the list options (Phase 4).

#### Phase 4: The Query (Read)
1.  The Frontend (reacting to the WebSocket or user navigation) sends `GET /api/students`.
2.  **API Gateway** receives the request.
3.  It queries the **Read Model DB** (populated in Phase 3A).
4.  It returns the highly optimized JSON data to the user.

---

## 4. Subscriptions Diagram

### Azure Event Grid Topics
| Topic Name | events | Subscribers |
| :--- | :--- | :--- |
| `prospect-events` | `ProspectCreated`, `ProspectUpdated`, `ProspectMerged` | `ProjectionService`, `ApiGateway` |
| `student-events` | `StudentCreated`, `StudentUpdated` | `ProjectionService`, `ApiGateway` |
| `instructor-events` | `InstructorCreated`, `InstructorUpdated` | `ProjectionService`, `ApiGateway` |

### Webhooks
*   **ProjectionService Endpoint**: `https://.../events/webhook`
    *   Purpose: Data Persistence for Read Models.
*   **ApiGateway Endpoint**: `https://.../api/events/webhook`
    *   Purpose: Real-time User Notification.
