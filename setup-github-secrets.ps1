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
    # Note: This might fail if ACR doesn't exist yet, which is expected on first run.
    try {
        az role assignment create `
            --assignee $clientId `
            --role "AcrPush" `
            --scope "/subscriptions/$subscriptionId/resourceGroups/rg-events-dev/providers/Microsoft.ContainerRegistry/registries/acreventsdevrcwv3i" `
            --output none
        Write-Host "[OK] ACR permissions granted" -ForegroundColor Green
    } catch {
        Write-Host "[WARN] Could not grant ACR permissions (ACR might not exist yet)" -ForegroundColor Yellow
    }
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

# ---------------------------------------------------------
# Ensure Repeatable Role Assignments
# ---------------------------------------------------------
Write-Host "`n--> Verifying Role Assignments..." -ForegroundColor Yellow

# 1. Contributor on rg-events-dev (Deployment Target)
$devRg = "rg-events-dev"
if (az group exists --name $devRg) {
    Write-Host "Granting Contributor on $devRg..." -ForegroundColor Gray
    az role assignment create --assignee $clientId --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/$devRg" --output none
} else {
    Write-Host "[WARN] $devRg does not exist yet. Terraform will create it, but SP needs permissions on Subscription to do so." -ForegroundColor Yellow
    # Ideally, we grant on Subscription level if RG doesn't exist, but we refrain to keep scope tight.
    # The initial 'create-for-rbac' usually handles the creation of the RG if scoped there.
}

# 2. Contributor on rg-events-tfstate (Terraform State)
$tfStateRg = "rg-events-tfstate"
$storageAccountKey = ""

if (az group exists --name $tfStateRg) {
    Write-Host "Granting Contributor on $tfStateRg..." -ForegroundColor Gray
    az role assignment create --assignee $clientId --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/$tfStateRg" --output none
    Write-Host "[OK] Access to Terraform State verified" -ForegroundColor Green

    # Fetch Storage Key for GitHub Secret
    Write-Host "Fetching Storage Account Key..." -ForegroundColor Gray
    $storageAccountName = az storage account list --resource-group $tfStateRg --query "[?contains(name, 'steventsstate')].name" -o tsv | Select-Object -First 1
    if ($storageAccountName) {
        $storageAccountKey = az storage account keys list --resource-group $tfStateRg --account-name $storageAccountName --query '[0].value' -o tsv
        Write-Host "[OK] Retrieved storage key for $storageAccountName" -ForegroundColor Green
    } else {
        Write-Host "[WARN] No storage account found in $tfStateRg" -ForegroundColor Yellow
    }
} else {
     Write-Host "[WARN] $tfStateRg does not exist. Please run setup-terraform-backend.ps1 first." -ForegroundColor Red
}

# Get SQL Admin credentials
Write-Host "`n--> Configuring SQL Admin credentials..." -ForegroundColor Yellow
$sqlAdminUsername = Read-Host "Enter SQL Admin Username (default: sqladmin)"
if ([string]::IsNullOrWhiteSpace($sqlAdminUsername)) {
    $sqlAdminUsername = "sqladmin"
}

$sqlAdminPassword = Read-Host -AsSecureString "Enter SQL Admin Password (must be strong!)"
$sqlAdminPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlAdminPassword))

while ([string]::IsNullOrWhiteSpace($sqlAdminPasswordPlain)) {
    Write-Host "Password cannot be empty." -ForegroundColor Red
    $sqlAdminPassword = Read-Host -AsSecureString "Enter SQL Admin Password"
    $sqlAdminPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlAdminPassword))
}

# Set GitHub secrets
Write-Host "`n--> Setting GitHub secrets..." -ForegroundColor Yellow

# Create the JSON object for azure/login action (standard format)
$azureCredentials = @{
    clientId = $clientId
    clientSecret = $clientSecret
    subscriptionId = $subscriptionId
    tenantId = $tenantId
    resourceManagerEndpointUrl = "https://management.azure.com/"
} | ConvertTo-Json -Compress

$secrets = @{
    "AZURE_CREDENTIALS"   = $azureCredentials
    "ARM_CLIENT_ID"       = $clientId
    "ARM_CLIENT_SECRET"   = $clientSecret
    "ARM_SUBSCRIPTION_ID" = $subscriptionId
    "ARM_TENANT_ID"       = $tenantId
    "TF_ARM_ACCESS_KEY"   = $storageAccountKey
    "SQL_ADMIN_USERNAME"  = $sqlAdminUsername
    "SQL_ADMIN_PASSWORD"  = $sqlAdminPasswordPlain
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
Write-Host "  - TF_ARM_ACCESS_KEY: ********" -ForegroundColor Gray
Write-Host "  - ARM_TENANT_ID: $tenantId" -ForegroundColor Gray
Write-Host "  - SQL_ADMIN_USERNAME: $sqlAdminUsername" -ForegroundColor Gray
Write-Host "  - SQL_ADMIN_PASSWORD: ********" -ForegroundColor Gray

Write-Host "`nYour GitHub Actions workflow is now ready to deploy to Azure!" -ForegroundColor Green
Write-Host "Push to the master branch to trigger a deployment.`n" -ForegroundColor Cyan
