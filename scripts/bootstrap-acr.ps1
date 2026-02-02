#!/usr/bin/env pwsh
Set-Location "$PSScriptRoot/.."
# Bootstrap ACR for the first deployment or after environment destruction
# This script creates ONLY the Azure Container Registry using Terraform
# so that the GitHub Actions "Build" step has a target to push images to.

$ErrorActionPreference = "Stop"

Write-Host "Bootstrapping Azure Container Registry..." -ForegroundColor Cyan

# Check dependencies
if (-not (Get-Command "terraform" -ErrorAction SilentlyContinue)) {
    Write-Error "Terraform is not installed."
    exit 1
}
if (-not (Get-Command "az" -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed."
    exit 1
}

# Get current context
Write-Host "Getting Azure context..."
try {
    $azContext = az account show --output json | ConvertFrom-Json
    $subscriptionId = $azContext.id
    $tenantId = $azContext.tenantId
    Write-Host "Using Subscription: $($azContext.name) ($subscriptionId)" -ForegroundColor Green
}
catch {
    Write-Error "Please login to Azure CLI using 'az login'"
    exit 1
}

# Variables matching GitHub Workflow
$acrName = "acreventsdevrcwv3i"
$projectName = "events"
$environment = "dev"
$location = "eastus2"

# Generate a placeholder SQL Password for validation
# (Terraform requires all variables to be present, even if not used in the targeted module)
Write-Host "`nTerraform requires all variables to be set regardless of the target." -ForegroundColor DarkGray
Write-Host "Generating placeholder SQL password..." -ForegroundColor DarkGray
$sqlPasswordPlain = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object { [char]$_ }) + "1!"

# Initialize Terraform
Write-Host "`nInitializing Terraform..." -ForegroundColor Cyan
Push-Location "infrastructure"
try {
    terraform init
}
catch {
    Write-Error "Failed to initialize Terraform. Ensure you have access to the remote state storage account."
    Pop-Location
    exit 1
}

# Apply Targeted Terraform
Write-Host "`nCreating ACR '$acrName' via Terraform..." -ForegroundColor Cyan
Write-Host "Target: module.container_registry" -ForegroundColor Magenta

terraform apply `
    -target="module.container_registry" `
    -var="subscription_id=$subscriptionId" `
    -var="tenant_id=$tenantId" `
    -var="project_name=$projectName" `
    -var="environment=$environment" `
    -var="location=$location" `
    -var="acr_name=$acrName" `
    -var="sql_admin_username=sqladmin" `
    -var="sql_admin_password=$sqlPasswordPlain" `
    -auto-approve

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS! ACR created." -ForegroundColor Green
    Write-Host "You can now re-run the GitHub Action workflow." -ForegroundColor Green
} else {
    Write-Error "Failed to create ACR."
}

Pop-Location
