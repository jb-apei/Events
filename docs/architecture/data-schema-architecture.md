# Data Schema & Architecture

This document tracks the database schemas and table structures for all services in the Events project, strictly following the **CQRS (Command Query Responsibility Segregation)** pattern.

## Overview

The system uses two distinct types of databases to separate write and read concerns:

1.  **Transactional Databases (Write Model)**: Normalized, optimized for writes and domain integrity. Owned by specific microservices.
2.  **Read Model Databases (Read Model)**: Denormalized, optimized for fast queries by the API Gateway/UI. Populated asynchronously via events.

---

## 1. Transactional Databases (Write Model)

These databases are the "Source of Truth" for domain aggregates.

### Service: Prospect Service
**Database**: \db-events-transactional-dev\
**Schema**: \dbo\

#### Table: \Prospects\
Stores the current state of Prospect entities.

| Column Name | Data Type | Nullable | Description |
| :--- | :--- | :--- | :--- |
| \Id\ | \int\ (PK, Identity) | No | Unique identifier for the prospect. |
| \FirstName\ | \
varchar(max)\ | No | First name. |
| \LastName\ | \
varchar(max)\ | No | Last name. |
| \Email\ | \
varchar(450)\ | No | Email address (Indexed, Unique). |
| \Phone\ | \
varchar(max)\ | Yes | Phone number. |
| \Source\ | \
varchar(max)\ | No | Source of the prospect. |
| \Status\ | \
varchar(max)\ | No | Current status (e.g., 'New'). |
| \Notes\ | \
varchar(max)\ | Yes | Notes. |
| \CreatedAt\ | \datetime2\ | No | |
| \UpdatedAt\ | \datetime2\ | Yes | |

#### Table: \Outbox\
Implements the Transactional Outbox pattern to ensure atomic events.

| Column Name | Data Type | Nullable | Description |
| :--- | :--- | :--- | :--- |
| \Id\ | \guid\ (PK) | No | Unique message ID. |
| \EventType\ | \
varchar(max)\ | No | The type of event (e.g., \ProspectCreated\). |
| \Payload\ | \
varchar(max)\ | No | JSON serialized event data. |
| \CreatedAt\ | \datetime2\ | No | When the event occurred. |
| \Published\ | \it\ | No | 0 = Pending, 1 = Published. |
| \ProcessedAt\ | \datetime2\ | Yes | When it was picked up by the relay. |

---

## 2. Read Model Database (Read Model)

**Service**: Projection Service / API Gateway
**Database**: \db-events-read-dev\
**Purpose**: Serves \GET\ requests from the API Gateway. Populated asynchronously via Event Grid events.

#### Table: \ProspectSummary\
A flattened/optimized view of prospects for the UI list.

| Column Name | Data Type | Nullable | Description |
| :--- | :--- | :--- | :--- |
| \ProspectId\ | \int\ (PK) | No | Matches the \Id\ from the Transactional DB. |
| \FirstName\ | \
varchar(max)\ | Yes | |
| \LastName\ | \
varchar(max)\ | Yes | |
| \Email\ | \
varchar(max)\ | Yes | |
| \Phone\ | \
varchar(max)\ | Yes | |
| \Status\ | \
varchar(max)\ | Yes | |
| \CreatedAt\ | \datetime2\ | No | |
| \UpdatedAt\ | \datetime2\ | Yes | |

---

## 3. Infrastructure & Patterns

### Event Grid Topics
- \evgt-events-prospect-events-dev\: Prospects domain events.
- \evgt-events-student-events-dev\: Student domain events.
- \evgt-events-instructor-events-dev\: Instructor domain events.

### Subscriptions
- **Projection Service**: Subscribes to all topics to update the \ReadModelDb\.
- **API Gateway**: Subscribes to all topics to push real-time WebSocket updates.

### Service Bus
- \sb-events-dev\: Handles commands (\CreateProspect\, \UpdateProspect\) sent from API Gateway to Domain Services.
