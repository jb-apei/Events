# Events Project - Testing Quick Reference Card

## 🔐 Login Credentials (MVP)

**Any email/password works!** The MVP accepts any credentials for testing.

### Suggested Test Accounts

```
Email: admin@events.com
Password: admin123

Email: test@example.com
Password: password

Email: john.doe@company.com
Password: test123
```

**Note**: Token is valid for **24 hours** after login.

---

## 🌐 Service Endpoints

### Frontend
- **URL**: http://localhost:3000
- **Framework**: React + Vite + TypeScript

### Backend Services

| Service | Port | URL | Purpose |
|---------|------|-----|---------|
| **ApiGateway** | 5000 | http://localhost:5000 | REST API + WebSocket hub |
| **ProspectService** | 5110 | http://localhost:5110 | Write model (commands) |
| **EventRelay** | 5120 | http://localhost:5120 | Outbox publisher |
| **ProjectionService** | 5130 | http://localhost:5130 | Read model (projections) |

---

## 🔌 API Endpoints (ApiGateway)

### Authentication
```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password"
}

Response: { "token": "eyJ...", "expiresAt": "...", "userId": "...", "email": "..." }
```

### Prospects

**Create Prospect**
```http
POST http://localhost:5000/api/prospects
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "555-1234",
  "source": "Website",
  "notes": "Interested in services"
}

Response: 202 Accepted
```

**Update Prospect**
```http
PUT http://localhost:5000/api/prospects/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "555-5678",
  "status": "Contacted"
}

Response: 202 Accepted
```

**Get All Prospects**
```http
GET http://localhost:5000/api/prospects
Authorization: Bearer {token}

Response: [{ "prospectId": 1, "firstName": "John", ... }]
```

**Get Single Prospect**
```http
GET http://localhost:5000/api/prospects/1
Authorization: Bearer {token}

Response: { "prospectId": 1, "firstName": "John", ... }
```

---

## 🔄 WebSocket Connection

**Endpoint**: `ws://localhost:5000/ws/events?token={jwt_token}`

**Subscribe to Events**:
```json
{
  "eventTypes": ["ProspectCreated", "ProspectUpdated"]
}
```

**Received Event Format**:
```json
{
  "eventId": "guid",
  "eventType": "ProspectCreated",
  "occurredAt": "2026-01-29T10:30:00Z",
  "correlationId": "trace-id",
  "subject": "prospect/123",
  "data": {
    "prospectId": 123,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    ...
  }
}
```

---

## 🧪 Testing Workflow (End-to-End)

### 1. Start All Services

**Terminal 1 - Frontend**:
```powershell
cd c:\Users\jbouchard\Documents\Projects\Events\src\frontend
npm run dev
```

**Terminal 2 - ApiGateway**:
```powershell
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ApiGateway
dotnet run
```

**Terminal 3 - ProspectService**:
```powershell
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ProspectService
dotnet run
```

**Terminal 4 - EventRelay** (optional for full event flow):
```powershell
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\EventRelay
dotnet run
```

**Terminal 5 - ProjectionService** (optional for read models):
```powershell
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ProjectionService
dotnet run
```

---

### 2. Test Complete Flow

**Step 1**: Open http://localhost:3000

**Step 2**: Login with any credentials
- Email: `test@example.com`
- Password: `password`

**Step 3**: Select Event Type
- Choose "ProspectCreated" from dropdown

**Step 4**: Fill Form
- First Name: John
- Last Name: Doe
- Email: john.doe@example.com
- Phone: 555-1234
- Status: New
- Notes: Test prospect

**Step 5**: Submit
- Click "Create Prospect"
- ✅ Watch for success message
- ✅ Watch prospect appear in list (via WebSocket update)

**Step 6**: Update Prospect
- Click on prospect in list
- Form auto-populates
- Switch to "ProspectUpdated" event type
- Change status to "Contacted"
- Submit
- ✅ Watch for real-time update in list

---

## 🗄️ Database Configuration (Local Testing)

### In-Memory Databases (Default)
All services use in-memory databases by default for quick testing without SQL Server.

### To Use SQL Server (Optional)

**Update connection strings in `appsettings.json`**:

**ProspectService** (transactional DB):
```json
"ConnectionStrings": {
  "ProspectDatabase": "Server=localhost;Database=EventsTransactional;Integrated Security=true;TrustServerCertificate=true"
}
```

**ProjectionService** (read model DB):
```json
"ConnectionStrings": {
  "ProjectionDatabase": "Server=localhost;Database=EventsReadModel;Integrated Security=true;TrustServerCertificate=true"
}
```

---

## 🐛 Troubleshooting

### Frontend Not Loading
```powershell
# Refresh PATH for npm
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
cd c:\Users\jbouchard\Documents\Projects\Events\src\frontend
npm run dev
```

### Backend API Errors
- Check service is running: http://localhost:5000/health
- Verify JWT token is included in Authorization header
- Check terminal logs for error details

### WebSocket Not Connecting
- Verify ApiGateway is running on port 5000
- Check browser console for WebSocket errors
- Ensure JWT token is passed in query string

### Events Not Appearing in UI
- Verify ApiGateway is receiving Event Grid webhooks
- Check EventRelay is publishing to Event Grid
- Confirm WebSocket subscription includes correct event types

---

## 📊 Event Types Reference

### Prospect Events (MVP)
- `ProspectCreated` - New prospect added
- `ProspectUpdated` - Prospect information changed
- `ProspectMerged` - Two prospects merged

### Student Events (Future)
- `StudentCreated`
- `StudentUpdated`
- `StudentChanged`

### Instructor Events (Future)
- `InstructorCreated`
- `InstructorUpdated`
- `InstructorDeactivated`

---

## 🔍 Health Check Endpoints

| Service | Health Check URL |
|---------|-----------------|
| ApiGateway | http://localhost:5000/health |
| ProspectService | http://localhost:5110/health |
| ProjectionService | http://localhost:5130/events/health |

---

## 📝 Sample Data for Testing

### Test Prospects

```json
// Prospect 1
{
  "firstName": "Alice",
  "lastName": "Johnson",
  "email": "alice.johnson@tech.com",
  "phone": "555-0101",
  "source": "LinkedIn",
  "status": "New"
}

// Prospect 2
{
  "firstName": "Bob",
  "lastName": "Smith",
  "email": "bob.smith@startup.io",
  "phone": "555-0202",
  "source": "Referral",
  "status": "Qualified"
}

// Prospect 3
{
  "firstName": "Carol",
  "lastName": "Williams",
  "email": "carol.w@company.com",
  "phone": "555-0303",
  "source": "Website",
  "status": "Contacted"
}
```

---

## 🎯 Testing Checklist

- [ ] Login with test credentials
- [ ] Create new prospect (ProspectCreated event)
- [ ] Verify prospect appears in list immediately
- [ ] Update prospect status (ProspectUpdated event)
- [ ] Verify update reflects in real-time
- [ ] Check WebSocket connection status
- [ ] Test form validation (empty fields)
- [ ] Test with multiple prospects
- [ ] Verify correlation IDs in logs
- [ ] Check Event Grid topic receives events (if configured)

---

## 💡 Tips

1. **Keep terminal windows visible** to watch real-time logs
2. **Browser DevTools** → Network tab shows WebSocket messages
3. **React Query DevTools** shows cache state (bottom-left of UI)
4. **Correlation IDs** link requests across services (check logs)
5. **In-memory DBs reset** on service restart (data is lost)

---

## 🚀 Quick Start Command (Copy-Paste)

```powershell
# Start all services in one command (requires 5 terminal windows)
# Terminal 1
cd c:\Users\jbouchard\Documents\Projects\Events\src\frontend; npm run dev

# Terminal 2
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ApiGateway; dotnet run

# Terminal 3
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ProspectService; dotnet run

# Terminal 4
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\EventRelay; dotnet run

# Terminal 5
cd c:\Users\jbouchard\Documents\Projects\Events\src\services\ProjectionService; dotnet run
```

---

**Last Updated**: January 29, 2026  
**Project**: Events - Identity Management System  
**Architecture**: Event-Driven Microservices (CQRS + Event Sourcing)
