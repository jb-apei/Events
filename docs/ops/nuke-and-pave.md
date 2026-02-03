# Nuke and Pave: Complete Environment Reset Guide

This guide documents the process for completely destroying and re-provisioning the Azure environment for the Events project ("Nuke and Pave"). This approach is useful for disaster recovery testing, eliminating configuration drift, or resetting the environment after major infrastructure refactoring.

## ⚠️ Warning
**This process allows for permanent data loss.**
*   All databases (Prospects, Students, Instructors) will be deleted.
*   All message queues (Service Bus) will be cleared.
*   All configuration history (Terraform State) can be optionally reset.

---

## Prerequisites

Ensure you have the following tools installed and authenticated:
*   **PowerShell 7+** (`pwsh`)
*   **Azure CLI** (`az login`)
*   **Terraform** (`terraform`)
*   **GitHub CLI** (`gh auth login`) - Required for updating repository secrets.

---

## Scenario A: Standard Reset (Preserve Terraform State)

Use this when you want to wipe the application resources but keep the detailed history of your infrastructure state intact.

1.  **Delete the Workload Resource Group**:
    ```powershell
    az group delete --name rg-events-dev --yes --no-wait
    ```

2.  **Re-provision w/ Soft Delete Bypass**:
    Since Key Vaults and Container Registries utilize "Soft Delete" (holding the name for 90 days), you must use the `-ForceRecreate` flag. This instructs Terraform to generate new random suffixes for these resources.

    ```powershell
    ./scripts/provision-environment.ps1 -ForceRecreate
    ```

3.  **Update GitHub Service Principal Permissions**:
    The Service Principal used by GitHub Actions loses access when the Resource Group is deleted. You must restore its Contributor/AcrPush roles.
    ```powershell
    ./scripts/setup-github-secrets.ps1
    ```

---

## Scenario B: The "Full Nuke" (Destroy Everything)

Use this when you want to simulate a completely fresh subscription start, or if your Terraform State (backend) has become corrupted.

### Phase 1: Destruction

1.  **Delete Workload Resources**:
    ```powershell
    az group delete --name rg-events-dev --yes --no-wait
    ```

2.  **Delete Terraform Control Plane**:
    This deletes the Storage Account holding your `.tfstate` files.
    ```powershell
    az group delete --name rg-events-tfstate --yes --no-wait
    ```

3.  **Wait**:
    Azure resource deletion is async. Wait 5-10 minutes for the groups to disappear.
    ```powershell
    az group list --output table
    ```

### Phase 2: Reconstruction

1.  **Rebuild Control Plane (Terraform Backend)**:
    This creates a fresh Storage Account for Terraform state.
    ```powershell
    ./scripts/setup-terraform-backend.ps1
    ```

2.  **Update Terraform Configuration**:
    The previous step outputs the new **Storage Account Name**. You **MUST** manually update it in two files because Terraform backend config cannot be parameterized with variables.

    *   Edit `infrastructure/core/main.tf`
    *   Edit `infrastructure/apps/main.tf`

    Update the `backend "azurerm"` block:
    ```hcl
    backend "azurerm" {
      resource_group_name  = "rg-events-tfstate"
      storage_account_name = "steventsstate<NEW_NUMBER>" # <--- UPDATE THIS
      container_name       = "tfstate"
      key                  = "core.tfstate" # (or apps.tfstate)
    }
    ```

3.  **Run Master Provisioner**:
    The `--ForceRecreate` flag is still recommended to ensure unique names for KeyVault/ACR.
    ```powershell
    ./scripts/provision-environment.ps1 -ForceRecreate
    ```
    *Follow the prompts to update GitHub Secrets during execution.*

4.  **Restore Helper Permissions**:
    ```powershell
    ./scripts/setup-github-secrets.ps1
    ```

---

## What the Automation Handles

The `provision-environment.ps1` script orchestrates the strict dependency order:
1.  **Infra Core**: Creates ACR, SQL Server, Service Bus.
2.  **Build Images**: Builds Docker images & pushes to the new ACR (Required before Apps can deploy).
3.  **DB Schema**: Whitelists local IP, runs `setup-db.ps1` to create Tables (Transactional & ReadModel).
4.  **Infra Apps**: Deploys Container Apps & Event Grid Subscriptions (Requires healthy DB/API Gateway).

## Troubleshooting

**Error: "Key Vault ... is in a soft deleted state"**
*   **Cause**: You didn't use `-ForceRecreate`, and Terraform tried to use the old name.
*   **Fix**: Run `./scripts/provision-environment.ps1 -ForceRecreate`.

**Error: "Storage Account ... does not exist"**
*   **Cause**: You deleted `rg-events-tfstate` but didn't update `main.tf` with the new storage name.
*   **Fix**: See Phase 2, Step 2 above.

**Error: "Login failed for user 'sqladmin'"**
*   **Cause**: Database schema script failed.
*   **Fix**: Ensure your local IP is allowed (the script attempts this, but corporate firewalls may block `ipinfo.io`). Manually add your IP via Azure Portal if needed.
