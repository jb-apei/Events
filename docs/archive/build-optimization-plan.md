
### 6. Optimize Build & Deploy Pipeline (New Enhancement)
**Issue**: The current CI/CD pipeline performs a full "world-rebuild" (Test -> Terraform -> Build All -> Deploy) on every commit, taking 15+ minutes.
**Impact**: High - Slow feedback loop for developers. Small code changes take too long to deploy.
**Effort**: Medium-High
**Action**:
- Split workflow into specific paths:
  - `infrastructure/**` trigger -> Runs Terraform.
  - `src/**` trigger -> Runs Build & Deploy.
- Implement "Code Only" fast path that skips Terraform state locking.
- Optimize Docker builds to run in parallel without waiting for Terraform if infra didn't change.
- Use `az containerapp update` for image updates instead of full revision restarts.
