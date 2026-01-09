# API Endpoints Documentation

Complete reference guide for all REST API endpoints in the Reservation Management System.

**API Version**: v1  
**Base URL**: `/api/v1`  
**Content-Type**: `application/json`  
**Authentication**: Not implemented (open API)

---

## Overview

The Reservation Management API provides endpoints for creating, managing, and querying reservations. The API follows RESTful principles and uses HTTP status codes to indicate operation success or failure.

### Quick Reference Table

| Method | Endpoint | Description | Status Code |
|--------|----------|-------------|-------------|
| `POST` | `/reservations` | Create a new reservation | 201 Created |
| `POST` | `/reservations/{id}/confirm` | Confirm a reservation | 200 OK |
| `POST` | `/reservations/{id}/cancel` | Cancel a reservation | 200 OK |
| `GET` | `/reservations?customerId={id}` | Get reservations by customer | 200 OK |

---

## Endpoints

### 1. Create Reservation

**Endpoint**: `POST /api/v1/reservations`

Creates a new reservation for a customer. The reservation is initially created in "Created" status and must be confirmed before it becomes active.

#### Request

**Headers**:
```
Content-Type: application/json
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

### 2. Confirm Reservation

**Endpoint**: `POST /api/v1/reservations/{id}/confirm`

Transitions a reservation from "Created" status to "Confirmed" status. Only reservations in "Created" status can be confirmed.

#### Request

**URL Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | Unique identifier of the reservation to confirm |

**Headers**:
```
Content-Type: application/json
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
  -H "Content-Type: application/json"
```

#### HTTP Status Codes

| Code | Meaning | When | Example |
|------|---------|------|---------|
| `200` | OK | Reservation successfully confirmed | Status changed to Confirmed |
| `400` | Bad Request | Business rule violated | Trying to confirm non-Created reservation |
| `404` | Not Found | Reservation doesn't exist | Invalid reservation ID |
| `500` | Internal Server Error | Database or unexpected error | Database connection failure |

---

### 3. Cancel Reservation

**Endpoint**: `POST /api/v1/reservations/{id}/cancel`

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
  -d '{
    "reason": "Guest requested cancellation"
  }'

# Without reason
curl -X POST https://localhost:7071/api/v1/reservations/660f9511-f40c-52e5-b827-557766551111/cancel \
  -H "Content-Type: application/json" \
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

### 4. Get Reservations by Customer

**Endpoint**: `GET /api/v1/reservations?customerId={customerId}`

Retrieves all reservations (created, confirmed, and cancelled) for a specific customer. Results are ordered by start date in descending order (newest first).

#### Request

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerId` | UUID | Yes | Unique identifier of the customer. Must be a valid non-empty GUID. |

**Headers**:
```
Content-Type: application/json
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
  -H "Content-Type: application/json"
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

## Documentation Updates

This documentation was last updated: **January 9, 2026**

For the latest code implementation, see:
- [ReservationEndpoints.cs](src/Reservation.API/Endpoints/ReservationEndpoints.cs)
- [Program.cs](src/Reservation.API/Program.cs)
- [DTOs](src/Reservation.API/DTOs/)

For architecture details, see:
- [README.md](README.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [DEVELOPMENT.md](DEVELOPMENT.md)
