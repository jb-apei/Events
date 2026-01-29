# Shared.Events - Event Schema Library

This library contains all domain event schemas used across the Events microservices architecture.

## Event Standards

All events follow the **CloudEvents v1.0** specification with additional tracking fields.

### Event Envelope Structure

```json
{
  "eventId": "guid",
  "eventType": "ProspectCreated",
  "schemaVersion": "1.0",
  "occurredAt": "2026-01-29T10:30:00Z",
  "producer": "ProspectService",
  "correlationId": "trace-abc123",
  "causationId": "command-xyz789",
  "subject": "prospect/12345",
  "tenantId": "optional-tenant-id",
  "traceparent": "00-trace-id-span-id-01",
  "data": {
    // Event-specific payload
  }
}
```

## Event Catalog

### Prospects (MVP Focus)

| Event Type | Description | Schema |
|------------|-------------|--------|
| `ProspectCreated` | New prospect record created | [ProspectCreated.cs](Prospects/ProspectCreated.cs) |
| `ProspectUpdated` | Prospect information updated | [ProspectUpdated.cs](Prospects/ProspectUpdated.cs) |
| `ProspectMerged` | Two prospects merged into one | [ProspectMerged.cs](Prospects/ProspectMerged.cs) |

### Students (Future)

| Event Type | Description | Schema |
|------------|-------------|--------|
| `StudentCreated` | New student enrolled | [StudentCreated.cs](Students/StudentCreated.cs) |
| `StudentUpdated` | Student information updated | [StudentUpdated.cs](Students/StudentUpdated.cs) |
| `StudentChanged` | Student status changed | [StudentChanged.cs](Students/StudentChanged.cs) |

### Instructors (Future)

| Event Type | Description | Schema |
|------------|-------------|--------|
| `InstructorCreated` | New instructor added | [InstructorCreated.cs](Instructors/InstructorCreated.cs) |
| `InstructorUpdated` | Instructor information updated | [InstructorUpdated.cs](Instructors/InstructorUpdated.cs) |
| `InstructorDeactivated` | Instructor deactivated | [InstructorDeactivated.cs](Instructors/InstructorDeactivated.cs) |

## Usage

### Creating an Event

```csharp
using Shared.Events.Prospects;

var prospectCreated = new ProspectCreated
{
    EventId = Guid.NewGuid().ToString(),
    CorrelationId = "request-correlation-id",
    CausationId = "command-id",
    Subject = $"prospect/{prospect.Id}",
    Data = new ProspectCreatedData
    {
        ProspectId = prospect.Id,
        FirstName = prospect.FirstName,
        LastName = prospect.LastName,
        Email = prospect.Email,
        Status = "New",
        CreatedAt = DateTime.UtcNow
    }
};
```

### Serializing Events

```csharp
using Shared.Events;

var json = EventSerializer.Serialize(prospectCreated);
```

### Deserializing Events

```csharp
var prospectCreated = EventSerializer.Deserialize<ProspectCreated>(json);
```

## Versioning

- **Schema Version**: Tracked in `schemaVersion` field (default: "1.0")
- **Breaking Changes**: Increment major version, create migration logic
- **Additive Changes**: Add optional fields without version bump
- **Backward Compatibility**: Required for 6 months minimum

## Best Practices

1. **Always set CorrelationId** - For distributed tracing
2. **Always set CausationId** - Links event to originating command
3. **Use UTC timestamps** - All DateTime fields in UTC
4. **Immutable Events** - Never modify published events, create new version
5. **Small Payloads** - Keep event data < 64KB, use blob reference for large data
6. **Subject Pattern** - Format: `{aggregate-type}/{aggregate-id}`

## Testing

Run schema validation tests:

```bash
dotnet test
```

## Notes

- Events are JSON serialized using System.Text.Json
- Property names use camelCase per JSON conventions
- Nullable reference types enabled for compile-time null safety
- Target framework: .NET 9.0
