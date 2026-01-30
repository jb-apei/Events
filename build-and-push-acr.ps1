#!/usr/bin/env pwsh
# Build and push Docker images to Azure Container Registry using cloud build

param(
    [Parameter(Mandatory=$true)]
    [string]$RegistryName,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "latest"
)

$ErrorActionPreference = "Stop"

# Define services and their build contexts
$services = @(
    @{ Name = "api-gateway"; Context = "src/services"; Dockerfile = "ApiGateway/Dockerfile" },
    @{ Name = "prospect-service"; Context = "src/services"; Dockerfile = "ProspectService/Dockerfile" },
    @{ Name = "student-service"; Context = "src/services"; Dockerfile = "StudentService/Dockerfile" },
    @{ Name = "instructor-service"; Context = "src/services"; Dockerfile = "InstructorService/Dockerfile" },
    @{ Name = "event-relay"; Context = "src/services"; Dockerfile = "EventRelay/Dockerfile" },
    @{ Name = "projection-service"; Context = "src/services"; Dockerfile = "ProjectionService/Dockerfile" },
    @{ Name = "frontend"; Context = "src/frontend"; Dockerfile = "Dockerfile" }
)

Write-Host "Building Docker images in Azure Container Registry..." -ForegroundColor Cyan
Write-Host "Registry: $RegistryName" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host ""

foreach ($service in $services) {
    $imageName = "$($service.Name):${Version}"
    
    Write-Host "Building $($service.Name)..." -ForegroundColor Green
    Write-Host "  Image: $imageName" -ForegroundColor Gray
    
    try {
        # Build the image in ACR (cloud build)
        az acr build `
            --registry $RegistryName `
            --image $imageName `
            -f "$($service.Context)/$($service.Dockerfile)" `
            $service.Context
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build $($service.Name)"
        }
        
        Write-Host "  Built successfully" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "  Error: $_" -ForegroundColor Red
        Write-Host ""
        exit 1
    }
}

Write-Host "All images built successfully in ACR!" -ForegroundColor Green
Write-Host ""
Write-Host "Images available in $RegistryName.azurecr.io:" -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "  - $RegistryName.azurecr.io/$($service.Name):$Version" -ForegroundColor White
}
