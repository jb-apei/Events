#!/usr/bin/env pwsh
# Build and push Docker images to Docker Hub

param(
    [Parameter(Mandatory=$true)]
    [string]$DockerHubUsername,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "latest",
    
    [Parameter(Mandatory=$false)]
    [switch]$NoPush
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

Write-Host "Building Docker images for Events project..." -ForegroundColor Cyan
Write-Host "Docker Hub Username: $DockerHubUsername" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host ""

foreach ($service in $services) {
    $imageName = "$DockerHubUsername/events-$($service.Name)"
    $imageTag = "${imageName}:${Version}"
    
    Write-Host "Building $($service.Name)..." -ForegroundColor Green
    Write-Host "  Image: $imageTag" -ForegroundColor Gray
    
    try {
        # Build the image
        docker build `
            -t $imageTag `
            -f "$($service.Context)/$($service.Dockerfile)" `
            $service.Context
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build $($service.Name)"
        }
        
        # Also tag as latest
        docker tag $imageTag "${imageName}:latest"
        
        Write-Host "  Built successfully" -ForegroundColor Green
        
        # Push to Docker Hub if not skipped
        if (-not $NoPush) {
            Write-Host "  Pushing to Docker Hub..." -ForegroundColor Yellow
            
            docker push $imageTag
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to push $imageTag"
            }
            
            docker push "${imageName}:latest"
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to push ${imageName}:latest"
            }
            
            Write-Host "  Pushed successfully" -ForegroundColor Green
        }
        
        Write-Host ""
    }
    catch {
        Write-Host "  Error: $_" -ForegroundColor Red
        Write-Host ""
        exit 1
    }
}

Write-Host "All images built successfully!" -ForegroundColor Green

if (-not $NoPush) {
    Write-Host ""
    Write-Host "Images pushed to Docker Hub:" -ForegroundColor Cyan
    foreach ($service in $services) {
        Write-Host "  - $DockerHubUsername/events-$($service.Name):$Version" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "To run locally with Docker Compose:" -ForegroundColor Yellow
Write-Host "  docker-compose up -d" -ForegroundColor White
