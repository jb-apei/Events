# Data Schema Architecture

## Overview
This document outlines the desired schema structure for the Events Data Architecture, specifically focusing on the **Identities Bounded Context**.

Our architecture strictly follows the **CQRS (Command Query Responsibility Segregation)** pattern, resulting in two distinct data stores:
1.  **Transactional Store (Write Model)**: Normalized, optimized for integrity and consistency.
2.  **Read Store (Read Model)**: Denormalized, optimized for query performance and specific UI views.

---

## 1. Transactional Store (Write Model)

**Database**: Azure SQL (`db-events-transactional`)
**Schema**: `Identity` (for the Identities Bounded Context)

The transactional store holds the authoritative state of the entities.

### Common Columns (Audit)
All tables in the transactional store should include:
- `CreatedAt` (datetime2, UTC)
- `UpdatedAt` (datetime2, UTC, Nullable)
- `UpdatedBy` (varchar, User/Service ID, Nullable)

### Tables

#### `Identity.Prospects`
Represents potential students who have expressed interest.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INT | PK, Identity | Internal unique identifier |
| `FirstName` | NVARCHAR(100) | Not Null | |
| `LastName` | NVARCHAR(100) | Not Null | |
| `Email` | NVARCHAR(255) | UX, Not Null | Unique email address |
| `Phone` | NVARCHAR(50) | Nullable | Contact number |
| `Source` | NVARCHAR(100) | Nullable | Marketing source (e.g., "Web", "Referral") |
| `Status` | NVARCHAR(50) | Not Null | e.g., "New", "Contacted", "Converted" |
| `Notes` | NVARCHAR(MAX) | Nullable | Internal notes |
| `[Audit Columns]` | ... | | See Common Columns |

#### `Identity.Students`
Represents active learners enrolled in the system.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INT | PK, Identity | Internal unique identifier |
| `StudentNumber`| NVARCHAR(50) | UX, Not Null | Unique Student ID Identifier |
| `FirstName` | NVARCHAR(100) | Not Null | |
| `LastName` | NVARCHAR(100) | Not Null | |
| `Email` | NVARCHAR(255) | UX, Not Null | Unique email address |
| `Phone` | NVARCHAR(50) | Nullable | |
| `EnrollmentDate` | DATETIME2 | Not Null | |
| `ExpectedGraduationDate` | DATETIME2 | Nullable | |
| `Status` | NVARCHAR(50) | Not Null | e.g., "Active", "Suspended", "Alumni" |
| `Notes` | NVARCHAR(MAX) | Nullable | |
| `[Audit Columns]` | ... | | See Common Columns |

#### `Identity.Instructors`
Represents faculty members.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INT | PK, Identity | Internal unique identifier |
| `EmployeeNumber`| NVARCHAR(50) | UX, Not Null | Unique Employee ID |
| `FirstName` | NVARCHAR(100) | Not Null | |
| `LastName` | NVARCHAR(100) | Not Null | |
| `Email` | NVARCHAR(255) | UX, Not Null | Unique email address |
| `Phone` | NVARCHAR(50) | Nullable | |
| `Specialization`| NVARCHAR(100) | Nullable | e.g. "Computer Science", "Physics" |
| `HireDate` | DATETIME2 | Not Null | |
| `Status` | NVARCHAR(50) | Not Null | e.g., "Active", "OnLeave" |
| `Notes` | NVARCHAR(MAX) | Nullable | |
| `[Audit Columns]` | ... | | See Common Columns |

---

## 2. Read Store (Read Model)

**Database**: Azure SQL (`db-events-read`) or Cosmos DB (Future)
**Schema**: `ReadModel`

The read store is updated asynchronously via events. Tables are often "flattened" DTOs ready for API consumption.

#### `ReadModel.ProspectViews`
Optimized for listing prospects in the UI.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | INT | PK (Matches Transactional Id) |
| `FullName` | NVARCHAR(200) | Computed: `FirstName + ' ' + LastName` |
| `Email` | NVARCHAR(255) | |
| `Status` | NVARCHAR(50) | |
| `LastActivity` | DATETIME2 | Most recent interaction timestamp |

---

## 3. Infrastructure & Patterns

### Outbox Table
Used for the Transactional Outbox pattern to ensure atomicity between DB writes and Event publishing.

**Table**: `Shared.Outbox`

| Column | Type | Description |
|--------|------|-------------|
| `Id` | BIGINT | PK, Identity |
| `EventId` | UNIQUEIDENTIFIER | Unique Event ID |
| `EventType` | VARCHAR(255) | e.g., "ProspectCreated" |
| `Payload` | NVARCHAR(MAX) | JSON serialization of the event |
| `OccurredAt` | DATETIME2 | UTC Timestamp |
| `TraceId` | VARCHAR(100) | Distributed tracing ID |
| `Published` | BIT | 0 = Pending, 1 = Published |

