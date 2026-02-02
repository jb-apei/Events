#!/usr/bin/env pwsh
Set-Location "$PSScriptRoot/.."
$ErrorActionPreference = "Stop"

Write-Host "`n==> Setting up Terraform Backend (Remote State)" -ForegroundColor Cyan

# Configuration
$location = "eastus2"
$resourceGroupName = "rg-events-tfstate"
$containerName = "tfstate"
# Generate a unique storage account name
$randomSuffix = Get-Random -Minimum 1000 -Maximum 9999
$storageAccountName = "steventsstate$randomSuffix"

# 1. Create Resource Group
Write-Host "`n--> Creating Resource Group '$resourceGroupName'..." -ForegroundColor Yellow
if ((az group exists --name $resourceGroupName) -eq 'false') {
    az group create --name $resourceGroupName --location $location | Out-Null
    Write-Host "[OK] Resource Group created" -ForegroundColor Green
} else {
    Write-Host "[OK] Resource Group already exists" -ForegroundColor Green
}

# 2. Create Storage Account
Write-Host "`n--> Creating Storage Account (this may take a minute)..." -ForegroundColor Yellow
$account = az storage account list --resource-group $resourceGroupName --query "[?contains(name, 'steventsstate')].name" -o tsv

if (-not $account) {
    Write-Host "Creating $storageAccountName..." -ForegroundColor Gray
    az storage account create `
        --resource-group $resourceGroupName `
        --name $storageAccountName `
        --sku Standard_LRS `
        --encryption-services blob `
        --allow-blob-public-access false `
        --output none
    $currentStorageName = $storageAccountName
    Write-Host "[OK] Storage Account created: $currentStorageName" -ForegroundColor Green
} else {
    $currentStorageName = $account
    Write-Host "[OK] Found existing storage account: $currentStorageName" -ForegroundColor Green
}

# 3. Create Blob Container
Write-Host "`n--> Creating Blob Container '$containerName'..." -ForegroundColor Yellow
$accountKey = az storage account keys list --resource-group $resourceGroupName --account-name $currentStorageName --query '[0].value' -o tsv

az storage container create `
    --name $containerName `
    --account-name $currentStorageName `
    --account-key $accountKey `
    --output none | Out-Null

Write-Host "[OK] Container created" -ForegroundColor Green

# 4. Output configuration for main.tf
Write-Host "`n==> SETUP COMPLETE!" -ForegroundColor Cyan
Write-Host "`nPlease update your infrastructure/main.tf with the following block:" -ForegroundColor Yellow
Write-Host "--------------------------------------------------------" -ForegroundColor Gray
Write-Host "terraform {"
Write-Host "  backend ""azurerm"" {"
Write-Host "    resource_group_name  = ""$resourceGroupName"""
Write-Host "    storage_account_name = ""$currentStorageName"""
Write-Host "    container_name       = ""$containerName"""
Write-Host "    key                  = ""terraform.tfstate"""
Write-Host "  }"
Write-Host "}"
Write-Host "--------------------------------------------------------" -ForegroundColor Gray

# 5. Set GitHub Secret for Storage Key (Optional but recommended for pipeline access)
Write-Host "`nDo you want to save the Storage Account Key to GitHub Secrets now?" -ForegroundColor Cyan
$response = Read-Host "Type 'y' to save 'TF_ARM_ACCESS_KEY', or any other key to skip"

if ($response -eq 'y') {
    Write-Host "Setting TF_ARM_ACCESS_KEY..." -ForegroundColor Gray
    echo $accountKey | gh secret set TF_ARM_ACCESS_KEY
    Write-Host "[OK] Secret set!" -ForegroundColor Green
}
