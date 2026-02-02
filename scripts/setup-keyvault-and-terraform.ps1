# Script: setup-keyvault-and-terraform.ps1
# Purpose: Automate manual steps for Key Vault role assignment and Terraform state management
# Usage: Run in PowerShell after logging in with az login

param(
    [string]$UserObjectId,
    [string]$KeyVaultResourceId,
    [string]$TerraformStateDir = "../infrastructure",
    [string]$TerraformTfvarsPath = "../infrastructure/terraform.tfvars"
)

function Assign-KeyVaultRole {
    Write-Host "Assigning Key Vault Secrets Officer role..."
    az role assignment create --assignee $UserObjectId --role "Key Vault Secrets Officer" --scope $KeyVaultResourceId
}

function Add-Oid-To-Tfvars {
    Write-Host "Adding user OID to terraform.tfvars..."
    $tfvarsContent = Get-Content $TerraformTfvarsPath -Raw
    if ($tfvarsContent -notmatch $UserObjectId) {
        $pattern = 'key_vault_admin_object_ids\s*=\s*\[(.*?)\]' 
        if ($tfvarsContent -match $pattern) {
            $newContent = $tfvarsContent -replace $pattern, "key_vault_admin_object_ids = [`$1, \"$UserObjectId\"]"
            Set-Content -Path $TerraformTfvarsPath -Value $newContent
            Write-Host "OID added to terraform.tfvars."
        } else {
            Write-Warning "key_vault_admin_object_ids not found in terraform.tfvars. Please add manually."
        }
    } else {
        Write-Host "OID already present in terraform.tfvars."
    }
}

function Import-Terraform-RoleAssignment {
    param(
        [string]$RoleAssignmentResourceId
    )
    Write-Host "Importing role assignment into Terraform state..."
    Push-Location $TerraformStateDir
    terraform import "azurerm_role_assignment.kv_terraform_admin[\"$UserObjectId\"]" "$RoleAssignmentResourceId"
    Pop-Location
}

function Move-Terraform-State {
    param(
        [string]$SpnOid
    )
    Write-Host "Moving Terraform state for SPN..."
    Push-Location $TerraformStateDir
    terraform state mv azurerm_role_assignment.kv_terraform_admin "azurerm_role_assignment.kv_terraform_admin[\"$SpnOid\"]"
    Pop-Location
}

# Example usage:
# .\setup-keyvault-and-terraform.ps1 -UserObjectId "<user-oid>" -KeyVaultResourceId "<key-vault-resource-id>"
# Then call Import-Terraform-RoleAssignment and Move-Terraform-State as needed

Write-Host "Script loaded. Call functions as needed."
