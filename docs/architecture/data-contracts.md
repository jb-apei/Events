# API Data Contracts & Naming Conventions

## Overview
This document standardizes data structures, property naming, and API contracts across all services to prevent mismatches between frontend and backend.

---

## Property Naming Standards

### General Rules

1. **Backend (C#)**: PascalCase for class properties
2. **API JSON**: camelCase using `[JsonPropertyName]` attributes
3. **Frontend (TypeScript)**: camelCase for interface properties
4. **Database**: PascalCase column names (EF Core default)

### ID Field Convention

**CRITICAL: All entity IDs follow this pattern**

| Entity        | Backend Property | JSON Property | Frontend Property | Type   |
|---------------|------------------|---------------|-------------------|--------|
| Prospect      | `Id`             | `prospectId`  | `prospectId`      | `int`  |
| Student       | `Id`             | `studentId`   | `studentId`       | `int`  |
| Instructor    | `Id`             | `instructorId`| `instructorId`    | `int`  |

**Why different names:**
- Backend uses generic `Id` for domain entities (DDD pattern)
- API uses specific `{entity}Id` for clarity (self-documenting)
- Frontend receives specific name for type safety

**Implementation:**
- Create `{Entity}Dto` classes for API responses
- Use `[JsonPropertyName("{entity}Id")]` on DTO
- Map domain entity → DTO in controllers

---

## Standard DTO Pattern

### File Structure
```
src/services/{ServiceName}/
├── Models/
│   ├── {Entity}Dto.cs        # API response model
│   ├── {Entity}Mapper.cs     # Entity → DTO conversion
│   └── Create{Entity}Request.cs  # API request models (if different from commands)
```

### DTO Template

```csharp
using System.Text.Json.Serialization;

namespace {ServiceName}.Models;

/// <summary>
/// Data Transfer Object for {Entity} entity.
/// Used for API responses to ensure consistent property naming with frontend.
/// </summary>
public class {Entity}Dto
{
    /// <summary>
    /// Unique identifier for the {entity} (mapped from Id).
    /// </summary>
    [JsonPropertyName("{entity}Id")]
    public int {Entity}Id { get; set; }

    /// <summary>
    /// {Entity}'s first name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// {Entity}'s last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// {Entity}'s email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the {entity} was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the {entity} was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    // Entity-specific properties...
}
```

### Mapper Template

```csharp
using {ServiceName}.Domain;

namespace {ServiceName}.Models;

public static class {Entity}Mapper
{
    public static {Entity}Dto ToDto(this {Entity} entity)
    {
        return new {Entity}Dto
        {
            {Entity}Id = entity.Id,  // Map Id → {entity}Id
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            // ... other properties
        };
    }

    public static List<{Entity}Dto> ToDtoList(this IEnumerable<{Entity}> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}
```

---

## Frontend Interface Standards

### TypeScript Interface Template

```typescript
export interface {Entity} {
  {entity}Id: number  // Must match backend JSON property
  firstName: string
  lastName: string
  email: string
  phone?: string      // Optional properties use ?
  status: string
  createdAt: string   // ISO 8601 date string from backend
  updatedAt: string
}

export interface Create{Entity}Request {
  firstName: string
  lastName: string
  email: string
  phone?: string
  // DO NOT include {entity}Id - backend generates this
}

export interface Update{Entity}Request {
  {entity}Id: number  // Required for updates
  firstName?: string  // All fields optional except ID
  lastName?: string
  email?: string
  phone?: string
}
```

**Key Points:**
- Frontend uses `number` for IDs (JavaScript number type)
- Backend JSON serializes `int` as number
- Dates are ISO 8601 strings (`DateTime.UtcNow.ToString("O")`)
- Optional properties use `?` in TypeScript, `string?` in C#

---

## Event Data Contracts

### Event Envelope Structure

**All events MUST follow this structure:**

```csharp
using System.Text.Json.Serialization;

namespace Shared.Events.{Entities};

public class {Entity}{Action} : EventEnvelope<{Entity}{Action}Data>
{
    public override string EventType => "{Entity}{Action}";

    public {Entity}{Action}()
    {
        Producer = "{EntityService}";
    }
}

public class {Entity}{Action}Data
{
    [JsonPropertyName("{entity}Id")]
    public int {Entity}Id { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    // ... entity-specific properties
}
```

**Frontend EventEnvelope:**

```typescript
export interface EventEnvelope {
  eventId: string
  eventType: string
  schemaVersion: string
  occurredAt: string
  producer: string
  correlationId: string
  causationId: string
  subject: string
  data: any  // Or specific type like ProspectCreatedData
}
```

---

## Command Contracts

### Backend Command

```csharp
using System.Text.Json.Serialization;

namespace {ServiceName}.Commands;

public class {Action}{Entity}Command
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    // ... other properties
}
```

**Key Points:**
- Commands have `commandId` and `correlationId`
- Use `[JsonPropertyName]` for camelCase JSON
- Frontend sends commands as JSON in POST body

---

## API Controller Pattern

### GET Endpoints (Read)

```csharp
/// <summary>
/// Get all {entities}.
/// </summary>
[HttpGet]
public async Task<IActionResult> GetAll{Entities}()
{
    var entities = await _dbContext.{Entities}
        .OrderByDescending(e => e.CreatedAt)
        .ToListAsync();

    // ALWAYS convert to DTOs before returning
    var dtos = entities.ToDtoList();

    return Ok(dtos);  // Returns JSON array with {entity}Id
}

/// <summary>
/// Get {entity} by ID.
/// </summary>
[HttpGet("{id}")]
public async Task<IActionResult> Get{Entity}(int id)
{
    var entity = await _dbContext.{Entities}.FindAsync(id);

    if (entity == null)
    {
        return NotFound(new { message = "{Entity} not found", {entity}Id = id });
    }

    // ALWAYS convert to DTO before returning
    var dto = entity.ToDto();

    return Ok(dto);  // Returns JSON object with {entity}Id
}
```

### POST Endpoints (Create)

```csharp
/// <summary>
/// Create a new {entity}.
/// </summary>
[HttpPost]
public async Task<IActionResult> Create{Entity}([FromBody] Create{Entity}Command command)
{
    // Set correlation ID from header or generate new one
    if (string.IsNullOrEmpty(command.CorrelationId))
    {
        command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
    }

    var result = await _createHandler.HandleAsync(command);

    if (!result.IsSuccess)
    {
        return BadRequest(new { errors = result.Errors });
    }

    // Return consistent response with {entity}Id
    return CreatedAtAction(
        nameof(Get{Entity}),
        new { id = result.Value },
        new { {entity}Id = result.Value, correlationId = command.CorrelationId });
}
```

### PUT Endpoints (Update)

```csharp
/// <summary>
/// Update an existing {entity}.
/// </summary>
[HttpPut("{id}")]
public async Task<IActionResult> Update{Entity}(int id, [FromBody] Update{Entity}Command command)
{
    // Ensure ID in URL matches command
    command.{Entity}Id = id;

    // Set correlation ID
    if (string.IsNullOrEmpty(command.CorrelationId))
    {
        command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
    }

    var result = await _updateHandler.HandleAsync(command);

    if (!result.IsSuccess)
    {
        return BadRequest(new { errors = result.Errors });
    }

    return Ok(new { {entity}Id = result.Value, correlationId = command.CorrelationId });
}
```

---

## Error Response Format

### Standard Error Response

```json
{
  "errors": [
    "First name is required.",
    "Email format is invalid."
  ]
}
```

```json
{
  "message": "Prospect not found",
  "prospectId": 123
}
```

**Frontend expects:**
- Validation errors: `{ errors: string[] }`
- Not found: `{ message: string, {entity}Id: number }`
- Server errors: `{ message: string }`

---

## Date/Time Handling

### Backend (C#)

```csharp
// ALWAYS use UTC
var occurredAt = DateTime.UtcNow;

// Serialize as ISO 8601
[JsonPropertyName("occurredAt")]
public DateTime OccurredAt { get; set; }  // System.Text.Json handles this
```

### Frontend (TypeScript)

```typescript
interface Event {
  occurredAt: string  // ISO 8601 string: "2026-01-29T10:30:00Z"
}

// Parse to Date if needed
const date = new Date(event.occurredAt)

// Display
const formatted = new Date(event.occurredAt).toLocaleString()
```

---

## Status/Enum Handling

### Backend Enum

```csharp
public static class ProspectStatus
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Converted = "Converted";
    public const string Disqualified = "Disqualified";
}
```

**Use string constants, not enums:**
- JSON serialization is simpler
- No conversion needed
- Frontend can use literal types

### Frontend Type

```typescript
export type ProspectStatus = 
  | 'New' 
  | 'Contacted' 
  | 'Qualified' 
  | 'Converted' 
  | 'Disqualified'

export interface Prospect {
  status: ProspectStatus  // Type-safe
}
```

---

## Response Wrapper (DO NOT USE)

**❌ WRONG:**
```csharp
return Ok(new { prospects = prospectList });  // Frontend gets { prospects: [...] }
```

**✅ CORRECT:**
```csharp
return Ok(prospectList);  // Frontend gets [...]
```

**Why:**
- Frontend expects arrays directly for collections
- No wrapper object needed
- Consistent with REST conventions

---

## Checklist for New Services

When creating StudentService or InstructorService:

### Backend:
- [ ] Create `Models/{Entity}Dto.cs` with `[JsonPropertyName("{entity}Id")]`
- [ ] Create `Models/{Entity}Mapper.cs` with `ToDto()` extension
- [ ] Update controller GET methods to use `.ToDto()` or `.ToDtoList()`
- [ ] Ensure POST returns `{ {entity}Id: number, correlationId: string }`
- [ ] Use `[JsonPropertyName]` on all command properties
- [ ] Event data classes use `[JsonPropertyName("{entity}Id")]`

### Frontend:
- [ ] Interface uses `{entity}Id: number` (not `id` or `Id`)
- [ ] Create/Update requests match backend command structure
- [ ] EventEnvelope interface matches backend event structure
- [ ] API client expects direct arrays (not wrapped in objects)

### Testing:
- [ ] Verify GET /api/{entities} returns array with `{entity}Id`
- [ ] Verify GET /api/{entities}/{id} returns object with `{entity}Id`
- [ ] Verify POST returns `{ {entity}Id, correlationId }`
- [ ] Verify events contain `{entity}Id` in data payload
- [ ] Frontend can parse responses without errors

---

## Common Pitfalls to Avoid

### ❌ ID Mismatch
```csharp
// Backend returns:
{ "id": 123 }

// Frontend expects:
{ "prospectId": 123 }
```
**Solution:** Use DTOs with `[JsonPropertyName("{entity}Id")]`

### ❌ Array Wrapper
```csharp
return Ok(new { data = prospects });  // Wrong
return Ok(prospects);                 // Correct
```

### ❌ Missing JsonPropertyName
```csharp
public class ProspectDto
{
    public int ProspectId { get; set; }  // Serializes as "ProspectId" (wrong)
    
    [JsonPropertyName("prospectId")]     // Serializes as "prospectId" (correct)
    public int ProspectId { get; set; }
}
```

### ❌ Date Time Without UTC
```csharp
var time = DateTime.Now;  // Wrong - local time
var time = DateTime.UtcNow;  // Correct
```

### ❌ Returning Domain Entity
```csharp
return Ok(prospect);  // Wrong - exposes internal structure
return Ok(prospect.ToDto());  // Correct - uses contract
```

---

## Example: Complete Flow

### Backend Controller
```csharp
[HttpGet]
public async Task<IActionResult> GetAllProspects()
{
    var prospects = await _dbContext.Prospects.ToListAsync();
    return Ok(prospects.ToDtoList());  // Returns [{ prospectId, firstName, ... }]
}
```

### Frontend API Call
```typescript
async getProspects(): Promise<Prospect[]> {
  const response = await this.client.get<Prospect[]>('/prospects')
  return response.data  // TypeScript knows it's Prospect[] with prospectId
}
```

### Frontend Display
```tsx
const { data: prospects } = useProspects()

prospects?.map(prospect => (
  <div key={prospect.prospectId}>  {/* No confusion - always prospectId */}
    {prospect.firstName} {prospect.lastName}
  </div>
))
```

---

## Service-Specific Contracts

### Prospect Service
- Entity ID: `prospectId` (int)
- Status: `New | Contacted | Qualified | Converted | Disqualified`
- Required: `firstName`, `lastName`, `email`
- Optional: `phone`, `source`, `notes`

### Student Service (Future)
- Entity ID: `studentId` (int)
- Status: `Active | Inactive | Graduated | Withdrawn`
- Required: `firstName`, `lastName`, `email`, `studentNumber`
- Optional: `phone`, `enrollmentDate`, `expectedGraduationDate`

### Instructor Service (Future)
- Entity ID: `instructorId` (int)
- Status: `Active | Inactive | OnLeave`
- Required: `firstName`, `lastName`, `email`, `employeeNumber`
- Optional: `phone`, `specialization`, `hireDate`

---

## Validation

### Backend Validation Rules
- Use domain entity validation (in `Create()` method)
- Return `Result<T>.Failure(errors)` for validation failures
- Controller returns `BadRequest(new { errors = result.Errors })`

### Frontend Validation
- Form validation before API call
- Display backend validation errors if present
- Use React Hook Form or similar for consistent UX

---

## Summary

**Golden Rules:**
1. **DTOs for all API responses** - never return domain entities directly
2. **{entity}Id naming** - specific ID names in JSON, generic `Id` in domain
3. **JsonPropertyName everywhere** - explicit camelCase for all public contracts
4. **No array wrappers** - return arrays directly, not `{ data: [] }`
5. **UTC timestamps** - always use `DateTime.UtcNow`
6. **Mappers for conversion** - centralized entity → DTO transformation
7. **Test the contract** - verify JSON structure matches TypeScript interfaces

Following these conventions eliminates 95% of frontend/backend integration issues!
