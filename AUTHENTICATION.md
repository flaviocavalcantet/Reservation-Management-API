# Authentication & Authorization Guide

## Overview

The Reservation Management API implements **JWT Bearer Token authentication** with **role-based access control (RBAC)**. All components follow Clean Architecture principles with strict separation of concerns.

**Status**: ✅ Production-Ready  
**Framework**: ASP.NET Core 8.0  
**Security**: JWT with HS256 (HMAC SHA-256)

---

## Architecture

### Component Structure

```
API Layer
├── AuthenticationEndpoints (Login/Register)
└── [Authorize] attribute on protected endpoints

Application Layer
└── ITokenService (abstraction for token generation)

Infrastructure Layer
├── JwtTokenService (token generation/validation)
├── JwtSettings (configuration)
├── ApplicationUser (Identity user implementation)
└── AuthenticationServiceConfiguration (DI setup)
```

### Clean Architecture Compliance

- **Domain Layer**: Zero authentication dependencies (pure business logic)
- **Application Layer**: ITokenService abstraction (framework-agnostic)
- **Infrastructure Layer**: JWT implementation, EntityFramework Core, Identity
- **API Layer**: HTTP concerns, token validation middleware, [Authorize] guards

---

## Configuration

### 1. JWT Settings (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "{use environment variable}",
    "Issuer": "ReservationAPI",
    "Audience": "ReservationAPIUsers",
    "ExpirationMinutes": 15
  }
}
```

### 2. Environment Variables

Set the JWT secret key via environment variable (recommended for security):

```powershell
# Windows PowerShell
$env:JwtSettings__SecretKey = "your-256-bit-base64-encoded-key"

# Linux/Mac
export JwtSettings__SecretKey="your-256-bit-base64-encoded-key"
```

**Generate a Secure Key**:
```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### 3. Service Registration (Program.cs)

```csharp
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();
```

---

## API Endpoints

### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK)**:
```json
{
  "accessToken": "eyJhbGc...",
  "expiresIn": 900,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "roles": ["User"]
}
```

### Register
```http
POST /auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Response (201 Created)**:
```json
{
  "accessToken": "eyJhbGc...",
  "expiresIn": 900,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "roles": ["User"]
}
```

### Protected Endpoints

All protected endpoints require the `Authorization` header:

```http
GET /reservations
Authorization: Bearer {accessToken}
```

---

## Token Structure

### Access Token Claims

```csharp
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",  // NameIdentifier (UserId)
  "email": "user@example.com",
  "role": ["User", "Manager"],                     // Multiple roles supported
  "iss": "ReservationAPI",                         // Issuer
  "aud": "ReservationAPIUsers",                    // Audience
  "exp": 1234567890,                               // Expiration (Unix timestamp)
  "iat": 1234566990                                // Issued at
}
```

### Validation Rules

- ✅ Signature validated using secret key
- ✅ Issuer must match configuration
- ✅ Audience must match configuration  
- ✅ Token must not be expired
- ✅ Required claims present (sub, email, iss, aud, exp)

---

## Role-Based Access Control

### Define Roles

```csharp
[Authorize(Roles = "Admin")]
public async Task<IResult> AdminOnlyEndpoint() { }

[Authorize(Roles = "User,Manager")]  // Either role
public async Task<IResult> UserOrManagerEndpoint() { }
```

### Available Roles

- **Admin**: Full system access, user management
- **Manager**: Operational control, batch operations
- **User**: Standard access to personal reservations

### Assign Roles

```csharp
await userManager.AddToRoleAsync(user, "User");
await userManager.AddToRoleAsync(user, "Manager");
```

---

## Security Practices

### ✅ Implemented

- Secret key stored in environment variables (not in code)
- Short-lived access tokens (15 minutes default)
- HTTPS-only token transmission (in production)
- Password hashing with ASP.NET Core Identity
- Secure token validation (signature, expiration, claims)

### 🔒 Recommendations

- Use HTTPS in production
- Rotate secret keys periodically
- Implement token refresh flow for better UX
- Log authentication failures
- Implement rate limiting on login endpoint
- Monitor for suspicious token patterns

---

## Testing

### Unit Tests (27 tests)

**File**: `tests/Reservation.Tests/Application/Authentication/AuthenticationTests.cs`

Coverage includes:
- Email validation (7 tests)
- Password validation (6 tests)
- Credential validation (8 tests)
- Role-based access control (6 tests)

**Run Tests**:
```bash
dotnet test tests/Reservation.Tests/ -v detailed
```

All tests use mocks - no database or infrastructure dependencies.

---

## Troubleshooting

### 401 Unauthorized on Protected Endpoint
- Verify token is included in `Authorization: Bearer {token}` header
- Verify token has not expired
- Check token claims match endpoint requirements

### 403 Forbidden
- User's roles don't match endpoint `[Authorize(Roles = "...")]` requirement
- Add role to user: `await userManager.AddToRoleAsync(user, "RoleName")`

### Invalid Token Signature
- Verify environment variable `JwtSettings__SecretKey` is set correctly
- Secret key must match the key used to generate the token
- Regenerate token after changing secret key

---

## See Also

- [README.md](README.md) - Project overview
- [QUICKSTART.md](QUICKSTART.md) - Setup instructions
- [TESTING.md](TESTING.md) - Test suite documentation
- [API_ENDPOINTS.md](API_ENDPOINTS.md) - Complete endpoint reference
