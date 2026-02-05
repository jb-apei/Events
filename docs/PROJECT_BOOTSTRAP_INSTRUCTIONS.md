# Project Bootstrap Instructions

## Role & Objective
You are an expert Solutions Architect and DevOps Engineer. Your task is to scaffold a new Event-Driven Microservices project, following the "Golden Path" established by the reference `Events` project, and incorporating the following best practices and conventions.

## Technology Stack (Strict)
- **Backend**: .NET 9.0 (ASP.NET Core Web API), EF Core 9.0
- **Frontend**: React 18+ (Vite), TypeScript, React Query (TanStack Query), Tailwind CSS
- **Messaging**: Azure Service Bus (Commands), Azure Event Grid (Events)
- **Infrastructure**: Terraform (Azure Verified Modules), Split State Architecture
- **CI/CD**: GitHub Actions (Split Infra/Code workflows)
- **Local Dev**: Docker Desktop (Azurite for storage/service bus emulation, SQL Server Edge)

## Folder Structure (Strict)
/
├── .github/workflows/          # CI/CD
├── docs/
│   ├── adr/                    # Architecture Decision Records (ADRs)
│   ├── project-log.md          # Manual deployment activities log
│   └── ...                     # All other documentation
├── infrastructure/
│   ├── core/                   # Slow-changing infra (VNet, SQL Server, Service Bus, Key Vault)
│   ├── apps/                   # Fast-changing infra (Container Apps, Databases)
│   └── modules/                # Shared Terraform modules
├── scripts/                    # All automation scripts (except setup)
│   └── ...                     # Individual scripts (db-migrations.ps1, deploy.ps1, etc.)
├── setup/                      # (Optional) If setup is a directory, it should only contain a main setup script and act as a menu/launcher for scripts in /scripts
│   └── setup.ps1               # Main entry point, provides menu to run scripts from /scripts
├── src/
│   ├── frontend/               # Vite React App
│   ├── services/               # .NET Solution
│   │   ├── [Service].API/      # Microservices (e.g., Identity, Orders)
│   │   ├── Shared.Events/      # Shared Contracts/Schemas (Nuget or Project Ref)
│   │   └── Shared.Infrastructure/ # Cross-cutting concerns (Outbox, Auth)
└── MANUALTRACKER.md            # (Legacy) Use docs/project-log.md instead

## Documentation & Logging (Mandatory)
- **ADRs**: All major architectural decisions must be documented as an ADR in `/docs/adr/` (use markdown, follow template).
- **Manual Deployment Log**: Any manual deployment or configuration step must be logged in `/docs/project-log.md` (date, context, action, resolution).
- **Folder Documentation**: Each major code folder (e.g., `/src/frontend`, `/src/services`, `/infrastructure/core`) must have a short `README.md` describing its purpose and usage, placed in the respective folder.
- **All documentation must reside under `/docs`** (except for per-folder `README.md`).

## Scripts & Setup (Strict)
- **All scripts** (automation, migration, deployment, etc.) must be placed in `/scripts`.
- **Setup Script**: There must be a single `setup.ps1` (or `setup.sh`) at the root or in `/setup` that acts as a menu/launcher for all scripts in `/scripts`. It should:
  - Present a menu to run any script in `/scripts`
  - Check prerequisites (Docker, .NET, Node, etc.)
  - Guide the user through environment setup
- **No business logic or automation scripts should be in the root**—only the setup launcher.

## Infrastructure & DevEx
- **Split State**: Maintain two separate Terraform states: `core.tfstate` and `apps.tfstate`. `apps` reads `core` via `terraform_remote_state`.
- **Identity First**: The user running Terraform (and the CI Service Principal) MUST be assigned "Key Vault Secrets Officer" and "AcrPush" explicitly in the `core` layer before any secrets are written.
- **Variables**: Create a script to generate `terraform.tfvars` from a template, asking the user for their OID and Subscription ID.

## Backend & Frontend
- **Backend**: Use CQRS, Transactional Outbox, and Shared Kernel patterns. Use project references for shared code.
- **Frontend**: Use React Query, Vite proxy config, and WebSocket for real-time updates.

## Execution Plan
1. **Scaffold**: Create the folder structure and empty solution files.
2. **Infra Core**: Generate `infrastructure/core` Terraform code.
3. **DevEx**: Write the `setup.ps1` launcher and all scripts in `/scripts`.
4. **Backend**: Generate the Shared Kernel and one "Hello World" microservice with Outbox pattern.
5. **Frontend**: Initialize Vite app with Proxy config.
6. **Docs**: Create `/docs/adr/`, `/docs/project-log.md`, and per-folder `README.md`.

## Automation
- The agent must automatically generate an ADR markdown file for every major architectural or tooling decision.
- The agent must append to `/docs/project-log.md` for every manual deployment or configuration step it cannot automate.
