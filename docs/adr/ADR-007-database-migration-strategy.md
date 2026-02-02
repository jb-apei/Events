# ADR-007: Database Migration Strategy (MVP)

**Status:** Accepted  
**Date:** 2026-02-02  
**Deciders:** Development Team

## Context
As we develop the Events microservices (Student, Instructor, Prospect), the database schema evolves rapidly. We need a strategy to apply Entity Framework Core migrations to the Azure SQL databases in our Development and Production environments associated with the correct version of the application code.

## Decision
We will execute database migrations **automatically on application startup** (Migrate on Startup).

Each microservice is responsible for migrating its own bounded context database.
- `ProspectService` -> `db-events-transactional-dev` (Prospect Tables) & `db-events-readmodel-dev` (if applicable)
- `StudentService` -> `db-events-transactional-dev` (Student Tables)
- `InstructorService` -> `db-events-transactional-dev` (Instructor Tables)

Code implementation pattern in `Program.cs`:
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    context.Database.Migrate();
}
```

## Rationale

### Pros
- **Velocity**: Developers don't need to manage separate SQL scripts or deployment pipelines for schema changes.
- **Sync**: The application code and database schema are always synchronized because the app updates the DB before starting to serve requests.
- **Simplicity**: Removes the need for a complex "Idempotent SQL Script" generation step in the CI pipeline or a "jumper" container.

### Cons
- **Security**: The application runtime identity requires DDL (Data Definition Language) permissions (`db_owner` or `ddl_admin`), violating the principle of least privilege.
- **Scalability**: If multiple replicas start simultaneously, they might race to apply migrations. (Mitigated currently by EF Core's distributed lock, but can be risky).
- **Startup Time**: Application startup is delayed by migration execution.

## Alternatives Considered

### 1. SQL Scripts in CI/CD Pipeline
- **Rejected for MVP**: Requires generating `dotnet ef migrations script --idempotent`, managing the artifact, and running it via `sqlcmd` in the GitHub Action. Adds significant complexity to the `deploy-infrastructure` pipeline.

### 2. Manual Execution
- **Rejected**: Error-prone and slows down deployment.

## Consequences
- We must ensure that breaking schema changes are handled carefully (e.g., column renames) to avoid downtime during the rolling deployment.
- Future phases will likely move to a "Migration Job" (init container or pre-deploy job) to separate concerns and reduce permissions for the runtime application level.
