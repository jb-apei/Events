# Events - Identity Management System

Event-driven microservices architecture for managing Prospects, Students, and Instructors.

## Project Structure

```
/
├── .github/
│   ├── workflows/                   # CI/CD pipelines
│   │   └── deploy-azure.yml         # Automated Azure deployment
│   ├── copilot-instructions.md      # Development guidelines
│   └── SECRETS_SETUP.md             # GitHub secrets configuration
├── docs/
│   ├── Architecture.md              # Detailed architecture documentation
│   ├── DEPLOYMENT.md                # Deployment guide
│   ├── api-data-contracts.md        # API contracts and schemas
│   ├── development-mode-patterns.md # Dev environment setup
│   ├── service-implementation-checklist.md
│   ├── services-startup-guide.md    # Service startup reference
│   ├── swagger-api-documentation.md # API documentation
│   └── TESTING_REFERENCE.md         # Testing documentation
├── src/
│   ├── frontend/                    # React + TypeScript + Vite
│   │   ├── components/              # React components (ProspectPage, etc.)
│   │   ├── hooks/                   # Custom hooks (useWebSocket, useProspects)
│   │   └── api/                     # API client code
│   ├── services/                    # .NET Core microservices
│   │   ├── ApiGateway/              # REST API + WebSocket hub + Outbox management
│   │   ├── ProspectService/         # Write model for Prospect aggregate
│   │   ├── StudentService/          # Write model for Student aggregate
│   │   ├── InstructorService/       # Write model for Instructor aggregate
│   │   ├── EventRelay/              # Outbox → Event Grid publisher
│   │   ├── ProjectionService/       # Read model projections
│   │   ├── Shared/                  # Shared domain models
│   │   ├── Shared.Events/           # Event contracts (ProspectCreated, etc.)
│   │   └── Shared.Infrastructure/   # Common Azure SDK utilities
│   └── shared/                      # Cross-platform shared code
└── infrastructure/                  # Terraform for Azure resources
    ├── main.tf                      # Main infrastructure definition
    ├── modules/                     # Custom Terraform modules
    │   ├── container-apps/          # Container Apps configuration
    │   └── event-grid-topics/       # Event Grid topics
    └── README.md                    # Infrastructure documentation
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Node.js 20+ (for frontend)
- Azure CLI (for deployment)
- Docker (optional, for local Azure emulation with Azurite)
- PowerShell 7+ (for deployment scripts)
- Terraform 1.11+ (for infrastructure)

### Local Development

#### Backend (.NET Services)

Start Azurite for local Azure emulation:
```bash
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

Then start services:
```bash
cd src/services
dotnet restore
dotnet build

# Start individual services
dotnet run --project ApiGateway
dotnet run --project ProspectService
dotnet run --project EventRelay
dotnet run --project ProjectionService
```

#### Frontend (React)

```bash
cd src/frontend
npm install
npm run dev
```

Visit http://localhost:5173

### Azure Deployment

#### Quick Deploy (Automated)

```powershell
# Deploy from local source
.\deploy.ps1

# Deploy from GitHub repository
.\deploy.ps1 -GitHubRepo "https://github.com/jb-apei/Events.git"
```

#### GitHub Actions (CI/CD)

Automated deployment on push to `master`:
1. Builds all container images in Azure Container Registry
2. Applies Terraform infrastructure changes
3. Restarts all container apps with new images

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment options.

## Architecture Overview

- **Commands**: Flow through Azure Service Bus (`identity-commands` queue)
- **Events**: Published to Azure Event Grid topics (`prospect-events`, `student-events`, `instructor-events`)
- **Pattern**: Transactional Outbox ensures reliable event publishing
- **Real-time**: WebSocket hub (`/ws/events`) pushes events to React clients
- **State Management**: React Query with cache invalidation on WebSocket events
- **Authentication**: Development mode allows unauthenticated WebSocket connections
- **Infrastructure**: Azure Container Apps, SQL Database (Basic tier), Service Bus, Event Grid
- **Observability**: Application Insights with OpenTelemetry

See [docs/Architecture.md](docs/Architecture.md) for comprehensive details.

## Services

### ApiGateway
- REST API endpoints (`/api/prospects`, `/api/students`, `/api/instructors`)
- WebSocket hub for real-time updates (`/ws/events`)
- Outbox management API (`/api/outbox`)
- Event Grid webhook receiver (`/api/events/webhook`)
- Development mode: Unauthenticated WebSocket access enabled

### ProspectService
- Processes Prospect commands (Create, Update)
- Implements write model with transactional outbox pattern
- Publishes ProspectCreated, ProspectUpdated events

### StudentService
- Processes Student commands (Create, Update, Changed)
- Implements write model with transactional outbox pattern
- Publishes StudentCreated, StudentUpdated, StudentChanged events

### InstructorService
- Processes Instructor commands (Create, Update, Deactivate)
- Implements write model with transactional outbox pattern
- Publishes InstructorCreated, InstructorUpdated, InstructorDeactivated events

### EventRelay
- Background service polling Outbox table
- Publishes events to Azure Event Grid
- Marks events as published after successful delivery

### ProjectionService
- Subscribes to Event Grid topics via webhook
- Updates read models (ProspectSummary, StudentSummary, InstructorSummary)
- Implements Inbox pattern for idempotency (7-day dedupe window)

### Frontend
- React with TypeScript and Vite
- Real-time updates via WebSocket connection
- React Query for state management and caching
- Displays Prospects, Students, and Instructors

## Development Workflow

1. **Create a command** in ProspectService/Commands (e.g., CreateProspectCommand.cs)
2. **Add handler** in Handlers/ that validates, saves to DB + Outbox in single transaction
3. **Define event schema** in Shared.Events (e.g., ProspectCreated.cs)
4. **EventRelay publishes** event to Event Grid (automatic background process)
5. **ApiGateway receives** event via webhook and forwards via WebSocket
6. **React UI receives** event through WebSocket hook and updates via React Query
7. **ProjectionService** updates read models (e.g., ProspectSummary table)

## Key Features

### WebSocket Real-Time Updates
- WebSocket endpoint: `/ws/events`
- Connection pooling: Up to 50 connections per user
- Automatic cleanup: Dead connections removed every 10 seconds
- Idle timeout: 10 minutes
- Development mode: No authentication required

### Transactional Outbox Pattern
All domain events are saved to an Outbox table in the same transaction as the domain entity, ensuring:
- **Atomicity**: Events never lost even if Event Grid is down
- **Reliability**: EventRelay publishes from Outbox with retries
- **Consistency**: Events always match database state

### Idempotency
All event handlers check Inbox table by `eventId` before processing:
- **Dedupe window**: 7 days
- **At-least-once delivery**: Event Grid may deliver multiple times
- **Safe replays**: Re-running events has no side effects

### Azure Container Apps Deployment
- Auto-scaling based on HTTP requests and CPU
- Managed identities for Key Vault access
- ACR integration for pulling images
- Rolling updates with zero downtime
- Environment variables from Key Vault secrets

## Configuration

### GitHub Secrets (for CI/CD)
Required secrets for GitHub Actions workflow:
- `ARM_CLIENT_ID` - Service principal client ID
- `ARM_CLIENT_SECRET` - Service principal secret
- `ARM_SUBSCRIPTION_ID` - Azure subscription ID
- `ARM_TENANT_ID` - Azure AD tenant ID

Run `setup-github-secrets.ps1` to automate setup.

### Local Development
Edit `appsettings.Development.json` in each service for local configuration:
- Connection strings point to local Azurite or Azure SQL
- Service Bus connection string
- Event Grid endpoint (optional for local testing)

## Testing

```bash
# Backend unit tests
cd src/services
dotnet test

# E2E tests (requires Azure resources)
dotnet test --filter Category=E2E
```

## Common Tasks

### View Outbox Messages
```powershell
# Query unpublished events
curl https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/api/outbox?published=false
```

### Restart All Services
```powershell
.\deploy.ps1 -SkipBuild -SkipTerraform
```

### Rebuild Images from GitHub
```powershell
.\deploy.ps1 -GitHubRepo "https://github.com/jb-apei/Events.git"
```

### View Container App Logs
```bash
az containerapp logs show \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --follow
```

## Troubleshooting

### WebSocket Connection Issues
- Check that WebSocket endpoint is `wss://` (not `ws://`) in production
- Verify development mode is enabled for local testing without auth
- Check browser console for connection errors
- Verify API Gateway is running and accessible

### Events Not Publishing
- Check Outbox table for unpublished events: `SELECT * FROM Outbox WHERE Published = 0`
- Verify EventRelay service is running
- Check Event Grid topic configuration in Azure Portal
- Review Application Insights logs for errors

### Container App Not Starting
- Check logs: `az containerapp logs show --name <app-name> --resource-group rg-events-dev`
- Verify Key Vault access policy includes container app managed identity
- Check environment variables are correctly set
- Verify ACR pull permissions

## Documentation

- [Architecture.md](docs/Architecture.md) - Complete architecture documentation
- [DEPLOYMENT.md](docs/DEPLOYMENT.md) - Deployment guide
- [api-data-contracts.md](docs/api-data-contracts.md) - API schemas
- [development-mode-patterns.md](docs/development-mode-patterns.md) - Local dev setup
- [services-startup-guide.md](docs/services-startup-guide.md) - Service startup reference
- [TESTING_REFERENCE.md](docs/TESTING_REFERENCE.md) - Testing and API reference

## Contributing

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for coding conventions and patterns.

## License

MIT
