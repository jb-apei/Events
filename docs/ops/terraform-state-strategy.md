# Terraform State Management Strategy

## Problem: State Locking
You are currently encountering frequent "terraform state lock" errors. This happens because the Terraform state file is locked globally whenever a `plan` or `apply` operation runs.

In the current architecture, we have a **Monolithic State**. A single `main.tf` file contains everything:
- Resource Groups
- Networking / Service Bus / SQL
- Application Configuration (Container Apps)

This means:
1.  **Global Lock**: Updating a single environment variable on a Container App locks the SQL Server and Networking configuration.
2.  **Long Plan Times**: Terraform must refresh the state of *every* resource in the Azure subscription, increasing the duration the lock is held.
3.  **CI/CD vs Local Conflict**: If GitHub Actions is deploying infrastructure while you are testing locally, one will fail.

## Best Practice Solution: Layered State (Implemented)

The industry standard to solve this is **State Splitting** (or Layering). We have broken the infrastructure into lifecycle-based layers.

### Implemented Layers

#### Layer 0: Bootstrap
- Resource Group for Terraform State
- Storage Account for State
- *Frequency: One-time setup*

#### Layer 1: Core Infrastructure (`infrastructure/core`)
- Networking (VNETs)
- Key Vault
- Service Bus Namespaces (Queues/Topics)
- SQL Server (Logical Server)
- Container Registry
- Log Analytics / App Insights
- Event Grid
- *Frequency: Rare (Monthly/Weekly)*

#### Layer 2: Application Resources (`infrastructure/apps`)
- Container Apps Environment
- Container Apps (The resources themselves)
- *Frequency: Frequent (Daily/Hourly)*
- *Dependencies*: Reads outputs from Layer 1 via `terraform_remote_state`.

### Benefits
- **Reduced Blast Radius**: If you break Layer 2, Layer 1 remains intact.

### Benefits
- **Reduced Blast Radius**: If you break Layer 3, Layer 1 remains intact.
- **Granular Locking**: Working on the "App" layer doesn't lock the "Networking" layer. The CI pipeline for apps won't block your local work on the database.
- **Faster Execution**: `terraform plan` only checks ~20 resources instead of 100+.

## Implementation Plan (Future)

To migrate to this, we would:
1.  Create separate folders: `infrastructure/core`, `infrastructure/apps`.
2.  Use `terraform_remote_state` data source in `apps` to read outputs (like Resource Group names) from `core`.
3.  Migrate resources using `terraform state mv`.

## Immediate Mitigation: CI/CD Concurrency

We can immediately improve the situation by ensuring GitHub Actions don't overlap with themselves, and by failing fast.

We will add `concurrency` groups to the workflows.
