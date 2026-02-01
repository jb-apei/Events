# Manual Configuration Tracker

This document tracks manual configuration steps, workarounds, and "glue" actions performed during the development and deployment of the Events project. The goal is to eventually automate these steps or document them as strict prerequisites.

## Azure Infrastructure

### Key Vault Access for Terraform Runner
**Date:** 2026-02-01
**Context:** Terraform Plan failures (403 Forbidden) when reading Key Vault secrets during local execution.
**Issue:** The user running `terraform plan` (local developer or CI runner) must have data-plane access ("Key Vault Secrets Officer") to the Key Vault to read/manage secrets. The Terraform configuration manages these assignments, but circular dependency issues (need permission to plan, plan applies permission) or state refactoring can require manual intervention.
**Manual Action:**
1. Manually assigned role via CLI to unblock the user:
   ```powershell
   az role assignment create --assignee "<user-oid>" --role "Key Vault Secrets Officer" --scope "<key-vault-resource-id>"
   ```
2. Added User OID to `terraform.tfvars` to persist the access:
   ```hcl
   key_vault_admin_object_ids = [
     "ab4af2de-2910-4277-9b31-5065a8f7ef21", # GitHub Actions SPN
     "05fedae5-834e-4aa4-a6d5-7eab66cb466a"  # Local Developer (jbouchard)
   ]
   ```
3. Imported the manual assignment into Terraform state (required because `main.tf` was refactored from a single resource to `for_each`):
   ```powershell
   terraform import 'azurerm_role_assignment.kv_terraform_admin["<user-oid>"]' "<role-assignment-resource-id>"
   ```
**Automation Goal:** Ensure the bootstrap script (`setup-local.ps1` or similar) validates that the current user has this permission before running Terraform, or improve the Terraform design to handle the "terraform admin" identity more robustly.

## Local Development Setup

### Terraform Variables
**Context:** `infrastructure/terraform.tfvars` is gitignored to secure sensitive data.
**Manual Action:** New developers must create this file manually based on `terraform.tfvars.example` and populate it with:
- `subscription_id`
- `tenant_id`
- `sql_admin_password`
- `key_vault_admin_object_ids` (Crucial for correct permissioning)

## Terraform State Management

### State Refactoring
**Date:** 2026-02-01
**Context:** Enabled multiple admins for Key Vault.
**Manual Action:** Moved existing state for the GitHub Actions SPN to the new `for_each` key format to avoid resource recreation/deletion.
```powershell
terraform state mv azurerm_role_assignment.kv_terraform_admin 'azurerm_role_assignment.kv_terraform_admin["<spn-oid>"]'
```
