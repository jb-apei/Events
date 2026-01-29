# Events - Identity Management System

Event-driven microservices architecture for managing Prospects, Students, and Instructors.

## Project Structure

```
/
├── docs/
│   └── Architecture.md              # Detailed architecture documentation
├── src/
│   ├── frontend/                    # React + TypeScript + Vite
│   ├── services/                    # .NET Core microservices
│   │   ├── ProspectService/         # Write model for Prospect aggregate
│   │   ├── ApiGateway/              # REST API + WebSocket hub
│   │   ├── EventRelay/              # Outbox → Event Grid publisher
│   │   ├── ProjectionService/       # Read model projections
│   │   ├── Shared.Events/           # Event contracts
│   │   └── Shared.Infrastructure/   # Common Azure SDK utilities
│   └── shared/                      # Cross-platform shared code
└── infrastructure/                  # Bicep/Terraform for Azure resources
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Node.js 20+ (for frontend)
- Azure CLI (for deployment)
- Docker (optional, for local Azure emulation)

### Backend (.NET Services)

```bash
cd src/services
dotnet restore
dotnet build
dotnet run --project ProspectService
```

### Frontend (React)

```bash
cd src/frontend
npm install
npm run dev
```

Visit http://localhost:3000

## Architecture Overview

- **Commands**: Flow through Azure Service Bus (`identity-commands` queue)
- **Events**: Published to Azure Event Grid topics (`prospect-events`, etc.)
- **Pattern**: Transactional Outbox ensures reliable event publishing
- **Real-time**: WebSocket hub pushes events to React clients
- **State Management**: React Query with cache invalidation on WebSocket events

See [docs/Architecture.md](docs/Architecture.md) for comprehensive details.

## Development Workflow

1. **Create a command** in ProspectService/Commands
2. **Add handler** that saves to DB + Outbox
3. **Define event schema** in Shared.Events
4. **EventRelay publishes** event to Event Grid
5. **ApiGateway forwards** event via WebSocket
6. **React UI receives** event and updates via React Query

## Testing

```bash
# Backend unit tests
cd src/services
dotnet test

# E2E tests
dotnet test --filter Category=E2E
```

## Contributing

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for coding conventions and patterns.

## License

MIT
