# Services Startup Guide

## Quick Start - All Services

```powershell
# Start all backend services in order
cd c:\Users\jbouchard\Documents\Projects\Events\src\services

# 1. ProspectService (port 5110)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ProspectService; dotnet run" -WindowStyle Normal

# 2. StudentService (port 5120)  
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd StudentService; dotnet run" -WindowStyle Normal

# 3. InstructorService (port 5130)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd InstructorService; dotnet run" -WindowStyle Normal

# 4. ApiGateway (port 5037)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ApiGateway; dotnet run" -WindowStyle Normal

# 5. Frontend (port 3000)
cd ..\frontend
npm run dev
```

## Service Overview

| Service | Port | Swagger | Purpose |
|---------|------|---------|---------|
| **ProspectService** | 5110 | http://localhost:5110/swagger | Manage potential students |
| **StudentService** | 5120 | http://localhost:5120/swagger | Manage enrolled students |
| **InstructorService** | 5130 | http://localhost:5130/swagger | Manage instructors |
| **ApiGateway** | 5037 | http://localhost:5037/swagger | API Gateway + WebSocket hub |
| **Frontend** | 3000 | http://localhost:3000 | React UI |

## Testing

### Test ProspectService
```powershell
# Login
$login = @{ email = "test@test.com"; password = "test123" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5037/api/auth/login" -Method Post -Body $login -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# Create Prospect
$prospect = @{ 
    firstName = "John"
    lastName = "Doe" 
    email = "john.doe@test.com"
    phone = "555-1234"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5037/api/prospects" -Method Post -Body $prospect -ContentType "application/json" -Headers $headers

# Get all Prospects
Invoke-RestMethod -Uri "http://localhost:5037/api/prospects" -Headers $headers
```

### Test StudentService
```powershell
# Create Student
$student = @{
    firstName = "Jane"
    lastName = "Smith"
    email = "jane.smith@test.com"
    phone = "555-5678"
    studentNumber = "STU-2026-001"
    enrollmentDate = "2026-01-29T00:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5037/api/students" -Method Post -Body $student -ContentType "application/json" -Headers $headers

# Get all Students
Invoke-RestMethod -Uri "http://localhost:5037/api/students" -Headers $headers
```

### Test InstructorService
```powershell
# Create Instructor
$instructor = @{
    firstName = "Dr. Alice"
    lastName = "Johnson"
    email = "alice.j@test.com"
    phone = "555-9876"
    employeeNumber = "EMP-2026-001"
    specialization = "Computer Science"
    hireDate = "2026-01-15T00:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5037/api/instructors" -Method Post -Body $instructor -ContentType "application/json" -Headers $headers

# Get all Instructors
Invoke-RestMethod -Uri "http://localhost:5037/api/instructors" -Headers $headers
```

## Event Flow

1. **Create Entity** → POST to ApiGateway → Forwards to service
2. **Service Saves** → Database + Outbox (transaction)
3. **Event Push** → HTTP POST to ApiGateway webhook (CloudEvents format)
4. **ApiGateway Broadcasts** → WebSocket to all connected clients
5. **Frontend Receives** → Invalidates React Query cache → UI updates

## WebSocket Events

Auto-subscribed events:
- `ProspectCreated`, `ProspectUpdated`, `ProspectMerged`
- `StudentCreated`, `StudentUpdated`, `StudentChanged`
- `InstructorCreated`, `InstructorUpdated`, `InstructorDeactivated`

## Development Mode Features

All services run without Azure dependencies:
- ✅ In-memory databases (no connection strings needed)
- ✅ HTTP-based event flow (no Azure Event Grid needed)
- ✅ No Service Bus required
- ✅ WebSocket for real-time updates

## Architecture Summary

```
┌─────────────────┐
│  React Frontend │ (Port 3000)
│  WebSocket Sub │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   ApiGateway    │ (Port 5037)
│  - Auth (JWT)   │
│  - WebSocket    │
│  - Proxy        │
└────┬─────┬──────┘
     │     │     │
     ▼     ▼     ▼
┌─────┐ ┌─────┐ ┌──────┐
│5110 │ │5120 │ │ 5130 │
│Pros │ │Stud │ │Instr │
│pect │ │ent  │ │uctor │
└─────┘ └─────┘ └──────┘
  │       │       │
  └───────┴───────┘
          │
   HTTP Push Events
   to ApiGateway
```

## Stopping Services

```powershell
# Stop all services
Get-Process -Name "ApiGateway","ProspectService","StudentService","InstructorService","node" -ErrorAction SilentlyContinue | Stop-Process -Force
```

## Next Steps

The frontend UI needs to be updated to:
1. Add StudentPage and InstructorPage components
2. Create student and instructor API clients
3. Add routing for /students and /instructors
4. Update WebSocket handlers for student/instructor events

See [service-implementation-checklist.md](service-implementation-checklist.md) for frontend implementation patterns.
