# Events Project - Cleanup & Simplification Action Plan

**Created**: January 30, 2026  
**Status**: In Progress  
**Timeline**: 3 weeks (prioritized implementation)

## Executive Summary

This action plan addresses critical technical debt and developer experience issues identified in the Events project. The plan focuses on configuration management, documentation consolidation, testing infrastructure, and deployment simplification.

---

## 🔴 CRITICAL Priority (Week 1 - Days 1-2)

### 1. Delete Unused docker-compose.yml ✅
**Issue**: 183-line docker-compose.yml exists but is not used in any deployment workflow, creating confusion for new developers  
**Impact**: High - Developer confusion, potential security issues (hardcoded passwords)  
**Effort**: 5 minutes  
**Action**: 
- Delete docker-compose.yml
- If local development orchestration is needed, create simplified version with external .env file
- Document local development setup in docs/development/guide.md

### 2. Create .env.example Template ✅
**Issue**: Configuration scattered across appsettings.json, Terraform, and environment variables with no documentation  
**Impact**: High - Impossible for new developers to configure services locally  
**Effort**: 30 minutes  
**Action**:
- Create .env.example with all required variables documented
- Include: SQL connection strings, Service Bus, Event Grid, Application Insights, JWT secrets
- Add comments explaining each variable's purpose and example values
- Reference from README.md

### 3. Fix .gitignore Entries ✅
**Issue**: .gitignore may be missing critical entries (.env, *.tfvars, local secrets)  
**Impact**: Critical - Risk of committing sensitive credentials  
**Effort**: 10 minutes  
**Action**:
- Verify .gitignore includes: .env, terraform.tfvars, *.user, bin/, obj/, appsettings.*.json (except templates)
- Add .azure/ folder if Azure CLI stores local state
- Ensure terraform.tfstate is ignored (currently tracked)

### 4. Remove Hardcoded Terraform URLs ✅
**Issue**: infrastructure/main.tf lines 315, 368, 382 contain hardcoded API Gateway URLs  
**Impact**: High - Prevents multi-environment deployments, breaks dev/test/prod isolation  
**Effort**: 20 minutes  
**Action**:
```hcl
# Replace hardcoded URLs with:
event_grid_subscriber_endpoint_url = "https://${azurerm_container_app.api_gateway.latest_revision_fqdn}/api/events/webhook"
```

### 5. Delete BUILD_SUMMARY.md Files ✅
**Issue**: BUILD_SUMMARY.md files in ApiGateway, EventRelay, ProjectionService are outdated snapshots  
**Impact**: Medium - Outdated information confuses developers  
**Effort**: 5 minutes  
**Action**: Delete all BUILD_SUMMARY.md files from service directories

### 6. Consolidate Documentation ✅
**Issue**: 8 documentation files with significant overlap and redundancy  
**Impact**: High - Developers can't find information, documentation drift  
**Effort**: 2 hours  
**Action**:
- Merge into 4 core documents:
  1. **README.md**: Quick start, tech stack, project overview
  2. **docs/development/guide.md**: Setup, configuration, local development, debugging
  3. **docs/architecture/overview.md**: System design, event schemas, patterns
  4. **docs/ops/deployment.md**: CI/CD, Azure deployment, troubleshooting
- Delete: development-mode-patterns.md (687 lines - too long), services-startup-guide.md, service-implementation-checklist.md
- Consolidate content from deleted files into appropriate sections above

---

## 🟡 HIGH Priority (Week 1 - Days 3-5)

### 7. Centralize Configuration Management
**Issue**: Configuration scattered across 5+ locations with no single source of truth  
**Impact**: High - Deployment failures, security vulnerabilities, difficult troubleshooting  
**Effort**: 4 hours  
**Action**:
- **Local Development**: .env file (ignored) → read by docker-compose or dotnet user-secrets
- **Azure Deployment**: Key Vault → referenced by Container Apps environment variables
- **Shared Settings**: appsettings.json (non-sensitive defaults only)
- **Terraform Variables**: terraform.tfvars (ignored) with terraform.tfvars.example checked in
- Remove all empty "" placeholders in appsettings.json files

### 8. Add Automated Tests
**Issue**: No test projects visible in Events.sln - zero automated testing coverage  
**Impact**: Critical - No validation of business logic, risky deployments  
**Effort**: 8 hours (initial setup)  
**Action**:
- Create test projects:
  - Shared.Events.Tests (unit tests for event schemas/validation)
  - ProspectService.Tests (unit + integration tests)
  - ApiGateway.Tests (WebSocket hub tests)
- Add to Events.sln and GitHub Actions workflow
- Target: 70% code coverage for handlers and domain logic

---

## 🟢 MEDIUM Priority (Week 2)

### 9. Standardize Logging & Observability
**Issue**: Inconsistent logging patterns across services  
**Impact**: Medium - Difficult to trace issues across microservices  
**Effort**: 3 hours  
**Action**:
- Implement structured logging with correlation ID propagation
- Add OpenTelemetry SDK to all services (already in project requirements)
- Configure Application Insights with consistent event names
- Document logging standards in docs/development/guide.md

### 10. Create Service Health Checks
**Issue**: No health check endpoints for Container Apps probes  
**Impact**: Medium - Poor reliability, no automatic recovery  
**Effort**: 2 hours  
**Action**:
- Add /health endpoints to all services
- Include: database connectivity, Service Bus connectivity, external dependency checks
- Configure Container Apps startup/liveness/readiness probes in Terraform

### 11. Implement Local Development Script
**Issue**: No automated local environment setup  
**Impact**: Medium - Painful onboarding for new developers  
**Effort**: 2 hours  
**Action**:
- Create setup-local.ps1 script:
  - Verify Docker Desktop running
  - Start Azurite container
  - Start SQL Server container
  - Apply EF migrations
  - Display connection strings
- Document in docs/development/guide.md

---

## 🔵 LOW Priority (Week 3+)

### 12. Add Architecture Decision Records (ADRs)
**Issue**: Key decisions (CQRS, Event-driven, Transactional Outbox) are undocumented  
**Impact**: Low - Future developers may not understand rationale  
**Effort**: 3 hours  
**Action**:
- Create docs/adr/ directory
- Document: Why event-driven, Why CQRS, Why Transactional Outbox, Auth strategy
- Use Markdown Any Decision Records (MADR) template

### 13. Create Deployment Smoke Tests
**Issue**: No automated validation after Azure deployment  
**Impact**: Medium - Broken deployments may not be detected  
**Effort**: 3 hours  
**Action**:
- Add smoke test job to GitHub Actions after deployment
- Test: API Gateway health, Prospect CRUD operations, Event Grid webhook
- Fail workflow if smoke tests don't pass

---

## Implementation Strategy

### Week 1: Developer Experience Foundation
**Focus**: Remove confusion, establish configuration patterns  
**Tasks**: Items 1-8 (Critical + High priority)  
**Goal**: New developers can clone repo and run services locally within 15 minutes

### Week 2: Code Quality & Reliability
**Focus**: Logging, health checks, development tooling  
**Tasks**: Items 9-11 (Medium priority)  
**Goal**: Production-ready observability and automatic recovery

### Week 3: Documentation & Testing
**Focus**: Knowledge capture, automated validation  
**Tasks**: Items 12-13 (Low priority)  
**Goal**: Self-documenting system with comprehensive test coverage

---

## Success Metrics

- ✅ **Configuration Clarity**: All config variables documented in .env.example
- ✅ **Documentation Quality**: Reduced from 8 files to 4 comprehensive guides
- ✅ **Test Coverage**: 70%+ coverage on handlers and domain logic
- ✅ **Deployment Confidence**: Smoke tests validate every deployment
- ✅ **Onboarding Speed**: New developer productive within 1 hour (vs current ~4 hours)
- ✅ **Security**: Zero secrets in git history, all sensitive data in Key Vault

---

## Dependencies & Risks

**Dependencies**:
- Azure Key Vault setup for secret management (Week 1, Item 7)
- Azurite and SQL Server containers for local development (Week 2, Item 11)

**Risks**:
- Configuration changes may require redeployment of all services
- Test infrastructure setup may uncover existing bugs (good thing!)
- Documentation consolidation requires coordination if multiple team members

**Mitigation**:
- Implement changes in feature branch, test thoroughly before merge
- Use Terraform plan to preview infrastructure changes
- Get team review on documentation structure before consolidation

---

## Notes

- This plan assumes single developer or small team
- Some tasks can be parallelized (e.g., tests + documentation)
- Priority levels may shift based on immediate business needs
- Track progress by checking off completed items and updating status
