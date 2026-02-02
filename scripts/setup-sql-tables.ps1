# SQL Database Setup Script for Events Project
# This script creates the Students and Instructors tables for the transactional database.
# Usage: Run with PowerShell and the SqlServer module installed.

# --- Students Table ---
$studentsTable = @"
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
"@

# --- Instructors Table ---
$instructorsTable = @"
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

# --- Connection Info ---
$server = "sql-events-dev.database.windows.net"
$database = "db-events-transactional-dev"
$username = "sqladmin"
$password = "P@ssw0rd123!SecureEventsDb"

# --- Execute SQL ---
Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $username -Password $password -Query $studentsTable
Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $username -Password $password -Query $instructorsTable

Write-Host "Students and Instructors tables created."
