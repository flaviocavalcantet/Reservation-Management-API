# API Endpoints Documentation

Complete reference guide for all REST API endpoints in the Reservation Management System.

**API Version**: v1  
**Base URL**: `/api/v1`  
**Content-Type**: `application/json`  
**Authentication**: JWT Bearer Token (see [AUTHENTICATION.md](AUTHENTICATION.md))

---

## Overview

The Reservation Management API provides endpoints for creating, managing, and querying reservations. The API follows RESTful principles and uses HTTP status codes to indicate operation success or failure.

The API also includes health check endpoints for monitoring application status and readiness.

### Quick Reference Table

| Method | Endpoint | Auth | Description | Status Code |
|--------|----------|------|-------------|-------------|
| `GET` | `/health/live` | No | Liveness check - is application running | 200 OK |
| `GET` | `/health/ready` | No | Readiness check - is application ready for traffic | 200 OK |
| `GET` | `/health/detailed` | No | Detailed health status with all dependencies | 200 OK |
| `POST` | `/auth/login` | No | Login with email/password | 200 OK |
| `POST` | `/auth/register` | No | Register new user | 201 Created |
| `POST` | `/reservations` | Yes | Create a new reservation | 201 Created |
| `POST` | `/reservations/{id}/confirm` | Yes | Confirm a reservation | 200 OK |
| `POST` | `/reservations/{id}/cancel` | Yes | Cancel a reservation | 200 OK |
| `GET` | `/reservations?customerId={id}` | Yes | Get reservations by customer | 200 OK |

---

## Health Check Endpoints

### 1. Liveness Check

**Endpoint**: `GET /health/live`

Indicates if the application process is running. Used by orchestration platforms (Kubernetes, Docker) to determine if the container should be restarted.

#### Request

**Headers**:
```
Content-Type: application/json
```

**Body**: Empty (GET request, no body)

#### Response

**Success (200 OK)**:
```json
{
  "status": "alive",
  "timestamp": "2026-01-24T10:30:00Z",
  "uptime": 512
}
```

**Service Unavailable (503 Service Unavailable)**:
```json
{
  "status": "unhealthy",
  "timestamp": "2026-01-24T10:30:00Z",
  "uptime": 512
}
```

#### Business Rules

- Always returns 200 if the application process is running
- No dependencies are checked
- Responds quickly (< 100ms)
- Can be called frequently without performance impact

#### Example cURL

```bash
curl -X GET https://localhost:7071/health/live \
  -H "Content-Type: application/json"
```

---

### 2. Readiness Check

**Endpoint**: `GET /health/ready`

Indicates if the application is ready to accept traffic. Used by load balancers to route traffic to this instance.

#### Request

**Headers**:
```
Content-Type: application/json
```

**Body**: Empty (GET request, no body)

#### Response

**Success (200 OK)**:
```json
{
  "status": "ready",
  "timestamp": "2026-01-24T10:30:00Z",
  "dependencies": ["self"]
}
```

**Not Ready (503 Service Unavailable)**:
```json
{
  "status": "notready",
  "timestamp": "2026-01-24T10:30:00Z",
  "dependencies": []
}
```

#### Business Rules

- Returns 200 when application startup is complete
- Checks all critical startup dependencies
- Used by load balancers for traffic routing decisions
- Endpoint should return 503 during application startup/shutdown

#### Example cURL

```bash
curl -X GET https://localhost:7071/health/ready \
  -H "Content-Type: application/json"
```

---

### 3. Detailed Health

**Endpoint**: `GET /health/detailed`

Returns comprehensive health information including all dependencies, runtime environment, and component statuses. Useful for monitoring dashboards and diagnostics.

#### Request

**Headers**:
```
Content-Type: application/json
```

**Body**: Empty (GET request, no body)

#### Response

**Success (200 OK)**:
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-24T10:30:00Z",
  "runtime": {
    "dotNetVersion": ".NET 8.0.0",
    "osDescription": "Windows 10 (build 19045)",
    "processorCount": 8
  },
  "components": {
    "database": {
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": 45.23
    },
    "cache": {
      "status": "Healthy",
      "description": "Cache service is healthy",
      "duration": 12.15
    }
  }
}
```

**Partially Degraded (503 Service Unavailable)**:
```json
{
  "status": "Degraded",
  "timestamp": "2026-01-24T10:30:00Z",
  "runtime": {
    "dotNetVersion": ".NET 8.0.0",
    "osDescription": "Windows 10 (build 19045)",
    "processorCount": 8
  },
  "components": {
    "database": {
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": 45.23
    },
    "cache": {
      "status": "Unhealthy",
      "description": "Cache service unavailable",
      "duration": 5000.00
    }
  }
}
```

#### Response Fields

**Top Level**:

| Field | Type | Description |
|-------|------|-------------|
| `status` | string | Overall health status: "Healthy", "Degraded", or "Unhealthy" |
| `timestamp` | DateTime (ISO 8601) | When the health check was performed |
| `runtime` | RuntimeInfo | Runtime environment information |
| `components` | object | Health status of individual components |

**Runtime Info**:

| Field | Type | Description |
|-------|------|-------------|
| `dotNetVersion` | string | .NET runtime version |
| `osDescription` | string | Operating system information |
| `processorCount` | int | Number of processors available |

**Component Health**:

| Field | Type | Description |
|-------|------|-------------|
| `status` | string | Component status: "Healthy", "Degraded", or "Unhealthy" |
| `description` | string | Human-readable description of component status |
| `duration` | number | Time taken for health check in milliseconds |

#### Health Status Values

| Status | Meaning | HTTP Code | Action |
|--------|---------|-----------|--------|
| `Healthy` | All components operational | 200 | Accept traffic normally |
| `Degraded` | Some components unhealthy but critical ones functional | 503 | Accept traffic with caution |
| `Unhealthy` | Critical components down | 503 | Do not route traffic |

#### Example cURL

```bash
curl -X GET https://localhost:7071/health/detailed \
  -H "Content-Type: application/json"
```

---

## Authentication Endpoints

### 1. Login

**Endpoint**: `POST /api/v1/auth/login`

Authenticates a user and returns a JWT access token.

#### Request

**Headers**:
```
Content-Type: application/json
```

**Body**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------| 
| `email` | string | Yes | User email address. Must be a valid email format. |
| `password` | string | Yes | User password. Minimum 6 characters. |

#### Response

**Success (200 OK)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "roles": ["User"]
}
```

**Invalid Credentials (401 Unauthorized)**:
```json
{
  "message": "Invalid email or password"
}
```

#### Business Rules

- Email must be a valid email format
- Password must be at least 6 characters
- Email and password are case-sensitive
- Invalid email/password returns 401 (generic message for security)

#### Example cURL

```bash
curl -X POST https://localhost:7071/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

---

### 2. Register

**Endpoint**: `POST /api/v1/auth/register`

Creates a new user account and returns a JWT access token.

#### Request

**Headers**:
```
Content-Type: application/json
```

**Body**:
```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fullName` | string | Yes | User's full name. 2-200 characters. |
| `email` | string | Yes | User email address. Must be unique and valid format. |
| `password` | string | Yes | User password. Minimum 6 characters. |

#### Response

**Success (201 Created)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "roles": ["User"]
}
```

**Email Already Exists (409 Conflict)**:
```json
{
  "message": "User with this email already exists"
}
```

#### Business Rules

- Email must be unique (409 if already registered)
- Email must be valid format
- Full name must be 2-200 characters
- Password must be at least 6 characters
- New users automatically get "User" role

#### Example cURL

```bash
curl -X POST https://localhost:7071/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

---

## Reservation Endpoints

### 3. Create Reservation

**Endpoint**: `POST /api/v1/reservations`

**Authentication**: Required (Bearer token)

Creates a new reservation for a customer. The reservation is initially created in "Created" status and must be confirmed before it becomes active.

#### Request

**Headers**:
```
Content-Type: application/json
Authorization: Bearer {accessToken}
```

**Body**:
```json
{
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "startDate": "2026-02-15T14:00:00Z",
  "endDate": "2026-02-20T10:00:00Z"
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customerId` | UUID | Yes | Unique identifier of the customer making the reservation. Must be a valid non-empty GUID. |
| `startDate` | DateTime (ISO 8601) | Yes | Date and time when the reservation begins. Must be in the future (at least 1 day from now). Must be before `endDate`. |
| `endDate` | DateTime (ISO 8601) | Yes | Date and time when the reservation ends. Must be after `startDate`. Cannot be more than 365 days in the future. |

#### Response

**Success (201 Created)**:
```json
{
  "success": true,
  "reservationId": "660f9511-f40c-52e5-b827-557766551111",
  "status": "Created",
  "errorMessage": null
}
```

**Validation Error (400 Bad Request)**:
```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Reservation start date must be at least 1 day in the future"
}
```

**Server Error (500 Internal Server Error)**:
```json
{
  "errors": [
    {
      "message": "An unexpected error occurred while processing your request"
    }
  ]
}
```

#### Business Rules

- `customerId` must be a valid, non-empty GUID
- `startDate` must be at least 1 day in the future from the current time
- `endDate` must be after `startDate`
- The maximum reservation duration is 365 days
- Validation errors are returned with a 400 status code

#### Example cURL

```bash
curl -X POST https://localhost:7071/api/v1/reservations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2026-02-15T14:00:00Z",
    "endDate": "2026-02-20T10:00:00Z"
  }'
```

#### HTTP Status Codes

| Code | Meaning | When | Example |
|------|---------|------|---------|
| `201` | Created | Reservation successfully created | New reservation returned with location header |
| `400` | Bad Request | Validation failed or invalid input | Invalid date range, empty GUID, etc. |
| `500` | Internal Server Error | Database or unexpected error | Database connection failure |

---

### 4. Confirm Reservation

**Endpoint**: `POST /api/v1/reservations/{id}/confirm`

**Authentication**: Required (Bearer token)

Transitions a reservation from "Created" status to "Confirmed" status. Only reservations in "Created" status can be confirmed.

#### Request

**URL Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | Unique identifier of the reservation to confirm |

**Headers**:
```
Content-Type: application/json
Authorization: Bearer {accessToken}
```

**Body**: Empty (no request body required)

#### Response

**Success (200 OK)**:
```json
{
  "success": true,
  "reservationId": "660f9511-f40c-52e5-b827-557766551111",
  "status": "Confirmed",
  "errorMessage": null
}
```

**Not Found (404 Not Found)**:
```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Reservation not found"
}
```

**Business Rule Violation (400 Bad Request)**:
```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Only Created reservations can be confirmed"
}
```

#### Business Rules

- Reservation must exist (404 if not found)
- Only reservations in "Created" status can be confirmed
- Cannot confirm already confirmed reservations
- Cannot confirm cancelled reservations
- A confirmed reservation cannot be reverted back to "Created" status

#### Example cURL

```bash
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/confirm \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

#### HTTP Status Codes

| Code | Meaning | When | Example |
|------|---------|------|---------|
| `200` | OK | Reservation successfully confirmed | Status changed to Confirmed |
| `400` | Bad Request | Business rule violated | Trying to confirm non-Created reservation |
| `404` | Not Found | Reservation doesn't exist | Invalid reservation ID |
| `500` | Internal Server Error | Database or unexpected error | Database connection failure |

---

### 5. Cancel Reservation

**Endpoint**: `POST /api/v1/reservations/{id}/cancel`

**Authentication**: Required (Bearer token)

Cancels an existing reservation with optional cancellation reason. Business rules apply based on reservation status and dates.

#### Request

**URL Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | Unique identifier of the reservation to cancel |

**Headers**:
```
Content-Type: application/json
```

**Body**:
```json
{
  "reason": "Guest requested cancellation"
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `reason` | string | No | Reason for cancellation. Recorded in audit trail. Can be null, but if provided must be non-empty. Examples: "Guest requested cancellation", "Double booking error", etc. Default: "No reason provided" |

#### Response

**Success (200 OK)**:
```json
{
  "success": true,
  "reservationId": "660f9511-f40c-52e5-b827-557766551111",
  "status": "Cancelled",
  "errorMessage": null
}
```

**Not Found (404 Not Found)**:
```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Reservation not found"
}
```

**Business Rule Violation (400 Bad Request)**:
```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Confirmed reservation cannot be cancelled after its start date"
}
```

#### Business Rules

- Reservation must exist (404 if not found)
- **Created reservations**: Can be cancelled at any time
- **Confirmed reservations**: Can only be cancelled before their start date
- **Already cancelled reservations**: Cannot be cancelled again
- Cancellation reason is optional but recommended for audit trail

#### Example cURL

```bash
# With reason
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/cancel \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{
    "reason": "Guest requested cancellation"
  }'

# Without reason
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/cancel \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -d '{}'
```

#### HTTP Status Codes

| Code | Meaning | When | Example |
|------|---------|------|---------|
| `200` | OK | Reservation successfully cancelled | Status changed to Cancelled |
| `400` | Bad Request | Business rule violated | Confirmed reservation past start date |
| `404` | Not Found | Reservation doesn't exist | Invalid reservation ID |
| `500` | Internal Server Error | Database or unexpected error | Database connection failure |

---

### 6. Get Reservations by Customer

**Endpoint**: `GET /api/v1/reservations?customerId={customerId}`

**Authentication**: Required (Bearer token)

Retrieves all reservations (created, confirmed, and cancelled) for a specific customer. Results are ordered by start date in descending order (newest first).

#### Request

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerId` | UUID | Yes | Unique identifier of the customer. Must be a valid non-empty GUID. |

**Headers**:
```
Content-Type: application/json
Authorization: Bearer {accessToken}
```

**Body**: Empty (GET request, no body)

#### Response

**Success (200 OK)**:
```json
[
  {
    "id": "660f9511-f40c-52e5-b827-557766551111",
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2026-02-20T14:00:00Z",
    "endDate": "2026-02-25T10:00:00Z",
    "status": "Confirmed",
    "createdAt": "2026-01-09T10:30:00Z",
    "modifiedAt": "2026-01-09T11:15:00Z"
  },
  {
    "id": "770g0612-g51d-63f6-c938-668877662222",
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2026-02-15T14:00:00Z",
    "endDate": "2026-02-20T10:00:00Z",
    "status": "Created",
    "createdAt": "2026-01-08T09:00:00Z",
    "modifiedAt": null
  }
]
```

**Empty Result (200 OK)**:
```json
[]
```

**Invalid Customer ID (400 Bad Request)**:
```json
{
  "error": "CustomerId must be a valid GUID, not empty GUID"
}
```

#### Response Fields

Each reservation object contains:

| Field | Type | Description |
|-------|------|-------------|
| `id` | UUID | Unique identifier for the reservation |
| `customerId` | UUID | Customer who made the reservation |
| `startDate` | DateTime (ISO 8601) | When the reservation starts |
| `endDate` | DateTime (ISO 8601) | When the reservation ends |
| `status` | string | Current status: "Created", "Confirmed", or "Cancelled" |
| `createdAt` | DateTime (ISO 8601) | When the reservation was created |
| `modifiedAt` | DateTime (ISO 8601) or null | When the reservation was last modified (null if only created) |

#### Filtering and Sorting

- **Filters by customer**: Only reservations for the specified `customerId` are returned
- **Includes all statuses**: Created, Confirmed, and Cancelled reservations
- **Sorted by start date**: Descending order (most recent start dates first)
- **Empty result**: Returns empty array if customer has no reservations

#### Example cURL

```bash
curl -X GET "https://localhost:7071/api/v1/reservations?customerId=550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

#### HTTP Status Codes

| Code | Meaning | When | Example |
|------|---------|------|---------|
| `200` | OK | Query executed successfully | Reservations returned (or empty array) |
| `400` | Bad Request | Invalid customer ID | Empty GUID or invalid format |
| `500` | Internal Server Error | Database or unexpected error | Database connection failure |

---

## Common Response Patterns

### Success Response

All successful mutation operations (POST) return this pattern:

```json
{
  "success": true,
  "reservationId": "660f9511-f40c-52e5-b827-557766551111",
  "status": "Created|Confirmed|Cancelled",
  "errorMessage": null
}
```

### Error Response

Failed operations return this pattern:

```json
{
  "success": false,
  "reservationId": null,
  "status": null,
  "errorMessage": "Descriptive error message"
}
```

### List Response

The GET endpoint returns an array of objects:

```json
[
  { /* ReservationDto object */ },
  { /* ReservationDto object */ }
]
```

---

## Error Handling

### HTTP Status Code Summary

| Code | Meaning | Common Causes |
|------|---------|---------------|
| `200` | OK | Successful GET or mutation operation |
| `201` | Created | Successful POST creating a new reservation |
| `400` | Bad Request | Validation error, invalid input, or business rule violation |
| `404` | Not Found | Reservation doesn't exist |
| `500` | Internal Server Error | Database error, unexpected exception, server issue |

### Common Error Messages

| Message | Cause | Solution |
|---------|-------|----------|
| `"CustomerId must be a valid GUID, not empty GUID"` | Empty GUID provided | Provide a valid non-empty UUID |
| `"Reservation start date must be at least 1 day in the future"` | Start date not far enough in future | Choose a date at least 1 day from now |
| `"Reservation not found"` | Invalid reservation ID | Verify the reservation ID exists |
| `"Only Created reservations can be confirmed"` | Trying to confirm non-Created reservation | Confirm only reservations in Created status |
| `"Confirmed reservation cannot be cancelled after its start date"` | Cancelling confirmed reservation past start date | Can only cancel before start date, create new one instead |

---

## Workflow Examples

### Example 1: Create and Confirm Reservation

```bash
# Step 1: Create reservation
curl -X POST https://localhost:7071/api/v1/reservations \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2026-02-15T14:00:00Z",
    "endDate": "2026-02-20T10:00:00Z"
  }'

# Response: 
# {
#   "success": true,
#   "reservationId": "660f9511-f40c-52e5-b827-557766551111",
#   "status": "Created"
# }

# Step 2: Confirm reservation
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/confirm \
  -H "Content-Type: application/json"

# Response:
# {
#   "success": true,
#   "reservationId": "660f9511-f40c-52e5-b827-557766551111",
#   "status": "Confirmed"
# }
```

### Example 2: Create and Cancel Reservation

```bash
# Step 1: Create reservation (same as above)

# Step 2: Cancel reservation with reason
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/cancel \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Guest needs to reschedule"
  }'

# Response:
# {
#   "success": true,
#   "reservationId": "660f9511-f40c-52e5-b827-557766551111",
#   "status": "Cancelled"
# }
```

### Example 3: Retrieve Customer's Reservations

```bash
# Get all reservations for a customer
curl -X GET "https://localhost:7071/api/v1/reservations?customerId=550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json"

# Response: Array of ReservationDto objects with various statuses
```

---

## Rate Limiting and Constraints

Currently, there are **no rate limits** implemented on the API. 

### Recommended Constraints for Production

- **Request timeout**: 30 seconds per request
- **Payload size limit**: 1 MB maximum
- **Rate limit**: Consider implementing 1,000 requests per minute per IP address
- **Connection pool**: Maximum 100 concurrent connections

---

## Testing the API

### Using Swagger UI

The API includes interactive Swagger/OpenAPI documentation:

1. Run the application: `dotnet run --project src/Reservation.API`
2. Open browser: `https://localhost:7071/swagger`
3. Use the interactive interface to test endpoints

### Using Postman

Import the endpoints into Postman for organized testing:

1. Create a new collection: "Reservation Management API"
2. Add each endpoint as a request
3. Set environment variables for `base_url` and `customerId`
4. Create test scripts to validate responses

### Using Insomnia

Similar to Postman, import the API endpoints and create requests:

```
POST https://localhost:7071/api/v1/reservations
GET https://localhost:7071/api/v1/reservations?customerId={{customerId}}
POST https://localhost:7071/api/v1/reservations/{{reservationId}}/confirm
POST https://localhost:7071/api/v1/reservations/{{reservationId}}/cancel
```

---

## API Design Patterns

### CQRS Pattern

The API implements **Command Query Responsibility Segregation (CQRS)**:

- **Commands** (POST): `CreateReservation`, `ConfirmReservation`, `CancelReservation`
  - Modify state
  - Return operation result with status
  - May emit domain events

- **Queries** (GET): `GetReservations`
  - Read data without side effects
  - Return denormalized read model
  - Optimized for query performance

### Vertical Slice Architecture

Each endpoint is organized as a vertical slice:

```
Endpoints/
├── ReservationEndpoints.cs
│   ├── MapCreateReservation()
│   ├── MapConfirmReservation()
│   ├── MapCancelReservation()
│   └── MapGetReservations()
└── [Request DTOs in ../DTOs/]
```

---

## Versioning

The current API version is **v1**, indicated by the `/api/v1` prefix in all endpoints.

### Future Versioning Strategy

- New major versions will introduce breaking changes (e.g., `/api/v2`)
- Old versions will be supported for a deprecation period
- Clients should pin API version in requests

---

## Authentication Details

For complete authentication setup, configuration, and security practices, see [AUTHENTICATION.md](AUTHENTICATION.md).

### Token Usage

After obtaining an access token from `/auth/login` or `/auth/register`, include it in all protected endpoint requests:

```
Authorization: Bearer {accessToken}
```

**Token Expiration**: 900 seconds (15 minutes)  
**Token Type**: JWT with HS256 signature  

---

## Documentation Updates

This documentation was last updated: **January 24, 2026**

For the latest code implementation, see:
- [AuthenticationEndpoints.cs](src/Reservation.API/Endpoints/AuthenticationEndpoints.cs)
- [ReservationEndpoints.cs](src/Reservation.API/Endpoints/ReservationEndpoints.cs)
- [Program.cs](src/Reservation.API/Program.cs)
- [DTOs](src/Reservation.API/DTOs/)

For architecture details, see:
- [README.md](README.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [AUTHENTICATION.md](AUTHENTICATION.md)
- [INDEX.md](INDEX.md) - Complete documentation guide
