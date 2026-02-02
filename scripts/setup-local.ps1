#Requires -Version 7.0
<#
.SYNOPSIS
    Automated local development environment setup for Events project.

.DESCRIPTION
    This script sets up the complete local development environment including:
    - Docker containers (Azurite, SQL Server)
    - Database migrations
    - Configuration validation

.PARAMETER SkipDocker
    Skip Docker container setup (useful if containers are already running)

.PARAMETER SkipMigrations
    Skip database migrations

.EXAMPLE
    .\setup-local.ps1

.EXAMPLE
    .\setup-local.ps1 -SkipDocker
#>

param(
    [switch]$SkipDocker,
    [switch]$SkipMigrations
)

Set-Location "$PSScriptRoot/.."

$ErrorActionPreference = "Stop"

Write-Host "🚀 Events Project - Local Development Setup" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
function Test-Prerequisites {
    Write-Host "📋 Checking prerequisites..." -ForegroundColor Yellow

    $errors = @()

    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    } catch {
        $errors += ".NET 8.0 SDK not found"
    }

    # Check Docker
    if (-not $SkipDocker) {
        try {
            $dockerVersion = docker --version
            Write-Host "  ✅ Docker: $dockerVersion" -ForegroundColor Green

            # Check if Docker is running
            docker ps | Out-Null
            if ($LASTEXITCODE -ne 0) {
                $errors += "Docker is installed but not running. Please start Docker Desktop."
            }
        } catch {
            $errors += "Docker Desktop not installed or not in PATH"
        }
    }

    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $errors += "PowerShell 7+ required (current: $($PSVersionTable.PSVersion))"
    } else {
        Write-Host "  ✅ PowerShell: $($PSVersionTable.PSVersion)" -ForegroundColor Green
    }

    if ($errors.Count -gt 0) {
        Write-Host ""
        Write-Host "❌ Prerequisites check failed:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }

    Write-Host ""
}

# Start Docker containers
function Start-Containers {
    Write-Host "🐳 Starting Docker containers..." -ForegroundColor Yellow

    # Start Azurite (Azure Storage Emulator)
    Write-Host "  Starting Azurite..." -ForegroundColor Gray
    $azuriteRunning = docker ps --filter "name=azurite" --filter "status=running" --format "{{.Names}}"

    if ($azuriteRunning) {
        Write-Host "  ✅ Azurite already running" -ForegroundColor Green
    } else {
        docker run -d `
            --name azurite `
            -p 10000:10000 `
            -p 10001:10001 `
            -p 10002:10002 `
            mcr.microsoft.com/azure-storage/azurite

        Write-Host "  ✅ Azurite started on ports 10000-10002" -ForegroundColor Green
    }

    # Start SQL Server
    Write-Host "  Starting SQL Server..." -ForegroundColor Gray
    $sqlRunning = docker ps --filter "name=sqlserver" --filter "status=running" --format "{{.Names}}"

    if ($sqlRunning) {
        Write-Host "  ✅ SQL Server already running" -ForegroundColor Green
    } else {
        docker run -d `
            --name sqlserver `
            -e "ACCEPT_EULA=Y" `
            -e "SA_PASSWORD=YourStrong!Passw0rd" `
            -p 1433:1433 `
            mcr.microsoft.com/mssql/server:2022-latest

        Write-Host "  ✅ SQL Server started on port 1433" -ForegroundColor Green
        Write-Host "     Waiting for SQL Server to be ready..." -ForegroundColor Gray
        Start-Sleep -Seconds 10
    }

    Write-Host ""
}

# Apply database migrations
function Apply-Migrations {
    Write-Host "🗄️  Applying database migrations..." -ForegroundColor Yellow

    $services = @(
        "ProspectService",
        "StudentService",
        "InstructorService",
        "ProjectionService"
    )

    foreach ($service in $services) {
        $servicePath = "src/services/$service"

        if (Test-Path $servicePath) {
            Write-Host "  Migrating $service..." -ForegroundColor Gray

            try {
                Push-Location $servicePath
                dotnet ef database update 2>&1 | Out-Null

                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  ✅ $service migrations applied" -ForegroundColor Green
                } else {
                    Write-Host "  ⚠️  $service migrations skipped (may not have migrations)" -ForegroundColor Yellow
                }
            } finally {
                Pop-Location
            }
        }
    }

    Write-Host ""
}

# Display connection information
function Show-ConnectionInfo {
    Write-Host "🔗 Connection Information" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "Azurite (Azure Storage Emulator):" -ForegroundColor White
    Write-Host "  Connection String: UseDevelopmentStorage=true" -ForegroundColor Gray
    Write-Host "  Blob Storage: http://127.0.0.1:10000" -ForegroundColor Gray
    Write-Host "  Queue Storage: http://127.0.0.1:10001" -ForegroundColor Gray
    Write-Host "  Table Storage: http://127.0.0.1:10002" -ForegroundColor Gray
    Write-Host ""

    Write-Host "SQL Server:" -ForegroundColor White
    Write-Host "  Connection String: Server=localhost,1433;Database=EventsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True" -ForegroundColor Gray
    Write-Host "  Server: localhost,1433" -ForegroundColor Gray
    Write-Host "  Username: sa" -ForegroundColor Gray
    Write-Host "  Password: YourStrong!Passw0rd" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Next Steps:" -ForegroundColor White
    Write-Host "  1. Run: .\setup-user-secrets.ps1" -ForegroundColor Gray
    Write-Host "  2. Start services individually:" -ForegroundColor Gray
    Write-Host "     cd src/services/ApiGateway && dotnet run" -ForegroundColor Gray
    Write-Host "     cd src/services/ProspectService && dotnet run" -ForegroundColor Gray
    Write-Host "  3. Or use VS Code debug configurations" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Useful Commands:" -ForegroundColor White
    Write-Host "  docker ps              # List running containers" -ForegroundColor Gray
    Write-Host "  docker logs azurite    # View Azurite logs" -ForegroundColor Gray
    Write-Host "  docker logs sqlserver  # View SQL Server logs" -ForegroundColor Gray
    Write-Host "  docker stop azurite sqlserver  # Stop containers" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
try {
    Test-Prerequisites

    if (-not $SkipDocker) {
        Start-Containers
    } else {
        Write-Host "⏭️  Skipping Docker setup" -ForegroundColor Yellow
        Write-Host ""
    }

    if (-not $SkipMigrations) {
        Apply-Migrations
    } else {
        Write-Host "⏭️  Skipping database migrations" -ForegroundColor Yellow
        Write-Host ""
    }

    Show-ConnectionInfo

    Write-Host "✅ Local development environment setup complete!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "❌ Setup failed: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
