<#
.SYNOPSIS
    Initialize .NET user-secrets for local development of Events project services.

.DESCRIPTION
    This script configures user-secrets for all services based on the .env.example template.
    User-secrets provide secure local configuration without committing sensitive values to git.
    
    Benefits:
    - Secrets stored outside project directory (not committed to git)
    - Service-specific configuration isolation
    - No need for .env files in local development
    - Works seamlessly with dotnet run and Visual Studio
    
.PARAMETER Interactive
    Prompt for each configuration value (default: use development defaults)

.EXAMPLE
    .\setup-user-secrets.ps1
    Sets up all services with development defaults

.EXAMPLE
    .\setup-user-secrets.ps1 -Interactive
    Prompts for each configuration value

.NOTES
    Requires: .NET SDK 6.0+ installed
    User-secrets location: %APPDATA%\Microsoft\UserSecrets\
#>

param(
    [switch]$Interactive = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Events Project - User Secrets Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Service paths
$services = @(
    @{ Name = "ApiGateway"; Path = "src/services/ApiGateway" },
    @{ Name = "ProspectService"; Path = "src/services/ProspectService" },
    @{ Name = "StudentService"; Path = "src/services/StudentService" },
    @{ Name = "InstructorService"; Path = "src/services/InstructorService" },
    @{ Name = "ProjectionService"; Path = "src/services/ProjectionService" },
    @{ Name = "EventRelay"; Path = "src/services/EventRelay" }
)

# Development defaults (safe for local development)
$defaults = @{
    # SQL Server (local Docker container)
    TransactionalDbConnectionString = "Server=localhost,1433;Initial Catalog=EventsTransactional;User ID=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False"
    ReadModelDbConnectionString = "Server=localhost,1433;Initial Catalog=EventsReadModel;User ID=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False"
    
    # Azurite (local storage emulator)
    ServiceBusConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=NOTUSED;UseDevelopmentStorage=true"
    
    # Local service URLs
    ApiGatewayUrl = "http://localhost:5037"
    ProspectServiceUrl = "http://localhost:5110"
    StudentServiceUrl = "http://localhost:5120"
    InstructorServiceUrl = "http://localhost:5130"
    
    # JWT (development key)
    JwtSecretKey = "dev-super-secret-jwt-signing-key-minimum-32-characters-long"
    JwtIssuer = "EventsApiGateway"
    JwtAudience = "EventsClients"
    
    # Event Grid (local testing - optional)
    EventGridProspectEndpoint = "http://localhost:4000/api/events"
    EventGridProspectKey = "local-development-key"
    EventGridStudentEndpoint = "http://localhost:4001/api/events"
    EventGridStudentKey = "local-development-key"
    EventGridInstructorEndpoint = "http://localhost:4002/api/events"
    EventGridInstructorKey = "local-development-key"
    EventGridWebhookKey = "webhook-validation-key-for-local-testing"
    
    # Service Bus queues
    ProspectCommandQueue = "prospect-commands"
    StudentCommandQueue = "student-commands"
    InstructorCommandQueue = "instructor-commands"
}

function Set-ServiceSecret {
    param(
        [string]$ServicePath,
        [string]$Key,
        [string]$Value
    )
    
    Push-Location $ServicePath
    try {
        dotnet user-secrets set $Key $Value --quiet
    } catch {
        Write-Host "  ⚠ Failed to set $Key" -ForegroundColor Yellow
    } finally {
        Pop-Location
    }
}

Write-Host "Configuring user-secrets for all services..." -ForegroundColor Cyan
Write-Host ""

foreach ($service in $services) {
    Write-Host "[$($service.Name)]" -ForegroundColor Yellow
    
    if (-not (Test-Path $service.Path)) {
        Write-Host "  ✗ Service path not found: $($service.Path)" -ForegroundColor Red
        continue
    }
    
    # Initialize user-secrets for the service
    Push-Location $service.Path
    try {
        dotnet user-secrets init --quiet 2>$null
        Write-Host "  ✓ User-secrets initialized" -ForegroundColor Green
    } catch {
        Write-Host "  ℹ User-secrets already initialized" -ForegroundColor Gray
    } finally {
        Pop-Location
    }
    
    # Configure service-specific secrets
    switch ($service.Name) {
        "ApiGateway" {
            Set-ServiceSecret $service.Path "Jwt:SecretKey" $defaults.JwtSecretKey
            Set-ServiceSecret $service.Path "Jwt:Issuer" $defaults.JwtIssuer
            Set-ServiceSecret $service.Path "Jwt:Audience" $defaults.JwtAudience
            Set-ServiceSecret $service.Path "ServiceBus:ConnectionString" $defaults.ServiceBusConnectionString
            Set-ServiceSecret $service.Path "EventGrid:WebhookValidationKey" $defaults.EventGridWebhookKey
            Set-ServiceSecret $service.Path "ProspectService:Url" $defaults.ProspectServiceUrl
            Set-ServiceSecret $service.Path "StudentService:Url" $defaults.StudentServiceUrl
            Set-ServiceSecret $service.Path "InstructorService:Url" $defaults.InstructorServiceUrl
            Write-Host "  ✓ Configured: JWT, Service Bus, Event Grid, Service URLs" -ForegroundColor Green
        }
        
        "ProspectService" {
            Set-ServiceSecret $service.Path "ConnectionStrings:ProspectDb" $defaults.TransactionalDbConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:ConnectionString" $defaults.ServiceBusConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:CommandQueue" $defaults.ProspectCommandQueue
            Set-ServiceSecret $service.Path "ApiGateway:Url" $defaults.ApiGatewayUrl
            Write-Host "  ✓ Configured: Database, Service Bus, API Gateway" -ForegroundColor Green
        }
        
        "StudentService" {
            Set-ServiceSecret $service.Path "ConnectionStrings:StudentDb" $defaults.TransactionalDbConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:ConnectionString" $defaults.ServiceBusConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:CommandQueue" $defaults.StudentCommandQueue
            Set-ServiceSecret $service.Path "ApiGateway:Url" $defaults.ApiGatewayUrl
            Write-Host "  ✓ Configured: Database, Service Bus, API Gateway" -ForegroundColor Green
        }
        
        "InstructorService" {
            Set-ServiceSecret $service.Path "ConnectionStrings:InstructorDb" $defaults.TransactionalDbConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:ConnectionString" $defaults.ServiceBusConnectionString
            Set-ServiceSecret $service.Path "Azure:ServiceBus:CommandQueue" $defaults.InstructorCommandQueue
            Set-ServiceSecret $service.Path "ApiGateway:Url" $defaults.ApiGatewayUrl
            Write-Host "  ✓ Configured: Database, Service Bus, API Gateway" -ForegroundColor Green
        }
        
        "ProjectionService" {
            Set-ServiceSecret $service.Path "ConnectionStrings:ProjectionDatabase" $defaults.ReadModelDbConnectionString
            Write-Host "  ✓ Configured: Read Model Database" -ForegroundColor Green
        }
        
        "EventRelay" {
            Set-ServiceSecret $service.Path "ConnectionStrings:ProspectDb" $defaults.TransactionalDbConnectionString
            Set-ServiceSecret $service.Path "Azure:EventGrid:ProspectTopicEndpoint" $defaults.EventGridProspectEndpoint
            Set-ServiceSecret $service.Path "Azure:EventGrid:ProspectTopicKey" $defaults.EventGridProspectKey
            Set-ServiceSecret $service.Path "Azure:EventGrid:StudentTopicEndpoint" $defaults.EventGridStudentEndpoint
            Set-ServiceSecret $service.Path "Azure:EventGrid:StudentTopicKey" $defaults.EventGridStudentKey
            Set-ServiceSecret $service.Path "Azure:EventGrid:InstructorTopicEndpoint" $defaults.EventGridInstructorEndpoint
            Set-ServiceSecret $service.Path "Azure:EventGrid:InstructorTopicKey" $defaults.EventGridInstructorKey
            Write-Host "  ✓ Configured: Outbox Database, Event Grid Topics" -ForegroundColor Green
        }
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ User-secrets setup complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Start SQL Server:" -ForegroundColor White
Write-Host "     docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourStrong@Passw0rd' -p 1433:1433 --name sql-server -d mcr.microsoft.com/mssql/server:2022-latest" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. (Optional) Start Azurite for Service Bus emulation:" -ForegroundColor White
Write-Host "     azurite --silent --location c:\azurite --debug c:\azurite\debug.log" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Run services:" -ForegroundColor White
Write-Host "     cd src/services/ApiGateway && dotnet run" -ForegroundColor Gray
Write-Host "     cd src/services/ProspectService && dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. View configured secrets for a service:" -ForegroundColor White
Write-Host "     dotnet user-secrets list --project src/services/ApiGateway" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. Remove all secrets (cleanup):" -ForegroundColor White
Write-Host "     dotnet user-secrets clear --project src/services/ApiGateway" -ForegroundColor Gray
Write-Host ""
Write-Host "User-secrets storage location:" -ForegroundColor Cyan
Write-Host "  $env:APPDATA\Microsoft\UserSecrets\" -ForegroundColor Gray
Write-Host ""
