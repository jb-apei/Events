#!/usr/bin/env pwsh
# Build and push Docker images to Azure Container Registry using cloud build

param(
    [Parameter(Mandatory=$true)]
    [string]$RegistryName,

    [Parameter(Mandatory=$false)]
    [string]$Version = "latest",

    [Parameter(Mandatory=$false)]
    [switch]$Sequential = $false
)

$ErrorActionPreference = "Continue"

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
Write-Host "Mode: $(if ($Sequential) { 'Sequential' } else { 'Parallel (faster)' })" -ForegroundColor Yellow
Write-Host ""

if ($Sequential) {
    # Sequential build (original behavior) - slower but useful for debugging
    foreach ($service in $services) {
        $imageName = "$($service.Name):${Version}"

        Write-Host "Building $($service.Name)..." -ForegroundColor Green
        Write-Host "  Image: $imageName" -ForegroundColor Gray

        try {
            $contextPath = $service.Context -replace '/','\'
            $dockerfilePath = "$($service.Dockerfile)" -replace '/','\'

            az acr build `
                --registry $RegistryName `
                --image $imageName `
                -f $dockerfilePath `
                $contextPath

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
} else {
    # Parallel build (new behavior) - faster, builds all images concurrently
    Write-Host "Submitting build jobs to ACR (running in parallel)..." -ForegroundColor Cyan
    Write-Host ""

    $buildJobs = @()

    foreach ($service in $services) {
        $imageName = "$($service.Name):${Version}"

        Write-Host "Queuing $($service.Name)..." -ForegroundColor Green

        try {
            # Temporarily relax error action to prevent warnings on stderr from triggering catch
            $oldRef = $ErrorActionPreference
            $ErrorActionPreference = "Continue"

            # Submit the build without waiting (--no-wait flag)
            $contextPath = $service.Context -replace '/','\'
            $dockerfilePath = "$($service.Dockerfile)" -replace '/','\'

            $output = az acr build `
                --registry $RegistryName `
                --image $imageName `
                -f $dockerfilePath `
                --no-wait `
                $contextPath 2>&1

            $ErrorActionPreference = $oldRef

            # Extract build ID from output (format: "Build ID: {id}")
            if ($output -match "Build ID:\s*(\S+)") {
                $buildId = $Matches[1]
                $buildJobs += @{ Service = $service.Name; BuildId = $buildId }
                Write-Host "  Queued with Build ID: $buildId" -ForegroundColor Gray
            } else {
                Write-Host "  Warning: Could not extract Build ID from output" -ForegroundColor Yellow
                Write-Host "  Output: $output" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "  Error: $_" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
    Write-Host "All build jobs submitted! Monitoring progress..." -ForegroundColor Cyan
    Write-Host ""

    # Monitor all builds in parallel
    $completedJobs = @()
    $failedJobs = @()
    $lastStatusUpdate = [DateTime]::MinValue

    while ($buildJobs.Count -gt 0) {
        # Print status update every 10 seconds
        if ((Get-Date) -gt $lastStatusUpdate.AddSeconds(10)) {
            $running = $buildJobs | ForEach-Object { $_.Service }
            Write-Host "[$((Get-Date).ToString("HH:mm:ss"))] Still building: $($running -join ', ')" -ForegroundColor Gray
            $lastStatusUpdate = Get-Date
        }

        foreach ($job in $buildJobs) {
            try {
                # Check status using 'az acr run show' which is lighter than fetching logs
                $status = az acr run show --run-id $job.BuildId --registry $RegistryName --query status -o tsv 2>$null

                if ($status -in "Succeeded", "Failed", "Canceled", "Error", "Timeout") {
                    Write-Host "$($job.Service): $status" -ForegroundColor $(if ($status -eq "Succeeded") { "Green" } else { "Red" })

                    if ($status -eq "Succeeded") {
                        $completedJobs += $job.Service
                    } else {
                        $failedJobs += $job.Service
                        # If failed, fetch the logs to show why
                        Write-Host "Fetching logs for failed build $($job.Service)..." -ForegroundColor Yellow
                        az acr item logs --name $job.BuildId --registry $RegistryName | Select-Object -Last 20
                    }

                    # Remove from pending jobs
                    $buildJobs = $buildJobs | Where-Object { $_.Service -ne $job.Service }
                }
            }
            catch {
                # Ignore transient errors checking status
            }
        }

        if ($buildJobs.Count -gt 0) {
            Start-Sleep -Seconds 5
        }
    }

    Write-Host ""

    if ($failedJobs.Count -gt 0) {
        Write-Host "Build failed for: $($failedJobs -join ', ')" -ForegroundColor Red
        exit 1
    }

    if ($completedJobs.Count -eq $services.Count) {
        Write-Host "All builds completed successfully!" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Images available in $RegistryName.azurecr.io:" -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "  - $RegistryName.azurecr.io/$($service.Name):$Version" -ForegroundColor White
}
