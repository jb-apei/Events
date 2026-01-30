# Developer Guide

Complete guide for setting up, configuring, and developing the Events project locally.

## Table of Contents
- [Environment Setup](#environment-setup)
- [Configuration](#configuration)
- [Running Services Locally](#running-services-locally)
- [Development Patterns](#development-patterns)
- [Service Implementation Guide](#service-implementation-guide)
- [Debugging](#debugging)
- [Testing](#testing)
- [Common Issues](#common-issues)

---

## Environment Setup

### Prerequisites

**Required:**
- .NET 8.0 SDK
- Node.js 20+
- PowerShell 7+
- Git

**Optional (but recommended):**
- Docker Desktop (for Azurite and SQL Server containers)
- Azure CLI (for cloud deployments)
- Terraform 1.11+ (for infrastructure management)

### Initial Setup

1. **Clone Repository**
   ```powershell
   git clone https://github.com/jb-apei/Events.git
   cd Events
   ```

2. **Install Dependencies**
   ```powershell
   # Backend
   cd src/services
   dotnet restore
   dotnet build
   
   # Frontend
   cd ../frontend
   npm install
   ```

3. **Configure Environment**
   ```powershell
   # Copy environment template
   cp .env.example .env
   
   # Edit .env with your local settings
   notepad .env
   ```

---

## Configuration

### Configuration Strategy

The project implements **centralized configuration management** using three layers:

| Environment | Configuration Source | Setup Command |
|------------|---------------------|---------------|
| **Local Development** | .NET User Secrets | `.\setup-user-secrets.ps1` |
| **Azure Deployment** | Azure Key Vault | Terraform provisions automatically |
| **Non-sensitive defaults** | appsettings.json | Checked into git |

**Key Benefits:**
- ✅ Secrets never committed to git
- ✅ Service-specific configuration isolation
- ✅ Zero .env files (uses .NET user-secrets locally)
- ✅ Automatic Key Vault integration in Azure (managed identity)
- ✅ Shared configuration library (Shared.Configuration) for consistency

### Quick Start: Local Configuration

Run this script to configure all services at once:

```powershell
.\setup-user-secrets.ps1
```

This initializes .NET user-secrets for all 6 services with development defaults:
- SQL Server: `localhost:1433` with default credentials
- Service Bus: Development endpoints
- API Gateway: `http://localhost:5037`
- JWT: Development signing key
- Event Grid: Local testing endpoints

**Verify configuration:**
```powershell
# View secrets for a specific service
dotnet user-secrets list --project src/services/ApiGateway
dotnet user-secrets list --project src/services/ProspectService

# Clear all secrets (reset)
dotnet user-secrets clear --project src/services/ApiGateway
```

**User-secrets storage location:**
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

### Configuration Architecture

**Priority Order** (highest to lowest):
1. Environment variables (Azure Container Apps)
2. Azure Key Vault (production/staging)
3. User Secrets (local development)
4. appsettings.{Environment}.json
5. appsettings.json

**Shared.Configuration Library:**

All services use `Shared.Configuration` project for:
- `ConfigurationExtensions.AddKeyVaultIfConfigured()` - Auto-detects and configures Key Vault
- `ConfigurationExtensions.ValidateRequiredConfiguration()` - Fail-fast validation
- Configuration models: `ServiceBusOptions`, `EventGridOptions`, `JwtOptions`, `ApiGatewayOptions`

**Example usage in Program.cs:**
```csharp
using Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault if configured (Azure), otherwise use user-secrets (local)
builder.Configuration.AddKeyVaultIfConfigured();

// Validate required configuration
builder.Configuration.ValidateRequiredConfiguration(
    "ConnectionStrings:ProspectDb",
    "Azure:ServiceBus:ConnectionString"
);

// Bind strongly-typed configuration
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));
```

### Environment Variables Reference

All configuration variables are documented in [`.env.example`](.env.example) (used as reference for user-secrets). Key categories:

#### Database Connections
```bash
# Transactional DB (Write Model)
ConnectionStrings__ProspectDb=Server=localhost,1433;Initial Catalog=EventsTransactional;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True

# Read Model DB
ConnectionStrings__ProjectionDatabase=Server=localhost,1433;Initial Catalog=EventsReadModel;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True
```

#### Azure Services
```bash
# Service Bus (for commands)
ServiceBus__ConnectionString=UseDevelopmentStorage=true  # Use Azurite locally

# Event Grid (for events)
EventGrid__ProspectTopicEndpoint=https://your-topic.eventgrid.azure.net/api/events
EventGrid__ProspectTopicKey=your-key-here

# API Gateway URL (for inter-service communication)
ApiGateway__Url=http://localhost:5037
ApiGateway__PushEvents=true  # Enable development mode event push
```

#### Authentication
```bash
Jwt__Secret=your-super-secret-jwt-signing-key-at-least-32-characters-long
Jwt__Issuer=events-identity-service
Jwt__Audience=events-api
Jwt__ExpirationMinutes=60
```

### Service Ports

| Service | Port | Swagger URL | Purpose |
|---------|------|-------------|---------|
| **ApiGateway** | 5037 | http://localhost:5037/swagger | REST API + WebSocket hub |
| **ProspectService** | 5110 | http://localhost:5110/swagger | Prospect write model |
| **StudentService** | 5120 | http://localhost:5120/swagger | Student write model |
| **InstructorService** | 5130 | http://localhost:5130/swagger | Instructor write model |
| **EventRelay** | N/A | N/A | Background worker (no HTTP) |
| **ProjectionService** | 5140 | http://localhost:5140/swagger | Read model projections |
| **Frontend** | 3000 | http://localhost:3000 | React UI |

---

## Running Services Locally

### Option 1: Manual Start (Recommended for Development)

**Terminal 1 - ApiGateway:**
```powershell
cd src/services/ApiGateway
dotnet run
```

**Terminal 2 - ProspectService:**
```powershell
cd src/services/ProspectService
dotnet run
```

**Terminal 3 - ProjectionService:**
```powershell
cd src/services/ProjectionService
dotnet run
```

**Terminal 4 - Frontend:**
```powershell
cd src/frontend
npm run dev
```

### Option 2: PowerShell Multi-Terminal Script

```powershell
# Start all backend services in separate windows
cd src/services

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ApiGateway; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ProspectService; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd StudentService; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd InstructorService; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ProjectionService; dotnet run"

# Start frontend
cd ..\frontend
npm run dev
```

### Option 3: Docker Containers (Future)

*Note: docker-compose.yml was removed. Future local orchestration will use a simplified approach with .env file.*

---

## Development Patterns

### Development Mode Architecture

In development mode (when `ApiGateway:PushEvents=true`), services bypass Azure Service Bus and Event Grid:

```
Frontend → ApiGateway → ProspectService (Command Handler)
                          ↓
                    Save to DB + Outbox (Transaction)
                          ↓
                    HTTP POST Event to ApiGateway Webhook
                          ↓
                    ApiGateway Broadcasts via WebSocket
                          ↓
                    Frontend Receives Event → Invalidates Cache → UI Updates
```

**Benefits:**
- No Azure dependencies required locally
- Faster development iteration
- Lower costs (no Azure resources needed)
- Same code paths as production

### Command Handler Pattern

Every write operation follows this pattern:

```csharp
public class CreateProspectCommandHandler : ICommandHandler<CreateProspectCommand, Result<int>>
{
    private readonly ProspectDbContext _dbContext;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;

    public async Task<Result<int>> HandleAsync(CreateProspectCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate and create domain entity
        var prospectResult = Prospect.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone
        );
        
        if (!prospectResult.IsSuccess) 
            return Result<int>.Failure(prospectResult.Errors);

        var prospect = prospectResult.Value!;

        // 2. Transactional Outbox: Save entity + event in single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Save entity
            await _dbContext.Prospects.AddAsync(prospect, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Create event envelope
            var prospectCreatedEvent = new ProspectCreated
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = "ProspectCreated",
                SchemaVersion = "1.0",
                OccurredAt = DateTime.UtcNow,
                Producer = "ProspectService",
                CorrelationId = command.CorrelationId,
                CausationId = command.CommandId,
                Subject = $"prospect/{prospect.Id}",
                Data = new ProspectCreatedData
                {
                    ProspectId = prospect.Id,
                    FirstName = prospect.FirstName,
                    LastName = prospect.LastName,
                    Email = prospect.Email,
                    Phone = prospect.Phone
                }
            };

            // Save to Outbox
            var outboxMessage = new OutboxMessage
            {
                EventId = prospectCreatedEvent.EventId,
                EventType = prospectCreatedEvent.EventType,
                Payload = JsonSerializer.Serialize(prospectCreatedEvent),
                CreatedAt = DateTime.UtcNow,
                Published = false
            };

            await _dbContext.Outbox.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 3. Development mode: Push event to ApiGateway (non-blocking)
            if (_configuration.GetValue<bool>("ApiGateway:PushEvents") && _httpClientFactory != null)
            {
                _ = Task.Run(async () => await PushEventToApiGateway(prospectCreatedEvent));
            }

            return Result<int>.Success(prospect.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<int>.Failure(new[] { $"Transaction failed: {ex.Message}" });
        }
    }
}
```

**Key Points:**
- Always use transactions for entity + outbox writes
- Non-blocking event push using `Task.Run()`
- Return `Result<T>` pattern for clean error handling
- Include correlation/causation IDs for tracing

### Event Subscription Pattern (React Frontend)

```typescript
// hooks/useWebSocket.ts
export const useWebSocket = () => {
  const queryClient = useQueryClient();
  
  useEffect(() => {
    const ws = new WebSocket('ws://localhost:5037/ws/events');
    
    ws.onmessage = (event) => {
      const cloudEvent = JSON.parse(event.data);
      
      switch (cloudEvent.eventType) {
        case 'ProspectCreated':
        case 'ProspectUpdated':
          // Invalidate prospects cache to trigger refetch
          queryClient.invalidateQueries(['prospects']);
          break;
        
        case 'StudentCreated':
          queryClient.invalidateQueries(['students']);
          break;
      }
    };
    
    return () => ws.close();
  }, [queryClient]);
};
```

---

## Service Implementation Guide

### Creating a New Service

Use this checklist when creating StudentService, InstructorService, or any new entity service.

#### 1. Project Structure
```
src/services/{EntityService}/
├── Commands/
│   ├── Create{Entity}Command.cs
│   └── Update{Entity}Command.cs
├── Domain/
│   └── {Entity}.cs
├── Handlers/
│   ├── Create{Entity}CommandHandler.cs
│   └── Update{Entity}CommandHandler.cs
├── Models/
│   ├── {Entity}Dto.cs
│   └── {Entity}Mapper.cs
├── Controllers/
│   └── {Entity}Controller.cs
├── Infrastructure/
│   ├── {Entity}DbContext.cs
│   └── OutboxMessage.cs
├── Program.cs
├── appsettings.json
└── Dockerfile
```

#### 2. Domain Entity Pattern

```csharp
public class Prospect
{
    // Properties use PascalCase
    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Private constructor for EF Core
    private Prospect() { }

    // Static factory method with validation
    public static Result<Prospect> Create(
        string firstName,
        string lastName,
        string email,
        string? phone)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName))
            errors.Add("First name is required");
        
        if (string.IsNullOrWhiteSpace(lastName))
            errors.Add("Last name is required");
        
        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email is required");
        else if (!email.Contains('@'))
            errors.Add("Email must be valid");

        if (errors.Any())
            return Result<Prospect>.Failure(errors);

        return Result<Prospect>.Success(new Prospect
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    // Update method with validation
    public Result<Prospect> Update(string firstName, string lastName, string email, string? phone)
    {
        var errors = new List<string>();

        // Validation logic...

        if (errors.Any())
            return Result<Prospect>.Failure(errors);

        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;

        return Result<Prospect>.Success(this);
    }
}
```

#### 3. DTO and Mapper Pattern

```csharp
// Models/ProspectDto.cs
public class ProspectDto
{
    [JsonPropertyName("prospectId")]  // camelCase for JSON
    public int ProspectId { get; set; }
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

// Models/ProspectMapper.cs
public static class ProspectMapper
{
    public static ProspectDto ToDto(this Prospect prospect)
    {
        return new ProspectDto
        {
            ProspectId = prospect.Id,  // Map Id → ProspectId
            FirstName = prospect.FirstName,
            LastName = prospect.LastName,
            Email = prospect.Email,
            Phone = prospect.Phone,
            CreatedAt = prospect.CreatedAt
        };
    }

    public static List<ProspectDto> ToDtoList(this IEnumerable<Prospect> prospects)
    {
        return prospects.Select(p => p.ToDto()).ToList();
    }
}
```

#### 4. Command Pattern

```csharp
public class CreateProspectCommand
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}
```

#### 5. API Naming Conventions

**Critical Rules:**
- Domain entity IDs: PascalCase in C# (`ProspectId`, `StudentId`)
- JSON property names: camelCase (`prospectId`, `studentId`)
- Always use `[JsonPropertyName]` attributes for consistency
- Map `entity.Id` → `dto.{Entity}Id` (e.g., `prospect.Id` → `prospectId`)

**Example API Contract:**
```json
{
  "prospectId": 123,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "555-1234",
  "createdAt": "2026-01-30T10:30:00Z"
}
```

---

## Debugging

### Visual Studio / VS Code

1. **Set Breakpoints** in command handlers or controllers
2. **Start Debugging** (F5) - `launchSettings.json` will use correct ports
3. **Attach to Process** for running services

### Useful Debug Endpoints

```powershell
# Check service health
Invoke-RestMethod http://localhost:5037/health

# View Swagger API docs
Start-Process http://localhost:5037/swagger

# Check database connection
dotnet ef database update --project src/services/ProspectService
```

### Logging

All services use structured logging with Serilog:

```csharp
_logger.LogInformation("Creating prospect {Email} with correlation {CorrelationId}", 
    command.Email, command.CorrelationId);
```

**Log Levels:**
- `Trace`: Extremely detailed (not recommended for production)
- `Debug`: Development debugging
- `Information`: General flow tracking
- `Warning`: Unusual but handled situations
- `Error`: Failures that need attention
- `Critical`: Application-breaking errors

---

## Testing

### Test Infrastructure

The project uses xUnit for automated testing with the following structure:

| Test Project | Purpose | Test Types |
|-------------|---------|-----------|
| `Shared.Events.Tests` | Event schema validation | Unit tests for EventEnvelope, event serialization |
| `ProspectService.Tests` | Command handler logic | Integration tests with InMemory database |
| `ApiGateway.Tests` | JWT service, WebSocket handlers | Unit + integration tests |

**Testing Stack:**
- **xUnit** - Test framework
- **FluentAssertions** - Readable assertions (`result.Should().Be(expected)`)
- **Moq** - Mocking dependencies
- **EF Core InMemory** - In-memory database for integration tests

### Running Tests

```powershell
# Run all tests
cd src/services
dotnet test Events.sln

# Run specific project
dotnet test Shared.Events.Tests/Shared.Events.Tests.csproj

# Run with verbose output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Current Test Coverage

**Shared.Events.Tests** (22 tests):
- EventEnvelope structure and metadata validation
- ProspectCreated event schema and serialization
- JSON serialization with camelCase property names
- DateTime UTC preservation across serialization
- Nullable field handling

✅ **All 22 tests passing**

### Writing New Tests

**Unit Test Example (Event Schema):**
```csharp
[Fact]
public void ProspectCreated_ShouldContainDataPayload()
{
    // Arrange & Act
    var prospectCreated = new ProspectCreated
    {
        Data = new ProspectCreatedData
        {
            ProspectId = 123,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        }
    };

    // Assert
    prospectCreated.Data.Should().NotBeNull();
    prospectCreated.Data.ProspectId.Should().Be(123);
    prospectCreated.Data.FirstName.Should().Be("John");
}
```

**Integration Test Example (Command Handler with InMemory DB):**
```csharp
public class CreateProspectCommandHandlerTests : IDisposable
{
    private readonly ProspectDbContext _dbContext;
    private readonly CreateProspectCommandHandler _handler;

    public CreateProspectCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ProspectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ProspectDbContext(options);
        _handler = new CreateProspectCommandHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ShouldCreateProspect_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProspectCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        // Act
        var result = await _handler.Handle(command);

        // Assert
        var saved = await _dbContext.Prospects.FindAsync(result.ProspectId);
        saved.Should().NotBeNull();
        saved.FirstName.Should().Be("John");
    }

    public void Dispose() => _dbContext.Dispose();
}
```

### Test Conventions

1. **Naming:** `MethodName_Should{Behavior}_When{Condition}`
2. **Arrange-Act-Assert:** Use clear test structure with comments
3. **One assertion per test:** Test one behavior at a time
4. **Dispose resources:** Implement `IDisposable` for integration tests
5. **Use InMemory DB:** Avoid external dependencies in tests
6. **FluentAssertions:** Use `.Should()` syntax for readability

### CI/CD Integration

Tests run automatically on every push via GitHub Actions:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - name: Run tests
        run: dotnet test Events.sln --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'
```

**Coverage Target:** 70% for handlers and domain logic.

### Manual Testing with PowerShell

```powershell
# 1. Login (if authentication enabled)
$login = @{ email = "test@test.com"; password = "test123" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5037/api/auth/login" -Method Post -Body $login -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# 2. Create Prospect
$prospect = @{ 
    firstName = "John"
    lastName = "Doe"
    email = "john.doe@test.com"
    phone = "555-1234"
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:5110/api/prospects" -Method Post -Body $prospect -ContentType "application/json" -Headers $headers

# 3. Get All Prospects
$prospects = Invoke-RestMethod -Uri "http://localhost:5037/api/prospects" -Headers $headers

# 4. Get Specific Prospect
$prospect = Invoke-RestMethod -Uri "http://localhost:5037/api/prospects/1" -Headers $headers
```

---

## Common Issues

### Issue: "Unable to connect to database"

**Solution:**
1. Ensure connection string in `.env` is correct
2. Verify SQL Server is running (if using Docker: `docker ps`)
3. Check firewall rules allow localhost:1433
4. Test connection: `dotnet ef database update`

### Issue: "Service Bus connection failed"

**Solution:**
1. For local development: Use `UseDevelopmentStorage=true` in `.env`
2. Ensure Azurite is running: `azurite --silent`
3. For Azure: Verify connection string in Key Vault

### Issue: "WebSocket connection refused"

**Solution:**
1. Ensure ApiGateway is running on correct port (5037)
2. Check `ApiGateway__Url` in `.env` matches frontend configuration
3. Verify CORS settings allow WebSocket upgrades

### Issue: "Events not appearing in UI"

**Solution:**
1. Verify `ApiGateway:PushEvents=true` in service configuration
2. Check WebSocket connection status in browser dev tools
3. Confirm event was saved to Outbox table
4. Check ApiGateway logs for event webhook receipt

### Issue: "Build failed with package restore errors"

**Solution:**
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
cd src/services
dotnet restore --force
```

---

## Additional Resources

- [Architecture Documentation](Architecture.md) - System design and patterns
- [Deployment Guide](DEPLOYMENT.md) - Azure deployment instructions
- [API Data Contracts](api-data-contracts.md) - API specifications
- [Terraform Best Practices](TERRAFORM_BEST_PRACTICES.md) - Infrastructure patterns
- [Action Plan](ACTION_PLAN.md) - Roadmap for improvements

---

**Need Help?**

- Check existing issues and documentation
- Review [copilot-instructions.md](../.github/copilot-instructions.md) for project conventions
- Consult team members or create a new issue
