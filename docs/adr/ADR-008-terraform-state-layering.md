# ADR-008: Terraform State Layering Strategy

**Status:** Accepted  
**Date:** 2026-02-02  
**Deciders:** DevOps Team

## Context
Our initial infrastructure setup used a **Monolithic State** approach, where a single `main.tf` and state file managed everything from Networking and Databases to Container Apps and secrets.

This caused significant operational issues:
1.  **Global Locking**: Updating an environment variable on a Container App locked the entire state, blocking updates to unrelated resources (like SQL or Service Bus).
2.  **Performance**: `terraform plan` took increasingly long as it had to refresh the state of every resource in the subscription.
3.  **High Coupling**: A mistake in the application layer configuration could potentially destroy core infrastructure like the database server.
4.  **CI/CD Bottlenecks**: Frequent "state blob is already locked" errors in GitHub Actions when multiple commits were pushed in succession.

## Decision
We will split the Terraform state into **Lifecycle-Based Layers**.

Instead of one monolithic state, we will maintain discrete state files for resources with different rates of change and criticalities.

### Layer 1: Core Infrastructure
- **Scope**: Networking, Key Vault, SQL Server (Server-level), Service Bus Namespace, Event Grid Domains, Log Analytics, Container Registry.
- **Change Frequency**: Low (Weekly/Monthly)
- **Criticality**: High (Destructive changes here cause outages)

### Layer 2: Application Resources
- **Scope**: Container Apps, SQL Databases, Service Bus Queues/Topics, Event Grid Subscriptions.
- **Change Frequency**: High (Daily/Hourly)
- **Criticality**: Medium (Can be redeployed easily)
- **Dependency**: Reads Layer 1 outputs via `terraform_remote_state`.

## Rationale

### Pros
- **Reduced Blast Radius**: Changes to the application layer cannot accidentally delete the SQL Server or VNET.
- **Granular Locking**: CI/CD for application code (Layer 2) does not block operations on core infrastructure.
- **Performance**: Plans are faster as they only check a subset of resources.
- **Safety**: "Core" infrastructure is naturally protected from "App" dev-loop churn.

### Cons
- **Complexity**: Managing passing outputs between layers (e.g., Layer 2 needs the Resource Group name from Layer 1) requires `output` variables and `terraform_remote_state` data sources.
- **Drift**: It's possible for Layer 2 to reference a resource in Layer 1 that has been manually deleted, though Terraform validation helps here.

## Implementation Details

We will use the existing `infrastructure/` folder for Layer 1 (Core) pending a full refactor, but logically verify that Application Deployments (Layer 2) are increasingly decoupled.

*Note: As of Feb 2026, we are in a transition phase. We have implemented file-based modularization (modules/container-apps) but are still sharing a state file in some pipelines. The strict separation of state files (backend keys) is the next immediate step.*
