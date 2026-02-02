#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Master setup script for Events project.
#>

$ErrorActionPreference = "Stop"

function Show-Menu {
    Clear-Host
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host "   EVENTS PROJECT SETUP" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host "1. Setup Terraform Backend (One-time)"
    Write-Host "2. Bootstrap Container Registry (One-time)"
    Write-Host "3. Setup Local Environment (Docker + Local DBs)"
    Write-Host "4. Check/Generate Database Migrations"
    Write-Host "5. Setup User Secrets (.NET)"
    Write-Host "6. Build & Deploy to Azure"
    Write-Host "7. Setup GitHub Secrets (CI/CD)"
    Write-Host "8. Key Vault: Assign Role"
    Write-Host "9. Key Vault: Add OID to terraform.tfvars"
    Write-Host "10. Terraform: Import Role Assignment"
    Write-Host "11. Terraform: Move State for SPN"
    Write-Host "Q. Quit"
    Write-Host "==============================" -ForegroundColor Cyan
}

while ($true) {
    Show-Menu
    $choice = Read-Host "Select an option"

    switch ($choice) {
        "1" { ./scripts/setup-terraform-backend.ps1; Pause }
        "2" { ./scripts/bootstrap-acr.ps1; Pause }
        "3" { ./scripts/setup-local.ps1; Pause }
        "4" { ./scripts/db-migrations.ps1; Pause }
        "5" { ./scripts/setup-user-secrets.ps1; Pause }
        "6" { ./scripts/deploy.ps1; Pause }
        "7" { ./scripts/setup-github-secrets.ps1; Pause }
        "8" {
            $userOid = Read-Host "Enter your Azure AD Object ID"
            $kvResourceId = Read-Host "Enter Key Vault Resource ID"
            pwsh -File ./scripts/setup-keyvault-and-terraform.ps1 -UserObjectId $userOid -KeyVaultResourceId $kvResourceId -Command "Assign-KeyVaultRole" -TerraformStateDir "./infrastructure/core" -TerraformTfvarsPath "./infrastructure/core/terraform.tfvars"
            Pause
        }
        "9" {
            $userOid = Read-Host "Enter your Azure AD Object ID"
            pwsh -File ./scripts/setup-keyvault-and-terraform.ps1 -UserObjectId $userOid -Command "Add-Oid-To-Tfvars" -TerraformTfvarsPath "./infrastructure/core/terraform.tfvars"
            Pause
        }
        "10" {
            $userOid = Read-Host "Enter your Azure AD Object ID"
            $roleAssignmentId = Read-Host "Enter Role Assignment Resource ID"
            pwsh -File ./scripts/setup-keyvault-and-terraform.ps1 -UserObjectId $userOid -Command "Import-Terraform-RoleAssignment" -RoleAssignmentResourceId $roleAssignmentId -TerraformStateDir "./infrastructure/core"
            Pause
        }
        "11" {
            $spnOid = Read-Host "Enter SPN Object ID"
            pwsh -File ./scripts/setup-keyvault-and-terraform.ps1 -Command "Move-Terraform-State" -SpnOid $spnOid -TerraformStateDir "./infrastructure/core"
            Pause
        }
        "Q" { exit }
        "q" { exit }
        default { Write-Host "Invalid option"; Start-Sleep -Seconds 1 }
    }
}
