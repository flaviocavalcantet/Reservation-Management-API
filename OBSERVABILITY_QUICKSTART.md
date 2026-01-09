# Quick Start: Observability Features

## Starting the Application

```bash
cd src/Reservation.API
dotnet run
```

Expected output:
```
[09:16:22 INF] [Program] Reservation Management API is ready to accept requests
[09:16:23 INF] [Microsoft.Hosting.Lifetime] Now listening on: http://localhost:5000
```

## Testing Health Endpoints

### Liveness Check (Is the app running?)
```bash
curl http://localhost:5000/health/live
```

Response:
```json
{
  "status": "alive",
  "timestamp": "2024-01-15T14:23:45.1234567Z",
  "uptime": 256
}
```

### Readiness Check (Can it accept traffic?)
```bash
curl http://localhost:5000/health/ready
```

Response:
```json
{
  "status": "ready",
  "timestamp": "2024-01-15T14:23:45.1234567Z",
  "dependencies": ["self"]
}
```

### Detailed Diagnostics
```bash
curl http://localhost:5000/health/detailed
```

Response:
```json
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

## Creating a Reservation with Correlation ID

### With Custom Correlation ID
```bash
curl -X POST http://localhost:5000/api/reservations \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: my-trace-123" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2024-02-01T10:00:00Z",
    "endDate": "2024-02-05T10:00:00Z"
  }'
```

Response includes correlation ID in header:
```json
{
  "correlationId": "my-trace-123",
  "data": {
    "id": "...",
    "customerId": "...",
    ...
  }
}
```

### Without Correlation ID (Auto-generated)
```bash
curl -X POST http://localhost:5000/api/reservations \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2024-02-01T10:00:00Z",
    "endDate": "2024-02-05T10:00:00Z"
  }'
```

Response includes auto-generated correlation ID.

## Viewing Logs

### Console Output (Live)
Application logs appear in console with timestamps and structured context:
```
[14:23:45.123 +00:00] [INF] [CreateReservationHandler] Creating reservation for customer 550e... CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
[14:23:45.145 +00:00] [DBG] [CreateReservationHandler] Reservation entity created with ID... CorrelationId=f47ac10b-58cc-4372-a567-0e02b2c3d479
```

### File Output
Logs saved to rolling daily files:
```bash
# List log files
ls logs/

# Output:
# reservation-api-20240115.txt
# reservation-api-20240114.txt
# ...

# View logs for specific correlation ID
grep "f47ac10b-58cc-4372-a567-0e02b2c3d479" logs/reservation-api-20240115.txt

# View errors
grep "\[ERR\]" logs/reservation-api-20240115.txt

# View all info level logs
grep "\[INF\]" logs/reservation-api-20240115.txt
```

## Error Response Example

### Invalid Request (Validation Error)
```bash
curl -X POST http://localhost:5000/api/reservations \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: error-test" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2020-01-01T10:00:00Z",
    "endDate": "2020-01-05T10:00:00Z"
  }'
```

Response (400 Bad Request):
```json
{
  "correlationId": "error-test",
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

Both response and logs will include the correlation ID for tracing.

## Configuration

### Change Log Level

**Development (Debug all logs):**
```json
// appsettings.Development.json
"Serilog": {
  "MinimumLevel": "Debug"
}
```

**Production (Info level only):**
```json
// appsettings.json
"Serilog": {
  "MinimumLevel": "Information"
}
```

### Change Log File Location

```json
// appsettings.json
"Serilog": {
  "WriteTo": [
    {
      "Name": "File",
      "Args": {
        "path": "C:/var/log/reservation-api-.txt",  // Custom path
        "rollingInterval": "Day"
      }
    }
  ]
}
```

## Monitoring Patterns

### Check Application Liveness (Kubernetes)
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 30
```

### Check Application Readiness (Load Balancer)
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 10
```

### View Detailed Status (Diagnostics)
```bash
# Full health status
curl http://localhost:5000/health/detailed

# Filter with jq
curl -s http://localhost:5000/health/detailed | jq '.components'
```

## Troubleshooting

### No Logs Appearing?
1. Check `ASPNETCORE_ENVIRONMENT` variable is set correctly
2. Verify log file permissions: `logs/` directory writable
3. Check MinimumLevel in appsettings

### Missing Correlation IDs?
1. Verify CorrelationIdMiddleware is registered in Program.cs
2. Check that clients send `X-Correlation-ID` header
3. Middleware auto-generates if not provided

### Health Endpoint Returning 503?
1. Check `/health/detailed` for component-specific status
2. Verify database connection string
3. Check for network connectivity issues

## Common Log Queries

### Find all errors for a customer
```bash
grep "550e8400-e29b-41d4-a716-446655440000" logs/*.txt | grep "\[ERR\]"
```

### Find operation duration
```bash
# Start and end timestamps for correlation ID
grep "f47ac10b-58cc-4372-a567-0e02b2c3d479" logs/*.txt
```

### Find slow operations
```bash
# Find operations that lasted more than expected
grep "\[DBG\]" logs/*.txt | grep "completed"
```

### Count errors by type
```bash
grep "\[ERR\]" logs/*.txt | sort | uniq -c | sort -rn
```

## See Also

- [OBSERVABILITY.md](./OBSERVABILITY.md) - Comprehensive guide
- [OBSERVABILITY_IMPLEMENTATION.md](./OBSERVABILITY_IMPLEMENTATION.md) - Implementation details
- Program.cs - Application startup code
- appsettings.json - Serilog configuration
