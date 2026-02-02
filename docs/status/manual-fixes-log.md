# Manual Infrastructure Updates Tracker

This document tracks manual interventions performed to resolve infrastructure deployment issues.

## 2026-02-01: Deployment Recovery

### 1. Key Vault Permissions Fix
**Issue:** `terraform plan` failed with checking Key Vault secrets (`403 Forbidden`).
**Root Cause:** The user/SPN running Terraform did not have `Key Vault Secrets Officer` role, and could not grant it to themselves via Terraform because Terraform couldn't even read the state of secrets.
**Resolution:**
- Manually assigned `Key Vault Secrets Officer` role to user `05fedae5-834e-4aa4-a6d5-7eab66cb466a` via Azure CLI.
- Imported the manual role assignment into Terraform state to prevent "Resource already exists" errors.
  ```bash
  terraform import 'azurerm_role_assignment.kv_terraform_admin["05fedae5-834e-4aa4-a6d5-7eab66cb466a"]' /subscriptions/3d4830e8-8095-45f1-8fbc-95fdc8569eb4/resourceGroups/rg-events-dev/providers/Microsoft.KeyVault/vaults/kv-events-dev-sagbkg/providers/Microsoft.Authorization/roleAssignments/9466b1fb-54b4-4b48-ac21-a1ceec50987d
  ```

### 2. Container App Updates & State Recovery
**Issue:** `terraform apply` failed with "Operation expired" and subsequently "Resource already exists" for Container Apps.
**Root Cause:**
- Timeouts: The default timeout (30m) was sufficient, but earlier deployments might have hung or failed silently in state. (Increased to 60m in code).
- State Drift: `student-service` and `instructor-service` existed in Azure but were missing from Terraform state (likely due to previous timeout/crash during creation).
**Resolution:**
- Imported existing Container Apps into state:
  ```bash
  terraform import 'module.container_apps.azurerm_container_app.apps["student-service"]' /subscriptions/3d4830e8-8095-45f1-8fbc-95fdc8569eb4/resourceGroups/rg-events-dev/providers/Microsoft.App/containerApps/ca-events-student-service-dev
  terraform import 'module.container_apps.azurerm_container_app.apps["instructor-service"]' /subscriptions/3d4830e8-8095-45f1-8fbc-95fdc8569eb4/resourceGroups/rg-events-dev/providers/Microsoft.App/containerApps/ca-events-instructor-service-dev
  ```

### 3. Pending Issues (Action Required)
**Issue:** Container Apps for `student-service` and `instructor-service` are failing to provision with `ContainerAppOperationError`:
> Field 'template.containers.student-service.image' is invalid with details: 'Invalid value: "acreventsdevrcwv3i.azurecr.io/student-service:latest": unable to pull image using Managed identity system...'

**Probable Causes:**
1. **Missing Images:** The Docker images have not been built/pushed to the ACR yet.
2. **Permission Propagation:** The `AcrPull` role assignment for the Managed Identity was just created and needs 5-10 minutes to propagate.

**Next Steps:**
- Run the build pipeline to ensure images exist: `.\deploy.ps1 -SkipTerraform` (or full deploy).
- If images exist, retry `terraform apply` after 15 minutes.
