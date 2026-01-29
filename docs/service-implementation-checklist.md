# Service Implementation Checklist

Use this checklist when creating StudentService, InstructorService, or any new entity service.

---

## Pre-Implementation

- [ ] Review [api-data-contracts.md](api-data-contracts.md) for naming conventions
- [ ] Review [development-mode-patterns.md](development-mode-patterns.md) for architecture patterns
- [ ] Decide on service port (5110=Prospect, 5120=Student, 5130=Instructor)
- [ ] Define entity properties and validation rules
- [ ] List event types (Created, Updated, Deleted, etc.)

---

## Backend Service Setup

### 1. Project Structure
- [ ] Create `src/services/{EntityService}` project
- [ ] Add folders: `Commands`, `Domain`, `Handlers`, `Models`, `Controllers`, `Infrastructure`
- [ ] Reference `Shared.Events` project
- [ ] Add NuGet packages: EF Core, Azure SDK (optional), System.Text.Json

### 2. Domain Entity (`Domain/{Entity}.cs`)
- [ ] Properties use PascalCase
- [ ] Primary key named `Id` (int)
- [ ] Include `CreatedAt` and `UpdatedAt` (DateTime)
- [ ] Private parameterless constructor for EF Core
- [ ] Static `Create()` factory method with validation
- [ ] Return `Result<{Entity}>` from factory
- [ ] Validation errors in list format

### 3. DTOs (`Models/{Entity}Dto.cs`)
- [ ] Create DTO class with `[JsonPropertyName]` attributes
- [ ] Map `Id` → `{entity}Id` with correct casing
- [ ] Use camelCase for all JSON properties
- [ ] Include all properties needed by frontend
- [ ] Add XML comments for documentation

### 4. Mapper (`Models/{Entity}Mapper.cs`)
- [ ] Extension method `ToDto(this {Entity} entity)`
- [ ] Extension method `ToDtoList(this IEnumerable<{Entity}> entities)`
- [ ] Map all properties from entity to DTO
- [ ] **Critical:** Map `entity.Id` → `dto.{Entity}Id`

### 5. Commands (`Commands/Create{Entity}Command.cs`, etc.)
- [ ] Add `[JsonPropertyName]` for all properties (camelCase)
- [ ] Include `CommandId` and `CorrelationId` properties
- [ ] Set default `CommandId = Guid.NewGuid().ToString()`
- [ ] Required properties use `string.Empty` default
- [ ] Optional properties use `string?`

### 6. Command Handlers (`Handlers/Create{Entity}CommandHandler.cs`)
- [ ] Inject dependencies: `DbContext`, `IConfiguration`, `ILogger`, `IHttpClientFactory?`
- [ ] Implement `ICommandHandler<TCommand, Result<int>>`
- [ ] Validate command → domain entity factory method
- [ ] **Transactional Outbox:**
  - Begin transaction
  - Save entity to database
  - Create event (EventEnvelope)
  - Save event to Outbox table
  - Commit transaction
- [ ] **Development Mode Event Push:**
  - Check `ApiGateway:PushEvents` config
  - Use `Task.Run()` for non-blocking HTTP POST
  - Post event to `{ApiGatewayUrl}/api/events/webhook`
  - Log success/failure
- [ ] Return `Result<int>.Success(entity.Id)` or `Failure(errors)`

### 7. DbContext (`Infrastructure/{Entity}DbContext.cs`)
- [ ] `DbSet<{Entity}> {Entities}`
- [ ] `DbSet<OutboxMessage> Outbox`
- [ ] Configure entity using `modelBuilder.Entity<{Entity}>()`
- [ ] Index on `Email` (unique)
- [ ] Index on `Status`
- [ ] Configure Outbox entity

### 8. Controller (`Controllers/{Entities}Controller.cs`)
- [ ] `[ApiController]`, `[Route("api/[controller]")]`
- [ ] Inject handlers, DbContext, logger
- [ ] **POST Create{Entity}:**
  - Accept `[FromBody] Create{Entity}Command`
  - Set CorrelationId from header or generate
  - Call handler
  - Return `BadRequest({ errors })` on failure
  - Return `CreatedAtAction` with `{ {entity}Id, correlationId }`
- [ ] **PUT Update{Entity}:**
  - Accept `int id` and `[FromBody] Update{Entity}Command`
  - Set `command.{Entity}Id = id`
  - Return `Ok({ {entity}Id, correlationId })`
- [ ] **GET All:**
  - Query `_dbContext.{Entities}.ToListAsync()`
  - Convert to DTOs: `entities.ToDtoList()`
  - Return `Ok(dtos)` (array, no wrapper)
- [ ] **GET by ID:**
  - Query `_dbContext.{Entities}.FindAsync(id)`
  - Return `NotFound({ message, {entity}Id })` if null
  - Convert to DTO: `entity.ToDto()`
  - Return `Ok(dto)`

### 9. Program.cs Configuration
- [ ] `builder.Services.AddHttpClient()`
- [ ] Conditional Service Bus: `if (!string.IsNullOrEmpty(connectionString))`
- [ ] Conditional in-memory DB: `if (string.IsNullOrEmpty(connectionString))`
- [ ] Register handlers as scoped services
- [ ] Configure CORS
- [ ] Add controllers

### 10. appsettings.json
- [ ] `ConnectionStrings:{ Entity}Db: ""`
- [ ] `ApiGateway:Url: "http://localhost:5037"`
- [ ] `ApiGateway:PushEvents: true`
- [ ] `Azure:ServiceBus:ConnectionString: ""`
- [ ] Set port in `launchSettings.json` (5110/5120/5130)

---

## Shared Events

### 11. Event Schema (`Shared.Events/{Entities}/{Entity}{Action}.cs`)
- [ ] Inherit from `EventEnvelope<{Entity}{Action}Data>`
- [ ] Override `EventType` property (e.g., "ProspectCreated")
- [ ] Set `Producer = "{Entity}Service"` in constructor
- [ ] Create data class with `[JsonPropertyName]` attributes
- [ ] **Critical:** Use `{entity}Id` for ID property
- [ ] Include all properties needed by event subscribers
- [ ] Match structure with frontend `EventEnvelope` interface

---

## API Gateway Updates

### 12. ApiGateway Controller (`Controllers/{Entities}Controller.cs`)
- [ ] Create new controller: `{Entities}Controller`
- [ ] Inject `IHttpClientFactory`, `IConfiguration`, `CommandPublisher`
- [ ] **GET Proxy Pattern:**
  - Read `{Entity}Service:Url` from config (default to `http://localhost:{port}`)
  - Create HttpClient
  - Forward GET to service: `httpClient.GetAsync($"{serviceUrl}/api/{entities}")`
  - Return JSON content directly
  - Handle errors gracefully (empty array on failure)
- [ ] **POST Command Publishing:**
  - Accept command
  - Publish to Service Bus (or direct API in dev mode)
  - Return appropriate response

### 13. ApiGateway Configuration
- [ ] Add to `appsettings.json`:
  ```json
  "{Entity}Service": {
    "Url": "http://localhost:{port}"
  }
  ```

### 14. WebSocket Auto-Subscribe
- [ ] Update `WebSocketHandler.cs`:
  ```csharp
  var defaultSubscriptions = new[] { 
      "ProspectCreated", "ProspectUpdated", 
      "StudentCreated", "StudentUpdated",  // Add new event types
      "InstructorCreated", "InstructorUpdated" 
  };
  ```

---

## Frontend Implementation

### 15. TypeScript Interface (`frontend/src/api/{entities}.ts`)
- [ ] Define `{Entity}` interface:
  - `{entity}Id: number` (NOT string, NOT id)
  - All properties in camelCase
  - Optional properties use `?`
  - Dates are `string` (ISO 8601)
- [ ] Define `Create{Entity}Request` interface (no ID)
- [ ] Define `Update{Entity}Request` interface (ID required, fields optional)
- [ ] Create API client class extending axios
- [ ] Add 401 interceptor (dispatch `auth:logout`)
- [ ] Methods: `get{Entities}()`, `get{Entity}(id)`, `create{Entity}()`, `update{Entity}()`

### 16. React Query Hooks (`frontend/src/hooks/use{Entities}.ts`)
- [ ] `use{Entities}()` hook for fetching list
  - Key: `['{entities}']`
  - Query function calls API
- [ ] `use{Entity}(id)` hook for fetching single entity
  - Key: `['{entity}', id]`
- [ ] `useCreate{Entity}()` mutation
  - On success: invalidate `['{entities}']`
- [ ] `useUpdate{Entity}()` mutation
  - On success: invalidate `['{entities}']` and `['{entity}', id]`
- [ ] `useInvalidate{Entities}()` hook
  - Returns function to invalidate cache
  - Used by WebSocket handler

### 17. Components

**{Entity}Page.tsx:**
- [ ] Import `useWebSocket` and `useInvalidate{Entities}`
- [ ] Setup WebSocket connection
- [ ] In `onMessage`: check for `{Entity}Created` and `{Entity}Updated`
- [ ] Call `invalidate{Entities}()` when relevant event received
- [ ] Display WebSocket status indicator
- [ ] Render `{Entity}List` and `{Entity}Form` components

**{Entity}List.tsx:**
- [ ] Use `use{Entities}()` hook
- [ ] Defensive check: `if (!{entities} || !Array.isArray({entities}))`
- [ ] Map entities: `{entities}.map({entity} => ...)`
- [ ] Use `{entity}.{entity}Id` as key
- [ ] Display loading/error states
- [ ] Handle empty state

**{Entity}Form.tsx:**
- [ ] Use `useCreate{Entity}()` and `useUpdate{Entity}()` mutations
- [ ] Event type picker (Create vs Update)
- [ ] Form fields for all required properties
- [ ] Validation before submit
- [ ] Display success/error messages
- [ ] Clear form on success

### 18. Routing (if needed)
- [ ] Add route in `App.tsx` or router
- [ ] Update navigation menu

---

## Testing Checklist

### 19. Backend Tests
- [ ] **API Contract Tests:**
  - POST create returns `{ {entity}Id: number, correlationId: string }`
  - GET all returns array with `{entity}Id` property
  - GET by ID returns object with `{entity}Id` property
  - Properties are camelCase in JSON
- [ ] **Event Push Test:**
  - Create entity
  - Check logs: "Event pushed to ApiGateway"
  - Verify event contains `{entity}Id`
- [ ] **Validation Test:**
  - Send invalid data
  - Verify error response: `{ errors: string[] }`
- [ ] **Database Test:**
  - Create entity
  - Verify saved in database
  - Verify Outbox entry created

### 20. Frontend Tests
- [ ] **Interface Compatibility:**
  - Fetch entities
  - Verify TypeScript has no type errors
  - Confirm `{entity}Id` property exists
- [ ] **WebSocket Integration:**
  - Create entity via UI
  - Verify event received in browser console
  - Verify UI updates without manual refresh
- [ ] **CRUD Operations:**
  - Create entity via form
  - Update entity
  - View entity details
  - List shows all entities

### 21. Integration Tests
- [ ] **End-to-End Flow:**
  - Login
  - Create entity via UI
  - Verify saved (check API response)
  - Verify event pushed (check ApiGateway logs)
  - Verify WebSocket broadcast (check browser console)
  - Verify UI updates (new entity appears)
  - Refresh page
  - Verify entity still appears (persisted)

---

## Common Mistakes to Avoid

- [ ] ❌ Returning `Ok(entity)` instead of `Ok(entity.ToDto())`
- [ ] ❌ Using `Id` in JSON instead of `{entity}Id`
- [ ] ❌ Forgetting `[JsonPropertyName]` attributes
- [ ] ❌ Wrapping arrays: `Ok(new { data = list })` instead of `Ok(list)`
- [ ] ❌ Using `DateTime.Now` instead of `DateTime.UtcNow`
- [ ] ❌ Not checking `ApiGateway:PushEvents` config before pushing
- [ ] ❌ Blocking on event push (use `Task.Run()`)
- [ ] ❌ Missing DTO mapper call in controller
- [ ] ❌ Frontend interface using `string` for ID instead of `number`
- [ ] ❌ Not updating WebSocket auto-subscribe array

---

## Deployment Readiness

- [ ] Connection strings configured
- [ ] Azure Service Bus connection string added (production)
- [ ] Event Grid topics configured (production)
- [ ] CORS policy updated for production domains
- [ ] Logging configured (Application Insights)
- [ ] Health check endpoints added
- [ ] OpenAPI/Swagger documentation generated
- [ ] Environment-specific appsettings files

---

## Documentation

- [ ] Update Architecture.md with new service
- [ ] Add service to service ports table
- [ ] Document new event types
- [ ] Update Copilot instructions if needed
- [ ] Add examples to API documentation

---

## Final Verification

Before merging:
- [ ] All checklist items completed
- [ ] No TypeScript compilation errors
- [ ] No C# build warnings
- [ ] API contract tests pass
- [ ] Frontend integration test passes
- [ ] WebSocket events working
- [ ] UI updates in real-time
- [ ] Data persists across service restarts (when using real DB)

---

## Time Estimate

**For experienced developer following this checklist:**
- Backend service: 2-3 hours
- Frontend components: 1-2 hours
- Testing & integration: 1 hour
- **Total: 4-6 hours per service**

**Using AI assistant (Copilot) with this checklist:**
- Provide checklist + entity definition
- AI generates all code following patterns
- Manual review and testing: 1-2 hours
- **Total: 1-2 hours per service**
