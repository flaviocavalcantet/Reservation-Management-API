# Developer Quick Reference Guide

## Exception Handling

### When to Use Each Exception:

```csharp
// Domain Validation - Input data violates domain rules
throw new DomainValidationException(nameof(EndDate), "Invalid end date");

// Business Rule Violation - Valid state, but business logic rejects operation  
throw new BusinessRuleViolationException(
    "ConfirmedReservationCancellationRule",
    "Cannot cancel after start date");

// Invalid State - Operation not allowed in current aggregate state
throw new InvalidAggregateStateException(
    Status.Value,
    nameof(Confirm),
    "Only created reservations can be confirmed");

// Not Found - Requested resource doesn't exist
throw new AggregateNotFoundException(nameof(Reservation), id);

// Conflict - Resource already exists or constraint violated
throw new AggregateConflictException(
    nameof(Reservation),
    id.ToString(),
    "Reservation already exists");
```

## Query Specifications

### Using Built-in Specifications:

```csharp
// Single customer's reservations
var spec = new ReservationsByCustomerSpecification(customerId);
var reservations = await _repository.GetBySpecificationAsync(spec);

// Active reservations with pagination
var spec = new PaginatedCustomerReservationsSpecification(customerId, page, pageSize);
var active = await _repository.GetBySpecificationAsync(spec);

// Confirmed reservations for customer
var spec = new ConfirmedReservationsForCustomerSpecification(customerId);
var confirmed = await _repository.GetBySpecificationAsync(spec);
```

### Creating Custom Specifications:

```csharp
public class MyCustomSpecification : Specification<Reservation>
{
    public MyCustomSpecification(Guid customerId, DateTime fromDate)
    {
        // Set filtering criteria
        Criteria = r => r.CustomerId == customerId && r.CreatedAt >= fromDate;
        
        // Include related data if needed
        // AddInclude(r => r.Items);
        
        // Apply ordering
        ApplyOrderByDescending(r => r.CreatedAt);
        
        // Apply paging
        ApplyPaging(skip: 0, take: 10);
    }
}
```

## Logging Best Practices

### Use Structured Logging:

```csharp
// ✅ Good - Structured properties
_logger.LogInformation(
    "Reservation {ReservationId} created for customer {CustomerId}",
    reservation.Id,
    reservation.CustomerId);

// ❌ Avoid - String interpolation
_logger.LogInformation($"Reservation {reservation.Id} created");

// ✅ Good - Exception with context
_logger.LogError(
    ex,
    "Failed to confirm reservation {ReservationId}. Error code: {ErrorCode}",
    reservationId,
    domainException.ErrorCode);
```

### Logging Levels:

- **DEBUG**: Detailed flow information for troubleshooting
- **INFO**: Important business events (successful operations)
- **WARNING**: Expected business rule violations, recoverable errors
- **ERROR**: Unexpected errors, needs investigation
- **CRITICAL**: System-level failures

## Result Pattern

### Returning Operation Results:

```csharp
// Success with data
return Result<ReservationDto>.Success(dto);

// Failure with error message
return Result<ReservationDto>.Failure("Reservation not found");

// Pattern matching
result.Match(
    onSuccess: dto => Ok(dto),
    onFailure: error => BadRequest(error)
);
```

## Validation in Handlers

### Expected Usage in Handler Catch:

```csharp
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
{
    try 
    {
        // Business logic
    }
    catch (AggregateNotFoundException ex)
    {
        // Handle not found - 404
        _logger.LogWarning(ex.Message);
        return ToErrorResult(ex.Message);
    }
    catch (InvalidAggregateStateException ex)
    {
        // Handle invalid state - 409
        _logger.LogWarning("Invalid state: {CurrentState}", ex.CurrentState);
        return ToErrorResult(ex.Message);
    }
    catch (BusinessRuleViolationException ex)
    {
        // Handle business rule - 422
        _logger.LogWarning("Rule {RuleName} violated", ex.RuleName);
        return ToErrorResult(ex.Message);
    }
    catch (DomainException ex)
    {
        // Catch-all for domain errors
        _logger.LogError(ex, "Domain error {ErrorCode}", ex.ErrorCode);
        return ToErrorResult(ex.Message);
    }
}
```

## API Error Responses

### HTTP Status Code Mapping:

| Exception Type | HTTP Code | Meaning |
|---|---|---|
| DomainValidationException | 400 | Bad Request (invalid input) |
| BusinessRuleViolationException | 422 | Unprocessable Entity (conflicts with rules) |
| InvalidAggregateStateException | 409 | Conflict (invalid state transition) |
| AggregateNotFoundException | 404 | Not Found (resource doesn't exist) |
| AggregateConflictException | 409 | Conflict (resource conflict) |
| ValidationException | 400 | Bad Request |
| UnauthorizedAccessException | 401 | Unauthorized |
| Generic Exception | 500 | Internal Server Error |

## Command Handler Template

```csharp
public class MyCommandHandler : ICommandHandler<MyCommand, MyResponse>
{
    private readonly IRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MyCommandHandler> _logger;

    public MyCommandHandler(IRepository repository, IUnitOfWork unitOfWork, ILogger<MyCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MyResponse> Handle(MyCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing command {CommandType}", typeof(MyCommand).Name);

        try
        {
            // 1. Load or create aggregate
            var aggregate = await _repository.GetByIdAsync(command.Id, cancellationToken);
            
            // 2. Business operation
            aggregate.DoSomething(command.Data);
            
            // 3. Persist
            await _repository.UpdateAsync(aggregate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Command processed successfully");
            return MapToResponse(aggregate);
        }
        catch (AggregateNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return ErrorResponse(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error: {ErrorCode}", ex.ErrorCode);
            return ErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return ErrorResponse("An unexpected error occurred");
        }
    }
}
```

## Testing Examples

### Testing with Specifications:

```csharp
[Fact]
public async Task GetReservations_WithCustomerSpecification_ReturnsCustomerReservations()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var spec = new ReservationsByCustomerSpecification(customerId);
    
    // Act
    var result = await _repository.GetBySpecificationAsync(spec);
    
    // Assert
    result.Should().AllSatisfy(r => r.CustomerId.Should().Be(customerId));
}
```

### Testing Exception Handling:

```csharp
[Fact]
public void Confirm_WhenInvalidState_ThrowsInvalidAggregateStateException()
{
    // Arrange
    var reservation = CreateReservation(status: Confirmed);
    
    // Act & Assert
    var ex = Assert.Throws<InvalidAggregateStateException>(() => reservation.Confirm());
    ex.CurrentState.Should().Be(Confirmed.Value);
    ex.ErrorCode.Should().Be("INVALID_STATE");
}
```

## Common Patterns

### Creating a Reservation (Happy Path):
```csharp
// Command -> Handler
var command = new CreateReservationCommand(customerId, startDate, endDate);
var result = await mediator.Send(command);

// Handler Flow:
// 1. Validation Behavior validates command
// 2. Logging Behavior logs start
// 3. Handler creates aggregate (throws DomainValidationException if invalid)
// 4. Handler persists via repository
// 5. UnitOfWork commits transaction
// 6. Logging Behavior logs completion
// 7. Return response DTO
```

### Updating Status (State Transition):
```csharp
// Command -> Handler
var command = new ConfirmReservationCommand(reservationId);
var result = await mediator.Send(command);

// Handler Flow:
// 1. Load aggregate from repository
// 2. Call business method (throws if invalid state)
// 3. Persist changes
// 4. Return updated state
// 5. Events published by infrastructure
```

## Anti-Patterns to Avoid

❌ **Don't**: Catch generic `Exception` at domain layer
✅ **Do**: Catch specific domain exceptions

❌ **Don't**: Use magic strings for error messages
✅ **Do**: Create constants or error codes

❌ **Don't**: Pass DbContext to domain layer
✅ **Do**: Use repository abstraction

❌ **Don't**: Mix business logic with infrastructure
✅ **Do**: Keep layers separated

❌ **Don't**: Log everything in one handler
✅ **Do**: Use logging behavior pipeline

---

## Useful Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName -p src/Reservation.Infrastructure -s src/Reservation.API

# Update database
dotnet ef database update -p src/Reservation.Infrastructure -s src/Reservation.API
```

---

**Last Updated**: January 21, 2026  
**Framework**: .NET 8  
**Architecture**: Clean Architecture + DDD + CQRS
