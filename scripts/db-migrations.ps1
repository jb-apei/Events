#!/usr/bin/env pwsh
Set-Location "$PSScriptRoot/.."
<#
.SYNOPSIS
    Manage EF Core migrations for all services.
#>

$ErrorActionPreference = "Stop"

function Manage-Migrations {
    param($Service, $ProjectName)

    Write-Host "Checking $Service..." -ForegroundColor Cyan
    $projectPath = "src/services/$Service/$ProjectName.csproj"

    # Check if Migrations folder exists
    if (-not (Test-Path "src/services/$Service/Migrations")) {
        Write-Host "  No migrations found. Generating InitialCreate..." -ForegroundColor Yellow

        # Set dummy connection string to force SQL provider (required for dotnet ef to pick SQL provider over InMemory)
        $env:ConnectionStrings__StudentDb="Server=localhost;Database=Dummy;User Id=sa;Password=Password123!;TrustServerCertificate=True"
        $env:ConnectionStrings__InstructorDb="Server=localhost;Database=Dummy;User Id=sa;Password=Password123!;TrustServerCertificate=True"
        $env:ConnectionStrings__ProspectDb="Server=localhost;Database=Dummy;User Id=sa;Password=Password123!;TrustServerCertificate=True"

        try {
            dotnet ef migrations add InitialCreate --project $projectPath --startup-project $projectPath --output-dir Migrations

            if ($LASTEXITCODE -eq 0) {
                Write-Host "  [OK] Migration generated." -ForegroundColor Green
            } else {
                Write-Host "  [ERROR] Failed to generate migration." -ForegroundColor Red
            }
        } catch {
             Write-Host "  [ERROR] Failed to run dotnet ef. Is it installed?" -ForegroundColor Red
        }
    } else {
        Write-Host "  [OK] Migrations folder exists." -ForegroundColor Green
    }
}

Write-Host "==> Checking Database Migrations" -ForegroundColor Cyan

Manage-Migrations "StudentService" "StudentService"
Manage-Migrations "InstructorService" "InstructorService"
Manage-Migrations "ProspectService" "ProspectService"
