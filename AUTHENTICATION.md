# Authentication & Authorization Guide

## Overview

The Reservation Management API implements **JWT Bearer Token authentication** with **role-based access control (RBAC)**. All components follow Clean Architecture principles with strict separation of concerns.

In addition to its custom JWT scheme, the API supports **OAuth2 / OpenID Connect (OIDC)** via [Auth0](https://auth0.com/), so clients can authenticate using the **Authorization Code + PKCE** flow and a third-party identity provider. Both schemes are validated transparently - see [OIDC Integration](#oidc-integration) below.

**Status**: ✅ Production-Ready  
**Framework**: ASP.NET Core 8.0  
**Security**: Custom JWT with HS256 (HMAC SHA-256) + Auth0 OIDC access tokens (RS256)

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
├── JwtTokenService (custom JWT token generation/validation)
├── JwtSettings (custom JWT configuration)
├── Auth0Settings (Auth0/OIDC configuration)
├── AuthenticationSchemeSelector (routes requests to "Bearer" or "Auth0" by token issuer)
├── ApplicationUser (Identity user implementation)
└── AuthenticationServiceConfiguration (DI setup - registers both schemes)
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

### 1a. Auth0 (OIDC) Settings (appsettings.json)

```json
{
  "Auth0Settings": {
    "Authority": "https://YOUR_TENANT.us.auth0.com/",
    "Audience": "https://reservation-api/",
    "RoleClaimType": "https://reservation-api/roles"
  }
}
```

Leave `Authority` empty to disable the Auth0 scheme entirely (default). See [OIDC Integration](#oidc-integration) for full setup instructions.

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

## OIDC Integration

### Overview

Alongside the custom HS256 JWT scheme, the API can validate **RS256 access tokens issued by Auth0** using the standard **OAuth2 Authorization Code flow with PKCE** (Proof Key for Code Exchange). This is the recommended flow for SPAs, mobile apps, and any public client.

Both schemes are active at the same time:

| Scheme name | Token type            | Issuer (`iss`)                         | Signing algorithm | Used for |
|-------------|------------------------|-----------------------------------------|--------------------|----------|
| `Bearer`    | Custom JWT             | `ReservationAPI`                        | HS256 (shared secret) | `/api/v1/auth/login`, `/api/v1/auth/register` |
| `Auth0`     | Auth0 OIDC access token | `https://YOUR_TENANT.us.auth0.com/`     | RS256 (JWKS)       | Tokens obtained via Auth0 login (SSO, social, enterprise connections) |

A request is routed to the correct scheme automatically: `AuthenticationSchemeSelector` reads the `iss` claim from the bearer token (without validating its signature) and forwards the request to the matching `JwtBearerHandler`, which then performs full signature/issuer/audience/lifetime validation. `[Authorize]` and `[Authorize(Roles = "...")]` attributes work unchanged for both token types - no per-endpoint configuration is required.

If `Auth0Settings:Authority` is left empty, the `Auth0` scheme is **not registered** and the API behaves exactly as it did before (custom JWT only). This keeps the feature fully opt-in.

### 1. Setting Up a Free Auth0 Tenant

1. Create a free account at [auth0.com](https://auth0.com/) (free tier supports up to 25,000 monthly active users - more than enough for a demo/portfolio project).
2. In the Auth0 Dashboard, create a **tenant** (e.g. `dev-reservation-api`). Your issuer/Authority will be `https://dev-reservation-api.us.auth0.com/`.
3. **Create an API** (Applications → APIs → Create API):
   - Name: `Reservation Management API`
   - Identifier (Audience): `https://reservation-api/` - this is an arbitrary URI used as the `aud` claim, it does **not** need to be reachable.
   - Signing algorithm: `RS256`
4. **Create an Application** (Applications → Applications → Create Application):
   - Type: **Single Page Application** (for the Authorization Code + PKCE flow) or **Native** for mobile/desktop clients.
   - Note the **Domain**, **Client ID**, and configure **Allowed Callback URLs** / **Allowed Web Origins** for your client app (e.g. `http://localhost:3000/callback`).
5. (Optional, for RBAC) **Map Auth0 roles to a custom claim** so they appear in the access token (see [Mapping Auth0 Roles to Existing RBAC](#mapping-auth0-roles-to-existing-rbac) below).

### 2. Configuration

Add the Auth0 tenant details to `appsettings.json` (or `appsettings.Development.json` / environment variables for secrets-free configuration):

```json
"Auth0Settings": {
  "Authority": "https://YOUR_TENANT.us.auth0.com/",
  "Audience": "https://reservation-api/",
  "RoleClaimType": "https://reservation-api/roles"
}
```

- **Authority**: Your Auth0 tenant's issuer URL. Must include the trailing slash - it must match the `iss` claim exactly.
- **Audience**: The API Identifier created in step 1.3 above. Must match the `aud` claim.
- **RoleClaimType**: The namespaced custom claim Auth0 will use to carry role information (see below).

A template is provided at [`appsettings.auth0.example.json`](src/Reservation.API/appsettings.auth0.example.json). Settings can also be supplied via environment variables, e.g.:

```powershell
$env:Auth0Settings__Authority = "https://dev-reservation-api.us.auth0.com/"
$env:Auth0Settings__Audience  = "https://reservation-api/"
```

### 3. Authorization Code + PKCE Flow

This flow is performed by the **client application** (SPA, mobile app, etc.) - the API only validates the resulting access token.

```
1. Client generates a random "code_verifier" and derives a "code_challenge" (SHA-256, base64url)
2. Client redirects the user to Auth0's /authorize endpoint:

   GET https://YOUR_TENANT.us.auth0.com/authorize
     ?response_type=code
     &client_id={CLIENT_ID}
     &redirect_uri={CALLBACK_URL}
     &scope=openid profile email
     &audience=https://reservation-api/
     &code_challenge={CODE_CHALLENGE}
     &code_challenge_method=S256
     &state={RANDOM_STATE}

3. User authenticates with Auth0 (or a social/enterprise identity provider)
4. Auth0 redirects back to {CALLBACK_URL}?code={AUTH_CODE}&state={STATE}
5. Client exchanges the authorization code for tokens:

   POST https://YOUR_TENANT.us.auth0.com/oauth/token
   Content-Type: application/json

   {
     "grant_type": "authorization_code",
     "client_id": "{CLIENT_ID}",
     "code_verifier": "{CODE_VERIFIER}",
     "code": "{AUTH_CODE}",
     "redirect_uri": "{CALLBACK_URL}"
   }

6. Auth0 responds with an access_token (RS256 JWT), id_token, and refresh_token
7. Client calls the Reservation API with:

   GET /api/v1/reservations
   Authorization: Bearer {access_token}
```

### 4. Obtaining a Test Token Without a Full Client

For local testing/Postman, Auth0 supports the **Resource Owner Password Grant** (not recommended for production, but convenient for development/testing):

```http
POST https://YOUR_TENANT.us.auth0.com/oauth/token
Content-Type: application/json

{
  "grant_type": "password",
  "username": "test-user@example.com",
  "password": "TestPassword123!",
  "audience": "https://reservation-api/",
  "client_id": "{CLIENT_ID}",
  "client_secret": "{CLIENT_SECRET}",
  "scope": "openid profile email"
}
```

The response's `access_token` can be used directly as a `Bearer` token against the Reservation API.

### Mapping Auth0 Roles to Existing RBAC

Auth0 access tokens do **not** include role information by default. To make `[Authorize(Roles = "Admin")]`-style checks work with Auth0 tokens, add a custom claim via an **Auth0 Action**:

1. Auth0 Dashboard → **Actions** → **Flows** → **Login**
2. Add a new Action (e.g. "Add Roles Claim") with:

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://reservation-api/';
  if (event.authorization) {
    api.accessToken.setCustomClaim(`${namespace}roles`, event.authorization.roles);
  }
};
```

3. Assign roles (`Admin`, `Manager`, `User` - matching this API's existing roles) to users/groups in **User Management → Roles**.
4. Set `Auth0Settings:RoleClaimType` to the namespaced claim (`https://reservation-api/roles`), matching the claim set above.

The `Auth0` JWT Bearer scheme is configured with `TokenValidationParameters.RoleClaimType` set to this value, so ASP.NET Core maps it onto `ClaimTypes.Role` automatically - identical to how the custom JWT's `role` claims are mapped. `NameClaimType` is set to `ClaimTypes.NameIdentifier`, matching the `sub` claim used by the custom JWT scheme for user identification.

### Token Claim Comparison

| Claim | Custom JWT (`Bearer`)        | Auth0 OIDC (`Auth0`)                                  |
|-------|-------------------------------|--------------------------------------------------------|
| `sub` | User's GUID (`ClaimTypes.NameIdentifier`) | Auth0 user ID (e.g. `auth0\|abc123`) |
| `iss` | `ReservationAPI`               | `https://YOUR_TENANT.us.auth0.com/`                     |
| `aud` | `ReservationWebApp`            | `https://reservation-api/`                              |
| roles | `role` claim (multiple values) | `https://reservation-api/roles` custom claim (mapped via `RoleClaimType`) |
| `exp` / `iat` | Set by `JwtTokenService` | Set by Auth0 |
| Algorithm | HS256 (shared secret) | RS256 (validated against Auth0's JWKS endpoint) |

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

### Unit Tests (39 tests)

**Files**:
- `tests/Reservation.Tests/Application/Authentication/AuthenticationTests.cs` (27 tests)
- `tests/Reservation.Tests/Application/Authentication/Auth0AuthenticationTests.cs` (12 tests)

Coverage includes:
- Email validation (7 tests)
- Password validation (6 tests)
- Credential validation (8 tests)
- Role-based access control (6 tests)
- Auth0 settings validation (6 tests)
- Authentication scheme selection by token issuer (6 tests)

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
