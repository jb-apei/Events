# SQL Database Setup Script for Events Project
# This script creates the Students and Instructors tables for the transactional database.
# Usage: Run with PowerShell and the SqlServer module installed.

# --- Students Table ---
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

# --- Prospects Table ---
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

# --- Instructors Table ---
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
"@

# --- Outbox Table ---
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

# --- Inbox Table ---
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

# --- Connection Info ---
param(
    [Parameter(Mandatory=$false)]
    [string]$Server = "sql-events-dev.database.windows.net",

    [Parameter(Mandatory=$false)]
    [string]$Database = "db-events-transactional-dev",

    [Parameter(Mandatory=$false)]
    [string]$Username = "sqladmin",

    [Parameter(Mandatory=$false)]
    [string]$Password = "P@ssw0rd123!SecureEventsDb"
)

# --- Execute SQL ---
Write-Host "Creating Tables in $Database on $Server..." -ForegroundColor Cyan

try {
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $studentsTable -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $prospectsTable -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $instructorsTable -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $outboxTable -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $Username -Password $Password -Query $inboxTable -ErrorAction Stop
    Write-Host "[OK] Tables ensured." -ForegroundColor Green
} catch {
    Write-Error "Failed to execute SQL: $_"
    exit 1
}

Write-Host "Students and Instructors tables created."
