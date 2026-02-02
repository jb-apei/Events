# ADR-005: GitHub Actions as CI/CD Control Plane

**Status:** Accepted  
**Date:** 2026-01-30  
**Deciders:** DevOps Team, Architecture Team

## Context

We need a reliable, automated, and maintainable way to build, test, provision infrastructure, and deploy the Events microservices system. The solution must support parallel builds (for speed), maintain strict security isolation, and integration deeply with our source control.

## Decision

We will use **GitHub Actions** as the single source of truth (Control Plane) for all CI/CD operations.

Specifically, we adopt the following patterns:
1.  **Matrix Build Strategy**: We explicitly reject sequential builds. All services must be built in parallel using runner matrices.
2.  **Optimized Build Contexts**: Builds must only upload the specific service directory (`src/services` or `src/frontend`) to the build agent/registry, never the repository root.
3.  **Immutable Artifacts**: Containers are tagged with the git commit SHA (`${{ github.sha }}`) to ensure traceability.
4.  **Infrastructure Integration**: Terraform is executed within the pipeline (Plan -> Apply) using OIDC/Service Principal authentication.

## Rationale

### Pros
- **Proximity to Code**: Pipeline definitions live alongside application code (`.github/workflows`), reducing context switching.
- **Performance**: Matrix strategies allow us to scale build agents horizontally (e.g., building 7 services takes the same time as building 1).
- **Security**: GitHub Secrets and OIDC provide robust secret management without exposing credentials in scripts.
- **Auditability**: Every deployment is linked to a specific Commit and Actor.

### Cons
- **Vendor Lock-in**: deeply tied to GitHub's ecosystem.
- **Cost**: Parallel runners increase minute usage (though total wall-clock time decreases).

## Alternatives Considered

### 1. Azure DevOps (Pipelines)
- **Rejected**: Separates code (GitHub) from pipelines (ADO), creating friction. Azure DevOps is powerful but introduces unnecessary complexity for this project scale.

### 2. Local/Script-based Deployment
- **Rejected for Production**: Scripts (`deploy.ps1`) are excellent for local dev loops but lack audit trails, secret management, and approval gates required for production.

## Implementation Details

- **Workflow Files**: 
  - `.github/workflows/deploy-infrastructure.yml`
  - `.github/workflows/deploy-services.yml`
- **Standards Doc**: `docs/ops/ci-cd.md`
