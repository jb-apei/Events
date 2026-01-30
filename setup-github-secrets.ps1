#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automatically set up GitHub secrets for Azure deployment

.DESCRIPTION
    This script retrieves Azure credentials and sets them as GitHub secrets
    for the GitHub Actions workflow to use.
#>

$ErrorActionPreference = "Stop"

Write-Host "`n==> Setting up GitHub Secrets for Azure Deployment" -ForegroundColor Cyan

# Check if logged into GitHub
Write-Host "`n--> Checking GitHub CLI authentication..." -ForegroundColor Yellow
try {
    gh auth status 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Not logged into GitHub. Running 'gh auth login'..." -ForegroundColor Yellow
        gh auth login
    }
    Write-Host "[OK] GitHub CLI authenticated" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] GitHub CLI authentication failed" -ForegroundColor Red
    exit 1
}

# Get Azure account info
Write-Host "`n--> Getting Azure account information..." -ForegroundColor Yellow
$account = az account show | ConvertFrom-Json

if (-not $account) {
    Write-Host "[ERROR] Not logged into Azure. Run 'az login' first." -ForegroundColor Red
    exit 1
}

$subscriptionId = $account.id
$tenantId = $account.tenantId

Write-Host "[OK] Found subscription: $($account.name)" -ForegroundColor Green

# Check if service principal exists
Write-Host "`n--> Checking for existing service principal..." -ForegroundColor Yellow
$spName = "github-actions-events"
$sp = az ad sp list --display-name $spName | ConvertFrom-Json

if ($sp.Count -eq 0) {
    Write-Host "Service principal not found. Creating new one..." -ForegroundColor Yellow
    
    # Create service principal with Contributor role
    Write-Host "--> Creating service principal: $spName" -ForegroundColor Yellow
    $spOutput = az ad sp create-for-rbac `
        --name $spName `
        --role Contributor `
        --scopes "/subscriptions/$subscriptionId/resourceGroups/rg-events-dev" `
        --output json | ConvertFrom-Json
    
    $clientId = $spOutput.appId
    $clientSecret = $spOutput.password
    
    Write-Host "[OK] Service principal created" -ForegroundColor Green
    
    # Grant ACR push permission
    Write-Host "--> Granting ACR push permissions..." -ForegroundColor Yellow
    az role assignment create `
        --assignee $clientId `
        --role "AcrPush" `
        --scope "/subscriptions/$subscriptionId/resourceGroups/rg-events-dev/providers/Microsoft.ContainerRegistry/registries/acreventsdevrcwv3i" `
        --output none
    
    Write-Host "[OK] ACR permissions granted" -ForegroundColor Green
}
else {
    Write-Host "[OK] Service principal already exists" -ForegroundColor Green
    $clientId = $sp[0].appId
    
    # Need to reset the secret since we can't retrieve it
    Write-Host "--> Resetting service principal credentials..." -ForegroundColor Yellow
    $resetOutput = az ad sp credential reset --id $clientId --output json | ConvertFrom-Json
    $clientSecret = $resetOutput.password
    
    Write-Host "[OK] Credentials reset" -ForegroundColor Green
}

# Set GitHub secrets
Write-Host "`n--> Setting GitHub secrets..." -ForegroundColor Yellow

$secrets = @{
    "ARM_CLIENT_ID" = $clientId
    "ARM_CLIENT_SECRET" = $clientSecret
    "ARM_SUBSCRIPTION_ID" = $subscriptionId
    "ARM_TENANT_ID" = $tenantId
}

foreach ($key in $secrets.Keys) {
    Write-Host "Setting $key..." -ForegroundColor Gray
    $value = $secrets[$key]
    echo $value | gh secret set $key
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] $key set successfully" -ForegroundColor Green
    }
    else {
        Write-Host "[ERROR] Failed to set $key" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n==> GitHub Secrets Setup Complete!" -ForegroundColor Green
Write-Host "`nThe following secrets have been set:" -ForegroundColor Cyan
Write-Host "  - ARM_CLIENT_ID: $clientId" -ForegroundColor Gray
Write-Host "  - ARM_CLIENT_SECRET: ********" -ForegroundColor Gray
Write-Host "  - ARM_SUBSCRIPTION_ID: $subscriptionId" -ForegroundColor Gray
Write-Host "  - ARM_TENANT_ID: $tenantId" -ForegroundColor Gray

Write-Host "`nYour GitHub Actions workflow is now ready to deploy to Azure!" -ForegroundColor Green
Write-Host "Push to the master branch to trigger a deployment.`n" -ForegroundColor Cyan
