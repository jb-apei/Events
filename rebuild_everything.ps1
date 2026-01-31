#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Master script to completely rebuild the Events environment from scratch.

.DESCRIPTION
    This script coordinates the entire destruction and reconstruction of the cloud environment.
    It executes the standard scripts in the correct order to ensure dependency resolution.

    Order of Operations:
    1. (Optional) Terraform Destroy (tears down all Azure resources)
    2. Setup Terraform Backend (Azure Storage for state)
    3. Bootstrap ACR (Create Container Registry separately first)
    4. Setup GitHub Secrets (Configures CI/CD credentials)
    5. Full Deployment (Build Images -> Apply Infrastructure -> Deploy Apps)

.PARAMETER Destroy
    If set, attempts to destroy all existing infrastructure before rebuilding.
    WARNING: THIS WILL DELETE DATA.

.EXAMPLE
    .\rebuild_everything.ps1
    Updates the environment or builds it if missing (idempotent).

.EXAMPLE
    .\rebuild_everything.ps1 -Destroy
    Tears down EVERYTHING and starts from zero.
#>

param(
    [switch]$Destroy
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
}

# 1. Credentials Check
Write-Step "Step 1: Checking Credentials"
$azAccount = az account show | ConvertFrom-Json
Write-Host "Azure Subscription: $($azAccount.name) ($($azAccount.id))" -ForegroundColor Green

$ghStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not logged into GitHub CLI. Please run 'gh auth login'"
    exit 1
}
Write-Host "GitHub CLI: Authenticated" -ForegroundColor Green

# 2. (Optional) Destruction
if ($Destroy) {
    Write-Step "Step 2: Destroying Existing Infrastructure"
    Write-Warning "This will delete all resources in the resource group. 5 seconds to cancel..."
    Start-Sleep -Seconds 5

    Push-Location infrastructure
    if (Test-Path ".terraform") {
        Write-Host "Running terraform destroy..."
        terraform destroy -auto-approve
    } else {
        Write-Warning "Terraform not initialized. Skipping 'terraform destroy' command."
        Write-Warning "If resource group exists, manual deletion might be required."
    }
    Pop-Location
}

# 3. Setup Terraform State Backend
Write-Step "Step 3: Configuring Terraform State Backend"
./setup-terraform-backend.ps1

# 4. Bootstrap ACR
Write-Step "Step 4: Bootstrapping Azure Container Registry"
# This ensures ACR exists so we can push images in the next step
./bootstrap-acr.ps1

# 5. Setup GitHub Secrets
Write-Step "Step 5: Configuring GitHub Secrets"
# Ensures Service Principal exists and repo secrets are set for CI/CD
./setup-github-secrets.ps1

# 6. Full Deployment
Write-Step "Step 6: Executing Full Deployment"
# Builds images, Applies full Terraform (databases, container apps, etc.), Restarts apps
./deploy.ps1

Write-Step "Rebuild Complete!"
Write-Host "Your environment should be fully operational." -ForegroundColor Green
Write-Host "API Gateway URL should be in the outputs above." -ForegroundColor Gray
