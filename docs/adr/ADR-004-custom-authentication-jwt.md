# ADR-004: Custom Authentication with JWT

**Status:** Accepted  
**Date:** 2026-01-17  
**Deciders:** Architecture Team

## Context

The Events project needs to authenticate users accessing the API Gateway and React frontend. We need a lightweight solution for this MVP that can evolve into Azure AD B2C or other enterprise identity providers in the future.

## Decision

We will implement **custom JWT-based authentication** where:
- ApiGateway issues JWT tokens on successful `/api/auth/login`
- Tokens contain user claims (userId, email, roles)
- All subsequent API requests include JWT in `Authorization: Bearer` header
- ApiGateway validates JWT on every request
- WebSocket connections authenticated via JWT in initial handshake

## Rationale

### Pros
- **Simplicity**: No external identity provider dependencies for MVP
- **Stateless**: JWTs are self-contained, no server-side session storage
- **Performance**: Fast validation (signature check only, no database lookup)
- **Flexibility**: Can add custom claims for authorization
- **WebSocket Support**: Single token works for HTTP and WebSocket
- **Migration Path**: Easy to swap JWT issuer with Azure AD B2C later

### Cons
- **No User Management**: No built-in password reset, MFA, etc.
- **Token Revocation**: Cannot invalidate tokens before expiry (mitigation: short TTL)
- **Secret Management**: JWT signing key must be securely stored
- **Scale Limitation**: Not suitable for large enterprise identity scenarios

## Alternatives Considered

### 1. Azure AD B2C
- **Rejected for MVP**: Too complex for initial development
- Requires tenant setup, user flows, policies
- **Future Direction**: Migrate to AD B2C for production

### 2. Identity Server / Duende
- **Rejected**: Heavy framework, overkill for MVP
- Adds significant infrastructure complexity

### 3. Cookie-Based Sessions
- **Rejected**: Doesn't work well with microservices and SPAs
- Requires sticky sessions (complicates load balancing)
- CORS issues with cross-domain cookies

## Implementation Details

### JWT Token Structure
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-123",
    "email": "user@example.com",
    "name": "John Doe",
    "roles": ["user"],
    "iss": "ApiGateway",
    "aud": "events-api",
    "exp": 1706630400,
    "iat": 1706626800
  }
}
```

### Token Lifecycle
- **TTL**: 1 hour (configurable via `Jwt:ExpirationHours`)
- **Refresh**: Not implemented in MVP (users re-login after expiry)
- **Storage**: LocalStorage in React (cleared on logout)

### Login Flow
```
1. POST /api/auth/login { email, password }
2. ApiGateway validates credentials (placeholder: test@test.com/test123)
3. Generate JWT with user claims
4. Return { token, expiresAt }
5. React stores token in localStorage
6. All API/WebSocket requests include: Authorization: Bearer <token>
```

### Validation Middleware
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]))
        };
        
        // WebSocket authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

## Security Considerations

### JWT Secret Management
- **Local Dev**: Stored in .NET user-secrets (not committed)
- **Azure**: Retrieved from Key Vault via managed identity
- **Secret Rotation**: Change `Jwt:Secret` to invalidate all tokens

### Attack Mitigations
- **XSS**: HttpOnly cookies would be better, but incompatible with SPA architecture
- **CSRF**: Not vulnerable (no cookies used)
- **Token Theft**: Short expiry (1 hour) limits damage window
- **Replay Attacks**: HTTPS required (prevents man-in-the-middle)

## Migration Path to Azure AD B2C

When ready to migrate:
1. Configure Azure AD B2C tenant
2. Update `AddAuthentication` to use `AddMicrosoftIdentityWebApi`
3. Replace `/api/auth/login` with redirect to B2C login flow
4. Update React to use MSAL library
5. Keep JWT validation logic (AD B2C issues standard JWTs)

## Consequences

### Positive
- MVP can ship quickly without external dependencies
- Developers can test authentication flow locally
- Simple to understand and debug
- No Azure AD B2C costs during development

### Negative
- No enterprise features (SSO, MFA, passwordless)
- Manual user management required
- Cannot revoke tokens before expiry
- Limited to single-tenant use case

## Future Enhancements

### Phase 2 (Production Readiness)
- Implement refresh tokens (longer sessions without re-login)
- Add token revocation list (Redis cache)
- Integrate Azure AD B2C for enterprise users
- Add role-based access control (RBAC)

### Phase 3 (Enterprise)
- Support multiple identity providers (AD, Google, Facebook)
- Implement custom user registration flow
- Add MFA support
- Audit logging for authentication events

## References

- [Microsoft: JWT Tokens](https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens)
- [JWT.io](https://jwt.io/) - Token debugger
- [OWASP: JWT Security](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- Project: `src/services/ApiGateway/Services/JwtService.cs`
