# CI/CD & DevOps Standards

This document establishes the referenced standards for Build, Deployment, and Infrastructure pipelines. These standards should be applied to this and future projects to ensure performance, reliability, and maintainability.

## 1. Pipeline Architecture

**Standard:** Use **GitHub Actions** as the control plane for all operational tasks (Build, Test, Provision, Deploy).

- **Triggers**:
  - `push` to `master/main` for production deployments.
  - `workflow_dispatch` for manual on-demand deployments.
  - `pull_request` for validation builds (tests only, no deploy).
- **concurrency**: Ensure strictly ordered deployments (prevent race conditions on Terraform state).

## 2. Build Strategy

**Standard:** Use **Matrix Strategies** for microservice builds.

### Why?
Sequential builds (building Service A, then Service B...) waste time. Matrix builds spin up multiple runners to build all services simultaneously.

### Implementation Pattern
```yaml
jobs:
  build:
    strategy:
      matrix:
        include:
          - name: service-a
            context: src/services
            dockerfile: ServiceA/Dockerfile
          - name: service-b
            context: src/services
            dockerfile: ServiceB/Dockerfile
    steps:
      - run: az acr build --image ${{ matrix.name }} ...
```

## 3. Build Context Optimization

**Standard:** Minimize the **Build Context** sent to the container registry.

### The Problem
Sending the repository root (`.`) as the build context uploads *everything* (docs, infrastructure, git history) to the build agent. This causes slow builds and timeouts.

### The Standard
- **Backend Services**: Context should be scoped to `src/services` (or relevant subfolder).
- **Frontend**: Context should be scoped to `src/frontend`.
- **Never** use root (`.`) context unless the Dockerfile explicitly requires files from both infrastructure and source (rare).

## 4. Infrastructure as Code (IaC)

**Standard:** Terraform must be executed **automatically** within the pipeline.

- **State Management**: Remote state in Azure Storage Account.
- **Authentication**: Service Principal (future: OIDC) with `Contributor` access.
- **Variables**: Injected via `env` vars mapped from GitHub Secrets (`ARM_*`).
- **Workflow**:
  1. `terraform fmt -check`
  2. `terraform plan -out=tfplan`
  3. `terraform apply tfplan`

## 5. Deployment & Runtime

**Standard:** Zero-downtime rolling updates via **Revision Management**.

- **Container Apps**: Do not restart in place. create a new **Revision**.
- **Health Probes**: All apps must expose `/health` and have `liveness`/`readiness` probes configured in Terraform.
- **Configuration**: All app config (DB strings, API URLs) must be injected as Environment Variables from Terraform (referencing Key Vault if sensitive).

## 6. Local Script Parity

**Standard:** Provide local PowerShell scripts that mimic the CI pipeline.

- `deploy.ps1`: Orchestrates the full flow (Build -> Terraform -> Deploy).
- `build-and-push-acr.ps1`: Allows developers to push images without waiting for GitHub.
- **Requirement**: Local scripts must use the *same* build contexts and Dockerfile paths as the GitHub Action to prevent "it works on my machine" issues.

## 7. Versioning & Tagging

**Standard:** Immutable Tags.

- `latest`: Mutable tag for current head.
- `${{ github.sha }}`: Immutable tag for specific commit.
- **Rollback**: Deployments should reference the SHA tag to ensure exact code version matches.
