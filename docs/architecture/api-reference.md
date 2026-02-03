# API Documentation - Swagger Endpoints

This document provides quick access to all Swagger/OpenAPI documentation for the Events microservices system.

## Service Overview

The system consists of 4 microservices:

| Service | Port | Purpose | Status |
|---------|------|---------|--------|
| **ApiGateway** | 5037 | API Gateway, Authentication, WebSocket Hub | ✅ Running |
| **ProspectService** | 5110 | Manage prospects (potential students) | ✅ Running |
| **StudentService** | 5120 | Manage enrolled students | ✅ Running |
| **InstructorService** | 5130 | Manage instructors | ✅ Running |

---

## 📚 Swagger UI Documentation

### ApiGateway
**Swagger UI**: [http://localhost:5037/swagger/index.html](http://localhost:5037/swagger/index.html)  
**OpenAPI JSON**: [http://localhost:5037/swagger/v1/swagger.json](http://localhost:5037/swagger/v1/swagger.json)

**Endpoints**:
- `POST /api/auth/login` - Authentication (get JWT token)
- `POST /api/events/webhook` - CloudEvents webhook (for event subscriptions)
- `GET /api/prospects` - Proxy to ProspectService
- `POST /api/prospects` - Proxy to ProspectService
- `PUT /api/prospects/{id}` - Proxy to ProspectService
- `GET /api/students` - Proxy to StudentService
- `POST /api/students` - Proxy to StudentService
- `PUT /api/students/{id}` - Proxy to StudentService
- `GET /api/instructors` - Proxy to InstructorService
- `POST /api/instructors` - Proxy to InstructorService
- `PUT /api/instructors/{id}` - Proxy to InstructorService
- **WebSocket**: `ws://localhost:5037/ws/events` - Real-time event subscriptions (Hub)

---

### ProspectService
**Swagger UI**: [http://localhost:5110/swagger/index.html](http://localhost:5110/swagger/index.html)  
**OpenAPI JSON**: [http://localhost:5110/swagger/v1/swagger.json](http://localhost:5110/swagger/v1/swagger.json)

**Endpoints**:
- `GET /api/prospects` - List all prospects
- `GET /api/prospects/{id}` - Get prospect by ID
- `POST /api/prospects` - Create new prospect
- `PUT /api/prospects/{id}` - Update existing prospect
- `POST /api/prospects/{id}/merge` - Merge duplicate prospects
- `GET /health` - Health check endpoint

**Events Published**:
- `ProspectCreated` - When a new prospect is created
- `ProspectUpdated` - When a prospect is updated
- `ProspectMerged` - When prospects are merged

---

### StudentService
**Swagger UI**: [http://localhost:5120/swagger/index.html](http://localhost:5120/swagger/index.html)  
**OpenAPI JSON**: [http://localhost:5120/swagger/v1/swagger.json](http://localhost:5120/swagger/v1/swagger.json)

**Endpoints**:
- `GET /api/students` - List all students
- `GET /api/students/{id}` - Get student by ID
- `POST /api/students` - Create new student
- `PUT /api/students/{id}` - Update existing student
- `GET /health` - Health check endpoint

**Events Published**:
- `StudentCreated` - When a new student is created
- `StudentUpdated` - When a student is updated
- `StudentChanged` - When student status changes

**Required Fields** (Create):
- `firstName` - Student's first name
- `lastName` - Student's last name
- `email` - Unique email address
- `studentNumber` - Unique student number (e.g., "STU-2026-001")
- `enrollmentDate` - Date student enrolled (ISO 8601 format)

**Optional Fields**:
- `phone` - Contact phone number
- `status` - Student status (default: "Active")
- `expectedGraduationDate` - Expected graduation date
- `notes` - Additional notes

---

### InstructorService
**Swagger UI**: [http://localhost:5130/swagger/index.html](http://localhost:5130/swagger/index.html)  
**OpenAPI JSON**: [http://localhost:5130/swagger/v1/swagger.json](http://localhost:5130/swagger/v1/swagger.json)

**Endpoints**:
- `GET /api/instructors` - List all instructors
- `GET /api/instructors/{id}` - Get instructor by ID
- `POST /api/instructors` - Create new instructor
- `PUT /api/instructors/{id}` - Update existing instructor
- `GET /health` - Health check endpoint

**Events Published**:
- `InstructorCreated` - When a new instructor is created
- `InstructorUpdated` - When an instructor is updated
- `InstructorDeactivated` - When an instructor is deactivated

**Required Fields** (Create):
- `firstName` - Instructor's first name
- `lastName` - Instructor's last name
- `email` - Unique email address
- `employeeNumber` - Unique employee number (e.g., "EMP-2026-001")
- `hireDate` - Date instructor was hired (ISO 8601 format)

**Optional Fields**:
- `phone` - Contact phone number
- `status` - Instructor status (default: "Active")
- `specialization` - Teaching specialization area
- `notes` - Additional notes

---

## 🔐 Authentication

All API endpoints (except `/api/auth/login` and Swagger docs) require JWT authentication.

### Get Access Token

```bash
curl -X POST http://localhost:5037/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "test123"
  }'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 86400
}
```

### Use Token in Requests

Include the token in the `Authorization` header:

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

## 📝 Example Requests

### Create a Prospect

```bash
curl -X POST http://localhost:5037/api/prospects \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "555-1234",
    "notes": "Interested in Computer Science"
  }'
```

### Create a Student

```bash
curl -X POST http://localhost:5037/api/students \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@school.edu",
    "studentNumber": "STU-2026-001",
    "enrollmentDate": "2026-01-15T00:00:00Z",
    "phone": "555-5678"
  }'
```

### Create an Instructor

```bash
curl -X POST http://localhost:5037/api/instructors \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Dr. Sarah",
    "lastName": "Williams",
    "email": "sarah.williams@school.edu",
    "employeeNumber": "EMP-2026-001",
    "hireDate": "2026-01-10T00:00:00Z",
    "specialization": "Mathematics",
    "phone": "555-9999"
  }'
```

---

## 🔄 Event Flow

1. **Command** → POST to ApiGateway (e.g., `/api/prospects`)
2. **ApiGateway** → Forwards to service (e.g., ProspectService on port 5110)
3. **Service** → Saves entity + event to database (transactional outbox)
4. **Service** → Pushes event to ApiGateway webhook (`POST /api/events/webhook`)
5. **ApiGateway** → Broadcasts event via WebSocket to connected clients
6. **Frontend** → Receives event → Invalidates React Query cache → UI updates

---

## 🧪 Testing Tips

### Using PowerShell

```powershell
# Login
$loginBody = @{ email = "test@example.com"; password = "test123" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5037/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$headers = @{ Authorization = "Bearer $($auth.token)" }

# Get prospects
Invoke-RestMethod -Uri "http://localhost:5037/api/prospects" -Headers $headers

# Create prospect
$prospect = @{
    firstName = "Test"
    lastName = "User"
    email = "test.user@example.com"
    phone = "555-0000"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5037/api/prospects" -Method Post -Body $prospect -ContentType "application/json" -Headers $headers
```

### Using Swagger UI

1. Open any Swagger UI link above
2. Click **"Authorize"** button at top
3. Enter: `Bearer YOUR_JWT_TOKEN`
4. Click **"Authorize"** then **"Close"**
5. Try any endpoint using the **"Try it out"** button

---

## 📊 API Response Formats

### Success Response (GET)

```json
[
  {
    "prospectId": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "555-1234",
    "status": "New",
    "createdAt": "2026-01-29T10:00:00Z",
    "updatedAt": "2026-01-29T10:00:00Z"
  }
]
```

### Success Response (POST/PUT)

```json
{
  "prospectId": 123,
  "correlationId": "abc-def-123"
}
```

### Error Response

```json
{
  "errors": [
    "A prospect with email john.doe@example.com already exists."
  ]
}
```

---

## 🏥 Health Checks

All services expose health check endpoints:

- ProspectService: [http://localhost:5110/health](http://localhost:5110/health)
- StudentService: [http://localhost:5120/health](http://localhost:5120/health)
- InstructorService: [http://localhost:5130/health](http://localhost:5130/health)
- ApiGateway: [http://localhost:5037/health](http://localhost:5037/health)

---

## 🛠️ Development Mode

All services currently run in **development mode**:

✅ In-memory databases (no SQL Server required)  
✅ HTTP-based event flow (no Azure Event Grid required)  
✅ No Azure Service Bus required  
✅ Hot reload enabled  
✅ Swagger UI enabled  

To switch to production mode, configure connection strings in `appsettings.json`.

---

## 📖 Additional Documentation

- [Services Startup Guide](services-startup-guide.md)
- [API Data Contracts](api-data-contracts.md)
- [Service Implementation Checklist](service-implementation-checklist.md)
- [Development Mode Patterns](development-mode-patterns.md)
- [Copilot Instructions](.github/copilot-instructions.md)

---

**Last Updated**: January 29, 2026  
**System Status**: ✅ All services operational
