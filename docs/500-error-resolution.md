# 500 Error Resolution - StudentService & InstructorService

## Problem Summary
StudentService and InstructorService POST endpoints were returning 500 Internal Server Error while ProspectService worked perfectly.

## Root Causes Identified

### 1. Wrong Project Reference
**Issue**: StudentService and InstructorService were referencing the wrong shared project.

**Details**:
- Services referenced `../Shared/Shared.csproj` (newly created during investigation)
- Should reference `../Shared.Events/Shared.Events.csproj` (like ProspectService)
- Shared.Events contains the EventEnvelope base class and all event definitions

**Fix**:
```xml
<!-- BEFORE (Wrong) -->
<ProjectReference Include="..\Shared\Shared.csproj" />

<!-- AFTER (Correct) -->
<ProjectReference Include="..\Shared.Events\Shared.Events.csproj" />
```

**Files Modified**:
- `src/services/StudentService/StudentService.csproj`
- `src/services/InstructorService/InstructorService.csproj`

---

### 2. Missing Event Data Fields
**Issue**: Event data classes in Shared.Events were missing fields that command handlers expected.

**StudentService Events**:
- `StudentCreatedData` was missing: `StudentNumber`, `ExpectedGraduationDate`, `Notes`
- `StudentUpdatedData` was missing: `ExpectedGraduationDate`, `Notes`

**InstructorService Events**:
- `InstructorCreatedData` was missing: `EmployeeNumber`, `Specialization`, `Notes`
- `InstructorUpdatedData` was missing: `Specialization`, `Notes`

**Fix**: Added all missing fields to event data classes to match what handlers create.

**Files Modified**:
- `src/services/Shared.Events/Students/StudentCreated.cs`
- `src/services/Shared.Events/Students/StudentUpdated.cs`
- `src/services/Shared.Events/Instructors/InstructorCreated.cs`
- `src/services/Shared.Events/Instructors/InstructorUpdated.cs`

---

### 3. In-Memory Database Transaction Warning
**Issue**: EF Core 9.0 throws an error when transactions are used with in-memory databases.

**Error Message**:
```
An error was generated for warning 'Microsoft.EntityFrameworkCore.Database.Transaction.TransactionIgnoredWarning': 
Transactions are not supported by the in-memory store.
```

**Explanation**:
- Command handlers use `BeginTransactionAsync()` for transactional outbox pattern
- In-memory databases don't support transactions (they're atomic by nature)
- EF Core generates a warning, which was being treated as an error

**Fix**: Suppress the transaction warning in DbContext configuration.

**Code Change** (StudentService Program.cs):
```csharp
// BEFORE
builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseInMemoryDatabase("StudentDb"));

// AFTER
builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseInMemoryDatabase("StudentDb")
           .ConfigureWarnings(warnings => 
               warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
```

**Files Modified**:
- `src/services/StudentService/Program.cs`
- `src/services/InstructorService/Program.cs`

---

## Testing Results

### StudentService (Port 5120)
**Test**: `POST /api/students`

**Request**:
```json
{
  "firstName": "Alice",
  "lastName": "Wilson",
  "email": "alice.wilson@test.edu",
  "studentNumber": "STU-123",
  "enrollmentDate": "2025-01-15T00:00:00Z"
}
```

**Response**: ✅ **201 Created**
```json
{
  "studentId": 1,
  "correlationId": "<guid>"
}
```

---

### InstructorService (Port 5130)
**Test**: `POST /api/instructors`

**Request**:
```json
{
  "firstName": "Bob",
  "lastName": "Johnson",
  "email": "bob.johnson@test.edu",
  "employeeNumber": "EMP-456",
  "hireDate": "2024-01-01T00:00:00Z"
}
```

**Response**: ✅ **201 Created**
```json
{
  "instructorId": 1,
  "correlationId": "<guid>"
}
```

---

## Lessons Learned

1. **Project References Matter**: Always verify project references match across similar services
2. **Event Schema Alignment**: Event data classes must match what handlers create - field mismatches cause runtime errors
3. **In-Memory DB Limitations**: In-memory databases don't support transactions; suppress warnings when using transactional patterns
4. **Debugging Strategy**: Add detailed exception handling in controllers during development to surface root causes
5. **Port Configuration**: Services run on different ports - verify correct port before testing

---

## System Status

| Service | Port | Status | Notes |
|---------|------|--------|-------|
| ApiGateway | 5037 | ✅ Running | Authentication, WebSocket, Proxies |
| ProspectService | 5110 | ✅ Running | POST working perfectly |
| StudentService | 5120 | ✅ **FIXED** | POST endpoint now working |
| InstructorService | 5130 | ✅ **FIXED** | POST endpoint now working |

---

## Next Steps

1. ✅ StudentService and InstructorService fully operational
2. ⏭️ Test complete workflow: Create → Read → Update for all entities
3. ⏭️ Verify events are published to ApiGateway webhook
4. ⏭️ Test real-time WebSocket event subscriptions
5. ⏭️ Update system test summary documentation

---

**Date**: 2025-01-29  
**Resolution Time**: ~2 hours  
**Status**: ✅ **RESOLVED**
