# Observability Implementation Guide

## Overview

This document describes the production-grade observability features implemented in the Reservation Management API, including structured logging, correlation tracking, health checks, and centralized exception handling.

## Architecture Overview

The observability implementation follows these principles:
- **Structured Logging**: All logs include structured context for better searchability and analysis
- **Request Tracing**: Every request is assigned a unique correlation ID for distributed tracing
- **Centralized Error Handling**: Unified exception handling with standardized error responses
- **Health Checks**: Multiple health check endpoints for monitoring and orchestration
- **Correlation Context**: Request context is propagated through the entire request pipeline

## Components

### 1. Structured Logging with Serilog

**Technology Stack:**
- Serilog 4.2.0 - Structured logging framework
- Serilog.AspNetCore 9.0.0 - ASP.NET Core integration
- Serilog.Sinks.Console 6.0.0 - Console output with colorized templates
- Serilog.Sinks.File 6.0.0 - File-based logging with rolling intervals

**Configuration:** [appsettings.json](./src/Reservation.API/appsettings.json)

**Features:**
- Structured context injection via Serilog.Context
- Log enrichment with machine name, thread ID, and environment username
- Different log levels for development (Debug) and production (Information)
- Rolling file output with configurable retention (30 days production, 7 days development)
- Filtered verbosity for Microsoft internal components

**Production Configuration:**
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/reservation-api-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentUserName"],
    "MinimumLevel": {
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

**Log Output Example:**
```
[2024-01-15 14:23:45.123 +00:00] [INF] [Reservation.Application.Features.Reservations.CreateReservationHandler] Creating reservation for customer 550e8400-e29b-41d4-a716-446655440001... CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
[2024-01-15 14:23:45.145 +00:00] [DBG] [Reservation.Application.Features.Reservations.CreateReservationHandler] Reservation entity created with ID fb1847c3-a8c3-4f14-8d5f-3c4e2b8a1234 CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
[2024-01-15 14:23:45.167 +00:00] [INF] [Reservation.Application.Features.Reservations.CreateReservationHandler] Reservation persisted successfully CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
```

### 2. Correlation ID Middleware

**File:** [Middleware/CorrelationIdMiddleware.cs](./src/Reservation.API/Middleware/CorrelationIdMiddleware.cs)

**Purpose:** Implements distributed request tracing by assigning a unique correlation ID to each request.

**Features:**
- Extracts existing `X-Correlation-ID` header from incoming requests
- Generates new GUID if correlation ID not present
- Stores correlation ID in `HttpContext.Items` for access throughout request pipeline
- Adds correlation ID to response headers for client tracking
- Integrates with Activity for OpenTelemetry support (future implementation)

**Usage Example:**
```csharp
// In middleware pipeline (Program.cs)
app.UseMiddleware<CorrelationIdMiddleware>();

// In handlers/endpoints
var correlationId = context.HttpContext.GetCorrelationId();
```

**Request Flow:**
1. Request arrives → Middleware extracts/generates correlation ID
2. Stored in `HttpContext.Items["CorrelationId"]` for downstream access
3. Added to response headers: `X-Correlation-ID: {guid}`
4. All logs within request scope include `CorrelationId` in structured context

### 3. Global Exception Handling Middleware

**File:** [Middleware/GlobalExceptionHandlingMiddleware.cs](./src/Reservation.API/Middleware/GlobalExceptionHandlingMiddleware.cs)

**Purpose:** Centralized exception handling with standardized error responses and structured logging.

**Features:**
- Catches all unhandled exceptions in the request pipeline
- Logs exceptions with appropriate severity (Critical, Error, Warning)
- Returns standardized `ErrorResponse` DTO with error details
- Maps exception types to appropriate HTTP status codes
- Includes correlation ID in all error responses for tracing

**Exception Mapping:**
| Exception Type | HTTP Status | Error Type |
|---|---|---|
| `ArgumentException` | 400 Bad Request | `ValidationError` |
| `ValidationException` | 400 Bad Request | `ValidationError` |
| `InvalidOperationException` | 409 Conflict | `ConflictError` |
| `KeyNotFoundException` | 404 Not Found | `NotFoundError` |
| `UnauthorizedAccessException` | 401 Unauthorized | `UnauthorizedError` |
| `Exception` (default) | 500 Internal Server Error | `InternalServerError` |

**Response Format:**
```json
{
  "correlationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "error": {
    "type": "ValidationError",
    "message": "Reservation start date cannot be in the past",
    "timestamp": "2024-01-15T14:23:45.1234567Z",
    "details": [
      {
        "field": "startDate",
        "message": "Start date must be in the future"
      }
    ]
  }
}
```

**Logging Behavior:**
- **Error Logs:** Logged with full exception stack trace for debugging
- **Correlation Tracking:** Each error is tagged with request correlation ID
- **Context Preservation:** Original request headers and path included in logs

### 4. Health Check Endpoints

**File:** [Endpoints/HealthEndpoints.cs](./src/Reservation.API/Endpoints/HealthEndpoints.cs)

**Purpose:** Provides multiple health check endpoints for different monitoring scenarios.

**Endpoints:**

#### `/health/live` - Liveness Probe
- **Purpose:** Indicates if the application process is running
- **Used by:** Container orchestration platforms (Kubernetes, Docker Swarm)
- **Action:** If unhealthy, pod is restarted
- **Response:** Simple `{"status":"alive"}` with memory usage

```bash
curl http://localhost:5000/health/live
{
  "status": "alive",
  "timestamp": "2024-01-15T14:23:45.1234567Z",
  "uptime": 256
}
```

#### `/health/ready` - Readiness Probe
- **Purpose:** Indicates if application is ready to accept traffic
- **Used by:** Load balancers to route traffic
- **Action:** If unhealthy, traffic is routed away but pod isn't restarted
- **Response:** Status with all dependencies health included

```bash
curl http://localhost:5000/health/ready
{
  "status": "ready",
  "timestamp": "2024-01-15T14:23:45.1234567Z",
  "dependencies": ["self"]
}
```

#### `/health/detailed` - Detailed Diagnostics
- **Purpose:** Comprehensive health information for monitoring dashboards
- **Used by:** Monitoring systems (Prometheus, Grafana) and debugging
- **Response:** Full system info including .NET version, OS, processor count, all components

```bash
curl http://localhost:5000/health/detailed
{
  "status": "Healthy",
  "timestamp": "2024-01-15T14:23:45.1234567Z",
  "runtime": {
    "dotNetVersion": ".NET 8.0.1",
    "osDescription": "Windows 10.0.22621",
    "processorCount": 8
  },
  "components": {
    "self": {
      "status": "Healthy",
      "description": "Application is running",
      "duration": 1.234
    }
  }
}
```

**Kubernetes Integration Example:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: reservation-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: reservation-api:latest
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 10
```

### 5. Handler Logging

**Files:**
- [Features/Reservations/CreateReservationCommand.cs](./src/Reservation.Application/Features/Reservations/CreateReservationCommand.cs)
- [Features/Reservations/ConfirmReservationCommand.cs](./src/Reservation.Application/Features/Reservations/ConfirmReservationCommand.cs)
- [Features/Reservations/CancelReservationCommand.cs](./src/Reservation.Application/Features/Reservations/CancelReservationCommand.cs)
- [Features/Reservations/GetReservationsQuery.cs](./src/Reservation.Application/Features/Reservations/GetReservationsQuery.cs)

**Features:**
- Structured logging at each step of request processing
- Information level: Operation start/completion
- Debug level: Detailed processing steps
- Warning level: Non-critical issues
- Error level: Exceptions with context

**Example - CreateReservationHandler:**
```csharp
public async Task<Result<ReservationDto>> Handle(
    CreateReservationCommand command,
    CancellationToken cancellationToken)
{
    // Info: Operation started
    _logger.LogInformation(
        "Creating reservation for customer {CustomerId}...", 
        command.CustomerId);

    // Attempt to create domain entity
    var result = Reservation.Create(
        command.CustomerId,
        command.StartDate,
        command.EndDate);

    if (!result.IsSuccess)
    {
        // Warning: Validation failed
        _logger.LogWarning(
            "Domain validation failed for customer {CustomerId}: {Error}",
            command.CustomerId,
            result.Error);
        return result;
    }

    var reservation = result.Value;

    // Debug: Entity created
    _logger.LogDebug(
        "Reservation entity created with ID {ReservationId}",
        reservation.Id);

    // Info: Persistence started
    _logger.LogInformation(
        "Persisting reservation {ReservationId}...",
        reservation.Id);

    try
    {
        _repository.Add(reservation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Info: Successful completion
        _logger.LogInformation(
            "Reservation persisted successfully");

        return Result<ReservationDto>.Success(dto);
    }
    catch (Exception ex)
    {
        // Error: Unexpected failure
        _logger.LogError(
            ex,
            "Unexpected error while persisting reservation {ReservationId}",
            reservation.Id);
        throw;
    }
}
```

## Usage Examples

### Running the Application

```bash
cd src/Reservation.API
dotnet run

# Output shows structured logs
[2024-01-15 14:23:45.123 +00:00] [INF] [Program] Starting Reservation Management API...
[2024-01-15 14:23:45.456 +00:00] [INF] [Program] Development environment detected - Swagger UI enabled at /swagger
[2024-01-15 14:23:45.789 +00:00] [INF] [Program] Endpoint mapping completed
[2024-01-15 14:23:46.012 +00:00] [INF] [Program] Reservation Management API is ready to accept requests
```

### Querying Health Endpoints

```bash
# Liveness check
curl -X GET http://localhost:5000/health/live

# Readiness check
curl -X GET http://localhost:5000/health/ready

# Detailed diagnostics
curl -X GET http://localhost:5000/health/detailed
```

### Creating a Reservation with Correlation ID

```bash
# Make request with custom correlation ID
curl -X POST http://localhost:5000/api/reservations \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: custom-trace-id-12345" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2024-02-01T10:00:00Z",
    "endDate": "2024-02-05T10:00:00Z"
  }'

# Response includes correlation ID
{
  "correlationId": "custom-trace-id-12345",
  "data": { ... }
}

# If no correlation ID provided, one is generated automatically
# Response would include generated correlation ID
```

### Accessing Logs

**Console Output (Development):**
```bash
# Real-time structured logs with colors and details
[2024-01-15 14:23:45.123 +00:00] [INF] [CreateReservationHandler] Creating reservation for customer 550e... CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
```

**File Output (Production):**
```bash
# Rolling daily log files
logs/
├── reservation-api-20240115.txt
├── reservation-api-20240114.txt
└── reservation-api-20240113.txt

# Query specific logs
grep "CorrelationId=f47ac10b" logs/reservation-api-20240115.txt
```

## Monitoring and Alerting

### Key Metrics to Monitor

1. **Request Rate:**
   - Requests per second
   - Error rate percentage

2. **Latency:**
   - P50, P95, P99 response times
   - Timeout occurrences

3. **Health Status:**
   - Liveness checks passing
   - Readiness checks passing
   - Dependency availability

4. **Error Distribution:**
   - By error type (validation, not found, conflicts, etc.)
   - By endpoint
   - By correlation ID for debugging

### Alerting Strategies

```yaml
# Example Prometheus alert rules
alerts:
  - name: HighErrorRate
    expr: rate(errors_total[5m]) > 0.05
    message: "Error rate > 5%"
  
  - name: HealthCheckFailing
    expr: health_check_status == 0
    message: "Health check failed"
  
  - name: HighResponseTime
    expr: response_time_p95 > 1000  # ms
    message: "P95 response time > 1s"
```

## Performance Considerations

1. **Logging Overhead:**
   - Structured logging has minimal performance impact (~1-2%)
   - File I/O is asynchronous in Serilog
   - Use appropriate log levels in production (Information, not Debug)

2. **Correlation ID:**
   - Lightweight GUID generation (< 1µs)
   - Minimal memory footprint
   - No database impact

3. **Health Checks:**
   - `/health/live`: ~1ms (no I/O)
   - `/health/ready`: ~5ms (checks dependencies)
   - `/health/detailed`: ~10ms (full diagnostics)

## Best Practices

1. **Logging:**
   - Use appropriate log levels
   - Include context in messages (IDs, names, etc.)
   - Avoid logging sensitive data (passwords, tokens)
   - Use structured data instead of string interpolation

2. **Correlation Tracking:**
   - Always propagate correlation IDs to downstream services
   - Include in all cross-service calls
   - Log correlation ID with every significant operation

3. **Error Handling:**
   - Let global middleware handle unexpected exceptions
   - Log validation errors as warnings, not errors
   - Include actionable information in error messages

4. **Health Checks:**
   - Keep liveness checks minimal (no I/O)
   - Include all critical dependencies in readiness check
   - Use detailed endpoint for troubleshooting only

## Troubleshooting

### Issue: Logs not appearing
**Solution:**
- Check MinimumLevel in appsettings.json
- Verify environment variable ASPNETCORE_ENVIRONMENT
- Check log file path permissions

### Issue: Missing Correlation IDs
**Solution:**
- Verify CorrelationIdMiddleware is registered first in pipeline
- Check that clients are including X-Correlation-ID header
- Middleware generates one automatically if not provided

### Issue: Health check returning 503
**Solution:**
- Check specific component status in `/health/detailed`
- Verify database connection string
- Check for network connectivity issues

## Future Enhancements

1. **OpenTelemetry Integration:**
   - Distributed tracing across microservices
   - Metrics collection (Prometheus)
   - Custom traces for business operations

2. **Elasticsearch Integration:**
   - Centralized log aggregation
   - Kibana dashboards for analysis
   - Log search and filtering

3. **Custom Metrics:**
   - Business operation metrics
   - Domain event tracking
   - Performance profiling

4. **Log Sampling:**
   - Reduce log volume for high-traffic scenarios
   - Intelligent sampling based on error rate
   - Per-customer sampling for debugging

## References

- [Serilog Documentation](https://serilog.net/)
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)
- [ASP.NET Core Health Checks](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/monitor-app-health)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
