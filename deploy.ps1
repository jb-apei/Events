
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Complete deployment script for Events project to Azure

.DESCRIPTION
    This script automates the entire deployment process:
    1. Builds all container images in Azure ACR
    2. Applies Terraform infrastructure changes
    3. Restarts container apps with new images
    4. Verifies deployment health

.PARAMETER RegistryName
    Azure Container Registry name (default: acreventsdevrcwv3i)

.PARAMETER ResourceGroup
    Azure resource group name (default: rg-events-dev)

.PARAMETER SkipBuild
    Skip building container images

.PARAMETER SkipTerraform
    Skip Terraform apply

.PARAMETER SkipRestart
    Skip restarting container apps

.EXAMPLE
    .\deploy.ps1
    Full deployment with all steps

.EXAMPLE
    .\deploy.ps1 -SkipBuild
    Deploy infrastructure and restart apps without rebuilding images
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$RegistryName = "acreventsdevrcwv3i",

    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-events-dev",

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory=$false)]
    [switch]$SkipTerraform,

    [Parameter(Mandatory=$false)]
    [switch]$SkipRestart,

    [Parameter(Mandatory=$false)]
    [string]$GitHubRepo = "",

    [Parameter(Mandatory=$false)]
    [string]$GitHubBranch = "master"
)

$ErrorActionPreference = "Stop"

# Color output functions
function Write-Step {
    param([string]$Message)
    Write-Host "`n===> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "  --> $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "  [ERROR] $Message" -ForegroundColor Red
}

# Verify Azure CLI is logged in
Write-Step "Verifying Azure CLI authentication..."
try {
    $account = az account show | ConvertFrom-Json
    Write-Success "Logged in as $($account.user.name)"
    Write-Info "Subscription: $($account.name)"
}
catch {
    Write-ErrorMsg "Not logged in to Azure. Please run: az login"
    exit 1
}

# Build container images
if (-not $SkipBuild) {
    Write-Step "Building container images in Azure ACR..."

    # Define services - using absolute paths to avoid Start-Job working directory issues
    $rootPath = $PWD.Path
    $services = @(
        @{ Name = "api-gateway"; Context = "$rootPath\src\services"; Dockerfile = "ApiGateway/Dockerfile" },
        @{ Name = "prospect-service"; Context = "$rootPath\src\services"; Dockerfile = "ProspectService/Dockerfile" },
        @{ Name = "student-service"; Context = "$rootPath\src\services"; Dockerfile = "StudentService/Dockerfile" },
        @{ Name = "instructor-service"; Context = "$rootPath\src\services"; Dockerfile = "InstructorService/Dockerfile" },
        @{ Name = "event-relay"; Context = "$rootPath\src\services"; Dockerfile = "EventRelay/Dockerfile" },
        @{ Name = "projection-service"; Context = "$rootPath\src\services"; Dockerfile = "ProjectionService/Dockerfile" },
        @{ Name = "frontend"; Context = "$rootPath\src\frontend"; Dockerfile = "Dockerfile" }
    )

    $buildJobs = @()

    foreach ($service in $services) {
        Write-Info "Starting build for $($service.Name)..."

        $job = Start-Job -ScriptBlock {
            param($Registry, $ImageName, $Context, $DockerfilePath, $GitRepo, $GitBranch)

            if ($GitRepo) {
                # Build from GitHub repository
                az acr build --registry $Registry `
                    --image "${ImageName}:latest" `
                    -f "$Context/$DockerfilePath" `
                    "${GitRepo}#${GitBranch}" 2>&1
            }
            else {
                # Build from local context
                Set-Location $Context
                $validDockerfile = $DockerfilePath -replace '/','\'

                az acr build --registry $Registry `
                    --image "${ImageName}:latest" `
                    -f $validDockerfile `
                    . 2>&1
            }
        } -ArgumentList $RegistryName, $service.Name, $service.Context, $service.Dockerfile, $GitHubRepo, $GitHubBranch

        $buildJobs += @{Job = $job; Name = $service.Name}
    }

    Write-Info "Waiting for builds to complete (this may take several minutes)..."

    foreach ($item in $buildJobs) {
        $job = $item.Job
        $name = $item.Name

        Wait-Job $job | Out-Null
        $output = Receive-Job $job

        if ($job.State -eq "Completed" -and $output -match "Succeeded") {
            Write-Success "$name built successfully"
        }
        else {
            Write-ErrorMsg "$name build failed"
            $output | Write-Host
            exit 1
        }

        Remove-Job $job
    }
}
else {
    Write-Info "Skipping build (using existing images)"
}

# Apply Terraform changes
if (-not $SkipTerraform) {
    Write-Step "Applying Terraform infrastructure changes..."

    Push-Location infrastructure

    try {
        Write-Info "Running terraform plan..."
        terraform plan -out=tfplan

        if ($LASTEXITCODE -ne 0) {
            throw "Terraform plan failed"
        }

        Write-Info "Applying changes..."
        terraform apply tfplan

        if ($LASTEXITCODE -ne 0) {
            throw "Terraform apply failed"
        }

        Write-Success "Infrastructure updated successfully"
    }
    catch {
        Write-ErrorMsg "Terraform failed: $_"
        Pop-Location
        exit 1
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Info "Skipping Terraform (infrastructure unchanged)"
}

# Restart container apps
if (-not $SkipRestart) {
    Write-Step "Restarting container apps with new images..."

    $apps = @(
        "ca-events-api-gateway-dev",
        "ca-events-prospect-service-dev",
        "ca-events-student-service-dev",
        "ca-events-instructor-service-dev",
        "ca-events-projection-service-dev",
        "ca-events-event-relay-dev",
        "ca-events-frontend-dev"
    )

    foreach ($app in $apps) {
        Write-Info "Restarting $app..."

        try {
            $revision = az containerapp revision list `
                --name $app `
                --resource-group $ResourceGroup `
                --query "[0].name" -o tsv

            if ($revision) {
                az containerapp revision restart `
                    --name $app `
                    --resource-group $ResourceGroup `
                    --revision $revision | Out-Null

                Write-Success "$app restarted"
            }
        }
        catch {
            Write-ErrorMsg "Failed to restart $app (may not exist yet)"
        }
    }
}
else {
    Write-Info "Skipping restart (apps will pick up changes on next deployment)"
}

# Verify deployment
Write-Step "Verifying deployment..."

$status = az containerapp list `
    --resource-group $ResourceGroup `
    --query "[].{Name:name, Status:properties.runningStatus}" `
    -o json | ConvertFrom-Json

Write-Host ""
Write-Host "Container App Status:" -ForegroundColor Cyan
foreach ($app in $status) {
    if ($app.Status -eq "Running") {
        Write-Host "  [OK] $($app.Name): " -NoNewline -ForegroundColor Green
        Write-Host $app.Status -ForegroundColor White
    }
    else {
        Write-Host "  [ERROR] $($app.Name): " -NoNewline -ForegroundColor Red
        Write-Host $app.Status -ForegroundColor White
    }
}

# Get URLs
Write-Step "Deployment URLs..."

$outputs = terraform output -json -state="infrastructure/terraform.tfstate" | ConvertFrom-Json

Write-Host ""
Write-Host "  Frontend:    $($outputs.frontend_url.value)" -ForegroundColor Green
Write-Host "  API Gateway: $($outputs.api_gateway_url.value)" -ForegroundColor Green
Write-Host ""

Write-Success "Deployment complete!"
Write-Host ""
