# Terraform Gaps & Remediation Plan

**Purpose**: This document identifies gaps preventing a fully automated "Destroy & Redeploy" of the Azure infrastructure using Terraform. The goal is to achieve true Infrastructure as Code (IaC) maturity where the entire environment can be rebuilt from scratch without manual intervention.

## Executive Summary

Current State: **Partially Automated**.
- **Terraform Core**: Successfully creates base infrastructure (ACR, SQL Server, Service Bus).
- **Terraform Apps**: Deploys Container Apps and Event Grid Subscriptions.
- **Result**: A strict `terraform apply` on a fresh subscription will **FAIL**.

The primary blockers are "Chicken-and-Egg" dependencies between infrastructure and application code (images, database schema).

## Gaps & Issues Matrix

| Priority | ID | Issue | Impact | Recommended Action | Order |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Critical** | **GAP-001** | **Missing Database Migrations (DML)** | Apps crash on startup (CrashLoopBackOff) because SQL tables do not exist. Terraform creates the DB, but not the schema. | Implement `init-containers` or a GitHub Action step to run EF Core migrations/SQL scripts against the new DB. | 1 |
| **Critical** | **GAP-002** | **ACR Image Dependency** | `apps/main.tf` fails because it tries to pull images that don't exist yet in the newly created ACR. | Split CD pipeline: 1. `infra/core`, 2. Build/Push Images, 3. `infra/apps`. | 2 |
| **High** | **GAP-003** | **Event Grid Subscription Handshake** | Terraform fails to create Event Grid subscriptions because the API Gateway isn't healthy (due to GAP-001) to answer the validation challenge. | Fix GAP-001 first. Ensure API Gateway `/api/events/webhook` is anonymous or properly secured for handshake. | 3 |
| **Medium** | **GAP-004** | **Secrets Leakage in TF State** | Connection Strings are injected as plain text env vars. They are visible in `terraform.tfstate`. | Refactor `apps/main.tf` to use Key Vault References associated with the User Assigned Identity. | 4 |

## Detailed Remediation Steps

### 1. Database Schema Automation (GAP-001)

**Issue**: `setup-sql-tables.ps1` is currently required to create `Students`, `Instructors` tables.
**Fix**:
*   **Intermediate**: Add a GitHub Action step between "Infrastructure Core" and "Deploy Apps" that runs `dotnet ef database update` or `Invoke-Sqlcmd`.
*   **Ideal (Cloud Native)**: Create a "Migrator" Container App (Job) that runs on deployment to apply schemas.
*   **Action**: Create a `DbMigrator` project in `.NET` and add it to the build pipeline.

### 2. Infrastructure Layering (GAP-002)

**Issue**: You cannot deploy the `apps` layer until the docker images exist in the ACR, but you can't build the images until the ACR exists (defined in `core`).
**Fix**: Formalize the 3-stage pipeline.
1.  **Stage 1 (Infra-Core)**: `terraform apply` (Core). Output: ACR Name, SQL Connection info.
2.  **Stage 2 (Build)**: `az acr build` / `docker push`. Target: The ACR from Stage 1.
3.  **Stage 3 (Infra-Apps)**: `terraform apply` (Apps). Deploys Container Apps using images from Stage 2.

### 3. Event Grid & Handshake (GAP-003)

**Issue**: `azurerm_eventgrid_event_subscription` expects the endpoint to return `200 OK` with a validation code.
**Fix**:
*   Ensure `ApiGateway` starts successfully (depends on GAP-001).
*   Add `depends_on = [module.container_apps]` to the subscription resource (already present, but relies on app being healthy).
*   Consider using a Dead Letter Queue or "status check" mechanisms in Terraform.

### 4. Secret Management (GAP-004)

**Issue**: Secrets are passed via `env_vars = { ... }` in `infrastructure/apps/main.tf`.
**Fix**:
*   Grant `Key Vault Secrets User` to the Container App Requestor Identity.
*   Change Terraform to set env var: `secret_ref = "sql-connection"`.
*   Update `infrastructure/modules/container-apps` to support `secret_ref`.

## Implementation Priority List

1.  **Refactor Pipeline**: Split `deploy-services.yml` or creating a `provision-environment.yml` that respects the Layering (Core -> Build -> Apps).
2.  **Automate SQL Setup**: Convert `setup-sql-tables.ps1` into a deployable artifact (Migration Tool).
3.  **Verify**: Run a full destruction of `dev` resource group and trigger the new pipeline.
