<#
.SYNOPSIS
    Sets up SQL tables for the Events project.
.DESCRIPTION
    Creates necessary tables for either the Transactional (Write) or ReadModel datbase.
    Handles idempotent creation (checks for existence).
#>
param(
    [Parameter(Mandatory=$false)]
    [string]$Server = "sql-events-dev.database.windows.net",

    [Parameter(Mandatory=$false)]
    [string]$Database = "db-events-transactional-dev",

    [Parameter(Mandatory=$false)]
    [string]$Username = "sqladmin",

    [Parameter(Mandatory=$false)]
    [string]$Password = "P@ssw0rd123!SecureEventsDb",

    [Parameter(Mandatory=$false)]
    [ValidateSet("Transactional", "ReadModel")]
    [string]$Mode = "Transactional"
)

$ErrorActionPreference = "Stop"

# --- DEFINITIONS ---

# 1. Students (Transactional)
$studentsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Students' and xtype='U')
BEGIN
    CREATE TABLE Students (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(50) NULL,
        StudentNumber NVARCHAR(50) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        EnrollmentDate DATETIME2 NOT NULL,
        ExpectedGraduationDate DATETIME2 NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
"@

# 2. Prospects (Transactional)
$prospectsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Prospects' and xtype='U')
BEGIN
    CREATE TABLE Prospects (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Source NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'New',
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
"@

# 3. Instructors (Transactional)
$instructorsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Instructors' and xtype='U')
BEGIN
    CREATE TABLE Instructors (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(50) NULL,
        EmployeeNumber NVARCHAR(50) NOT NULL,
        Specialization NVARCHAR(100) NULL,
        HireDate DATETIME2 NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
"@

# 4. Outbox (Transactional) - for reliability
$outboxTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Outbox' and xtype='U')
BEGIN
    CREATE TABLE Outbox (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventId UNIQUEIDENTIFIER NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        Published BIT NOT NULL DEFAULT 0,
        PublishedAt DATETIME2 NULL
    );
END
"@

# 5. Inbox (Both) - for idempotency
$inboxTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Inbox' and xtype='U')
BEGIN
    CREATE TABLE Inbox (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventId UNIQUEIDENTIFIER NOT NULL,
        ProcessedAt DATETIME2 NOT NULL
    );
END
"@

# 6. ProspectSummary (ReadModel)
$prospectSummaryTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProspectSummary' and xtype='U')
BEGIN
    CREATE TABLE ProspectSummary (
        ProspectId INT PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Status NVARCHAR(20) NOT NULL,
        Source NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
"@

# --- EXECUTION ---

Write-Host "Connecting to $Server ($Database) in mode: $Mode..." -ForegroundColor Cyan

function Run-Sql {
    param([string]$Query, [string]$Name)
    Write-Host "  -> Ensuring $Name table..." -NoNewline
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $Query -ErrorAction Stop
    Write-Host " [OK]" -ForegroundColor Green
}

try {
    if ($Mode -eq "Transactional") {
        Run-Sql -Query $studentsTable -Name "Students"
        Run-Sql -Query $prospectsTable -Name "Prospects"
        Run-Sql -Query $instructorsTable -Name "Instructors"
        Run-Sql -Query $outboxTable -Name "Outbox"
        Run-Sql -Query $inboxTable -Name "Inbox"
    }
    elseif ($Mode -eq "ReadModel") {
        Run-Sql -Query $prospectSummaryTable -Name "ProspectSummary"
        Run-Sql -Query $inboxTable -Name "Inbox"
    }
} catch {
    Write-Error "Failed to execute SQL: $_"
    exit 1
}

Write-Host "Database setup complete." -ForegroundColor Green
