#!/usr/bin/env pwsh

# ---------------------------------------------------------------------------
# Master Provisioning Script
# Orchestrates the full deployment lifecycle:
# 1. Infrastructure Core (Terraform)
# 2. Build & Push Images (ACR)
# 3. Database Migrations (SQL)
# 4. Infrastructure Apps (Terraform)
# ---------------------------------------------------------------------------

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev",

    [Parameter(Mandatory=$false)]
    [string]$Region = "eastus2",

    [Parameter(Mandatory=$false)]
    [switch]$ForceRecreate,

    [Parameter(Mandatory=$false)]
    [switch]$SkipConfirmation
)

$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot/.."
# --- Helper Functions ---
function Write-Header { param([string]$Msg) Write-Host "`n=================================================================="; Write-Host " $Msg"; Write-Host "==================================================================" -ForegroundColor Cyan }
function Write-Step { param([string]$Msg) Write-Host "`n---> $Msg" -ForegroundColor Yellow }
function Write-Success { param([string]$Msg) Write-Host "  [OK] $Msg" -ForegroundColor Green }

# --- 0. Prerequisites ---
Write-Header "Step 0: Prerequisites Check"

if (-not (Get-Command "terraform" -ErrorAction SilentlyContinue)) { Throw "Terraform is not installed." }
if (-not (Get-Command "az" -ErrorAction SilentlyContinue)) { Throw "Azure CLI is not installed." }
if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) { Throw "Git is not installed." }
if (-not (Get-Module -Name SqlServer -ListAvailable)) { Write-Warning "SqlServer module not found. SQL steps might fail. Install with 'Install-Module SqlServer'" }

# Check Terraform Backend (State Storage)
# Read the configured storage account from main.tf to see if it exists
Write-Header "Step 0.1: Verifying Terraform Backend"
try {
    $tfBackendConfig = Get-Content "infrastructure/core/main.tf" -Raw
    if ($tfBackendConfig -match 'storage_account_name\s*=\s*"([^"]+)"') {
        $configuredStorageAccount = $matches[1]
        Write-Host "Checking for backend storage: $configuredStorageAccount..." -NoNewline

        $exists = az storage account show --name $configuredStorageAccount --query "id" --output tsv 2>$null
        if (-not $exists) {
            Write-Host " [MISSING]" -ForegroundColor Red
            Throw "The Terraform state storage account '$configuredStorageAccount' does not exist in Azure.`n    ACTION REQUIRED: Run './scripts/setup-terraform-backend.ps1' and update the main.tf files with the new storage name."
        }
        Write-Host " [FOUND]" -ForegroundColor Green
    }
} catch {
    Write-Warning "Could not verify backend storage existence. Terraform might fail if rg-events-tfstate is missing."
}

# Check Azure Login
try {
    $azAccount = az account show --output json | ConvertFrom-Json
    Write-Success "Logged in as $($azAccount.user.name) ($($azAccount.id))"
} catch {
    Throw "Please run 'az login' first."
}

if (-not $SkipConfirmation) {
    $conf = Read-Host "This will provision/update the '$Environment' environment in '$Region'. Continue? (y/n)"
    if ($conf -ne 'y') { exit }
}

# --- 1. Infrastructure Core ---
Write-Header "Step 1: Infrastructure Core (Terraform)"
Set-Location "infrastructure/core"

Write-Step "Initializing Terraform (Core)..."
terraform init -upgrade

Write-Step "Applying Terraform (Core)..."
$tfArgs = @("-var=environment=$Environment", "-auto-approve")

if ($ForceRecreate) {
    Write-Warning "Usage of -ForceRecreate detected: Regenerating Random Suffix."
    Write-Warning "This changes resource names (KeyVault, ACR) to bypass Azure Soft-Delete conflicts."
    $tfArgs += "-replace=random_string.suffix"
}

terraform apply @tfArgs

Write-Step "Retrieving Outputs..."
$tfOutput = terraform output -json | ConvertFrom-Json
$acrLoginServer = $tfOutput.container_registry_login_server.value
$acrName = $acrLoginServer.Split(".")[0]
$sqlFqdn = $tfOutput.sql_server_fqdn.value
$sqlDbName = $tfOutput.transactional_database_name.value
$rgName = $tfOutput.resource_group_name.value

Write-Success "Core Infra Ready. ACR: $acrName, SQL: $sqlFqdn"

# 1.1 Update GitHub Secrets (Optional but recommended)
# Since ACR name might change with -ForceRecreate or new deployment, we should sync secrets.
Write-Header "Step 1.1: Update GitHub Secrets"
if ((Get-Command "gh" -ErrorAction SilentlyContinue) -and ($SkipConfirmation -or (Read-Host "Update GitHub Secrets (ACR_NAME, etc)? (y/n)") -eq 'y')) {
   Write-Step "Updating 'ACR_NAME' secret..."
   echo $acrName | gh secret set ACR_NAME

   # Ensure the SP used by GitHub has AcrPush on this new registry
   # Note: Real automation would create/get the SP here and assign role.
   # usage of separate setup-github-secrets.ps1 is recommended for full rotation.
   Write-Warning "Remember to run './scripts/setup-github-secrets.ps1' if you completely deleted the Resource Group, `n    as the Service Principal permissions need to be re-applied to the new ACR."
}

Set-Location "$PSScriptRoot/.."

# --- 2. Build & Push Images ---
Write-Header "Step 2: Build & Push Images"
Write-Step "Building images using ACR Task (faster than local docker build)..."

$images = @("api-gateway", "prospect-service", "student-service", "instructor-service", "event-relay", "projection-service", "frontend")
$root = $PWD

foreach ($img in $images) {
    Write-Step "Building $img..."

    $dockerfile = if ($img -eq "frontend") { "src/frontend/Dockerfile" } else { "src/services/${img}/Dockerfile" }
    $context = if ($img -eq "frontend") { "src/frontend" } else { "src/services" }

    # Simple logic to map service name to folder name (kebab to Pascal) if needed,
    # but Dockerfile paths above handle it.
    # Note: Service folders are PascalCase (ProspectService) but image names are kebab-case (prospect-service)

    # fix Dockerfile path for services
    if ($img -ne "frontend") {
        # Convert kebab-case (prospect-service) to PascalCase (ProspectService)
        $folderName = (Get-Culture).TextInfo.ToTitleCase($img.Replace("-", " ")).Replace(" ", "")
        $dockerfile = "src/services/$folderName/Dockerfile"
    }

    az acr build --registry $acrName --image "${img}:latest" --file $dockerfile $context
}

Write-Success "All images built and pushed."

# --- 3. Database Migration ---
Write-Header "Step 3: Database Migration"

# Add temporary firewall rule for local IP
Write-Step "Whitelisting local IP on SQL Server..."
$publicIp = (Invoke-RestMethod ipinfo.io/ip).Trim()
try {
    az sql server firewall-rule create --resource-group $rgName --server $sqlFqdn.Split(".")[0] --name "AllowLocalClient" --start-ip-address $publicIp --end-ip-address $publicIp 2>$null
    Write-Success "Added IP $publicIp to firewall."
} catch {
    Write-Warning "Could not add firewall rule (might already exist)."
}

Write-Step "Running Schema Setup (Transactional)..."
$sqlPassword = "P@ssw0rd123!SecureEventsDb"
$dbScript = "$PSScriptRoot/scripts/setup-db.ps1"

& $dbScript -Server $sqlFqdn -Database $sqlDbName -Username "sqladmin" -Password $sqlPassword -Mode "Transactional"

Write-Step "Running Schema Setup (ReadModel)..."
$readModelDbName = $tfOutput.readmodel_database_name.value
& $dbScript -Server $sqlFqdn -Database $readModelDbName -Username "sqladmin" -Password $sqlPassword -Mode "ReadModel"

Write-Success "Database tables created for Transctional and ReadModel DBs."

# --- 4. Infrastructure Apps ---
Write-Header "Step 4: Infrastructure Apps (Terraform)"
Set-Location "infrastructure/apps"

Write-Step "Initializing Terraform (Apps)..."
terraform init -upgrade

Write-Step "Applying Terraform (Apps)..."
terraform apply -var="environment=$Environment" -var="image_tag=latest" -auto-approve

Write-Success "Apps Deployed!"
Set-Location "$PSScriptRoot/.."

Write-Header "Provisioning Complete!"
Write-Host "Services should be reachable shortly." -ForegroundColor Green
