# Events - Identity Management System

Event-driven microservices architecture for managing Prospects, Students, and Instructors. (MVP Focus: Prospects)

## 🚀 Quick Start

### Prerequisites

- **.NET 10.0 SDK**
- **Node.js 20+**
- **PowerShell 7+**
- **Git**
- **Azurite** (Required for local Azure Service Bus/Storage emulation)

### Get Started in 5 Minutes

1. **Clone & Configure**
   ```powershell
   git clone https://github.com/jb-apei/Events.git
   cd Events
   cp .env.example .env  # Edit with your settings
   ```

2. **Start Infrastructure Emulator**
   ```powershell
   # Start Azurite (in a separate terminal)
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. **Start Backend Services**
   ```powershell
   cd src/services
   dotnet restore && dotnet build
   
   # Start in separate terminals
   dotnet run --project ApiGateway        # Port 5037
   dotnet run --project ProspectService   # Port 5110
   dotnet run --project ProjectionService # Port 5140
   ```

4. **Start Frontend**
   ```powershell
   cd src/frontend
   npm install
   npm run dev  # Opens http://localhost:3000
   ```

5. **Test It**
   - Open http://localhost:3000
   - Create a prospect
   - Watch real-time updates via WebSocket

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[DEVELOPER_GUIDE.md](docs/DEVELOPER_GUIDE.md)** | Complete setup, configuration, patterns, debugging |
| **[Architecture.md](docs/Architecture.md)** | System design, CQRS, event schemas |
| **[DEPLOYMENT.md](docs/DEPLOYMENT.md)** | Azure deployment, CI/CD, troubleshooting |
| **[ACTION_PLAN.md](docs/ACTION_PLAN.md)** | Roadmap for improvements |
| **[TERRAFORM_BEST_PRACTICES.md](docs/TERRAFORM_BEST_PRACTICES.md)** | Infrastructure patterns |

---

## 🏗️ Architecture Overview

### Tech Stack

**Frontend:** React + TypeScript + React Query + WebSocket  
**Backend:** .NET 10 microservices  
**Cloud:** Azure Container Apps + SQL + Service Bus + Event Grid  
**Patterns:** CQRS, Event-Driven, Transactional Outbox

### Services

| Service | Port | Purpose |
|---------|------|---------|
| **ApiGateway** | 5037 | REST API + WebSocket hub |
| **ProspectService** | 5110 | Prospect write model |
| **StudentService** | 5120 | Student write model |
| **InstructorService** | 5130 | Instructor write model |
| **EventRelay** | N/A | Outbox → Event Grid publisher |
| **ProjectionService** | 5140 | Read model projections |
| **Frontend** | 3000 | React UI |

### Event Flow

```
Frontend → ApiGateway → ProspectService
                          ↓
                    Save Entity + Event (Transaction)
                          ↓
                    Publish Event → EventRelay → Event Grid
                          ↓
                    ApiGateway → WebSocket → Frontend (Real-time Update)
```

---

## 🚢 Deployment

### Automated Deployment (Recommended)

```powershell
# Deploy from GitHub (production)
.\deploy.ps1 -GitHubRepo "https://github.com/jb-apei/Events.git"

# Or deploy from local source
.\deploy.ps1
```

### GitHub Actions (CI/CD)

Automatically deploys on push to `master`:
1. Builds all 7 container images in Azure Container Registry
2. Applies Terraform infrastructure changes
3. Restarts Container Apps with new images

**Setup:** See [DEPLOYMENT.md](docs/DEPLOYMENT.md) for GitHub secrets configuration.

---

## 📁 Project Structure

```
/
├── .github/workflows/        # CI/CD pipelines
├── docs/                     # Documentation
│   ├── DEVELOPER_GUIDE.md    # ⭐ Start here for development
│   ├── Architecture.md       # System design
│   └── DEPLOYMENT.md         # Azure deployment
├── src/
│   ├── frontend/             # React + TypeScript
│   ├── services/             # .NET microservices
│   │   ├── ApiGateway/       # API + WebSocket hub
│   │   ├── ProspectService/  # Write model
│   │   ├── EventRelay/       # Event publisher
│   │   ├── ProjectionService/# Read model
│   │   ├── Shared.Events/    # Event contracts
│   │   └── Shared.Infrastructure/
│   └── shared/
└── infrastructure/           # Terraform IaC
    ├── main.tf
    └── modules/
```

---

## 🧪 Development Workflow

### Creating a New Prospect

```powershell
# 1. Login (if auth enabled)
$login = @{ email = "test@test.com"; password = "test123" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5037/api/auth/login" -Method Post -Body $login -ContentType "application/json"

# 2. Create prospect
$prospect = @{ 
    firstName = "John"
    lastName = "Doe"
    email = "john.doe@test.com"
    phone = "555-1234"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5110/api/prospects" `
    -Method Post `
    -Body $prospect `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $($auth.token)" }
```

### Real-Time Event Updates

The UI automatically receives updates via WebSocket when:
- Prospects are created/updated
- Students are enrolled
- Instructors are added

No page refresh needed!

---

## 🛠️ Key Features

- ✅ **Event-Driven Architecture** - CQRS with Event Sourcing patterns
- ✅ **Real-Time Updates** - WebSocket subscriptions for instant UI updates
- ✅ **Transactional Outbox** - Guaranteed event delivery
- ✅ **Idempotent Handlers** - Safe at-least-once processing
- ✅ **Azure Native** - Container Apps, SQL, Service Bus, Event Grid
- ✅ **CI/CD Pipeline** - Automated GitHub Actions deployment
- ✅ **Infrastructure as Code** - Terraform for reproducible deployments
- ✅ **Development Mode** - Run locally without Azure dependencies

---

## 🔧 Configuration

All configuration documented in [`.env.example`](.env.example):

```bash
# Database
ConnectionStrings__ProspectDb=Server=localhost,1433;...

# Azure Services (optional for local dev)
ServiceBus__ConnectionString=UseDevelopmentStorage=true
EventGrid__ProspectTopicEndpoint=https://...

# Development Mode
ApiGateway__Url=http://localhost:5037
ApiGateway__PushEvents=true  # Enable local event push
```

See [Developer Guide](docs/development/guide.md) for complete configuration details.

---

## 🤝 Contributing

1. Check [Action Plan](docs/status/action-plan.md) for planned improvements
2. Follow patterns in [Developer Guide](docs/development/guide.md)
3. Review [.github/copilot-instructions.md](.github/copilot-instructions.md) for conventions
4. Create feature branch and submit PR

---

## 📖 Learn More

- **Documentation Home:** [docs/README.md](docs/README.md)
- **Getting Started:** [Developer Guide](docs/development/guide.md)
- **System Design:** [Architecture](docs/architecture/overview.md)
- **Deployment:** [Deployment Guide](docs/ops/deployment.md)
- **API Contracts:** [Data Contracts](docs/architecture/data-contracts.md)
- **Infrastructure:** [Terraform Practices](docs/ops/terraform.md)

---

## 📝 License

[Your License Here]

## 🆘 Support

- Review documentation in [`docs/`](docs/README.md)
- Check [Action Plan](docs/status/action-plan.md) for known limitations
- Create an issue for bugs or feature requests
