# CQRS Application Layer Implementation Summary

## Overview
The Application layer implements a Command Query Responsibility Segregation (CQRS) pattern using MediatR for separating read and write operations. This document describes the implementation of 4 core use cases for the Reservation Management system.

## Architecture Pattern: CQRS

### Commands (Write Operations)
- **CreateReservationCommand**: Create a new reservation
- **ConfirmReservationCommand**: Transition reservation from Created → Confirmed
- **CancelReservationCommand**: Transition reservation to Cancelled state

### Queries (Read Operations)  
- **GetReservationsQuery**: Retrieve reservations for a customer

## Implementation Details

### 1. CreateReservation Use Case
**Files:**
- `Features/Reservations/CreateReservation/CreateReservationCommand.cs`

**Responsibility Flow:**
1. **Input**: CustomerId, StartDate, EndDate
2. **Validation**: MediatR pipeline behavior (enabled via ValidationBehavior)
3. **Business Logic**: 
   - Domain aggregate creates reservation via `Reservation.Create()` factory method
   - Domain enforces: EndDate >= StartDate, valid customer ID
4. **Persistence**: Repository adds aggregate, Unit of Work saves
5. **Output**: ReservationOperationResultDto with success/error status

**Error Handling:**
- InvalidOperationException (domain rules) → Returns error DTO with message
- Unexpected errors → Returns generic error DTO

---

### 2. ConfirmReservation Use Case
**Files:**
- `Features/Reservations/ConfirmReservation/ConfirmReservationCommand.cs`

**Responsibility Flow:**
1. **Input**: ReservationId
2. **Repository Query**: Load aggregate by ID
3. **Business Logic**:
   - Domain aggregate transitions via `Confirm()` method
   - Domain enforces: Only Created → Confirmed transitions allowed
4. **Persistence**: Repository updates aggregate, Unit of Work saves
5. **Output**: ReservationOperationResultDto with updated status

**Business Rules Enforced by Domain:**
- Status must be Created to confirm
- Raises InvalidOperationException if invalid transition attempted

---

### 3. CancelReservation Use Case
**Files:**
- `Features/Reservations/CancelReservation/CancelReservationCommand.cs`

**Responsibility Flow:**
1. **Input**: ReservationId, Reason (optional in domain)
2. **Repository Query**: Load aggregate by ID
3. **Business Logic**:
   - Domain aggregate transitions via `Cancel()` method
   - Domain enforces complex date-based rules:
     - Can only cancel before reservation start date (if confirmed)
     - Created reservations can always be cancelled
4. **Persistence**: Repository updates aggregate, Unit of Work saves
5. **Output**: ReservationOperationResultDto with success/error status

**Key Business Rule:**
```
Confirmed reservations cannot be cancelled after their start date
Raises InvalidOperationException with appropriate message
```

---

### 4. GetReservations Use Case (Query)
**Files:**
- `Features/Reservations/GetReservations/GetReservationsQuery.cs`

**Responsibility Flow:**
1. **Input**: CustomerId
2. **Repository Query**: `GetByCustomerIdAsync(customerId)`
3. **Mapping**: Domain entities → ReservationDto DTOs
4. **Output**: IEnumerable<ReservationDto> (read model for API)

**Characteristics:**
- Read-only operation (no validation or business logic)
- Returns cleaned DTOs (no domain implementation details exposed)
- Query handlers are simpler than command handlers

---

## DTOs (Data Transfer Objects)

### ReservationDto (Read Model)
Used for API responses:
```csharp
public record ReservationDto(
    Guid Id,
    Guid CustomerId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt
);
```

### ReservationOperationResultDto (Operation Result)
Wraps success/error responses:
```csharp
public record ReservationOperationResultDto(
    bool Success,
    Guid? ReservationId,
    string? Status,
    string? ErrorMessage
);
```

## Design Principles Applied

### 1. Separation of Concerns
- **Commands**: Write operations with business logic and validation
- **Queries**: Read operations with no side effects
- **Handlers**: Orchestrate domain operations and persistence

### 2. No Infrastructure Code in Handlers
- Handlers use repository and unit of work interfaces only
- EF Core implementation details confined to Infrastructure layer
- Enables easy testing and layer independence

### 3. Domain-Driven Design
- Domain aggregates enforce all business rules
- Handlers call domain methods, don't re-implement logic
- Exceptions from domain translated to DTOs for API clients

### 4. Error Handling Strategy
- Domain InvalidOperationException → User-friendly error DTO
- Unexpected exceptions → Generic error message (details in logs)
- Prevents leaking internal details to API clients

### 5. MediatR Pipeline Behaviors
Configured in API layer for cross-cutting concerns:
- **LoggingBehavior**: Request/response logging
- **ValidationBehavior**: Input validation (FluentValidation-ready)

## Interface Dependencies

Each handler depends on:
- **IRepository interface**: From Domain layer for aggregate access
- **IUnitOfWork interface**: From Domain layer for transaction management
- **DTOs**: From Application.DTOs namespace

```csharp
// Example: CreateReservationHandler dependencies
public CreateReservationHandler(
    IReservationRepository repository,  // Domain interface
    IUnitOfWork unitOfWork)              // Domain interface
{
    _repository = repository;
    _unitOfWork = unitOfWork;
}
```

## Next Steps: Integration Points

To use these handlers, API endpoints should:

1. **Inject MediatR**: `IMediator mediator`
2. **Send Command/Query**: `await mediator.Send(command/query)`
3. **Handle Response**: Convert DTO to HTTP response (200, 400, 500)
4. **Map Input**: API request body → Command/Query object

Example (future API endpoint):
```csharp
app.MapPost("/api/reservations", async (CreateReservationRequest request, IMediator mediator) =>
{
    var command = new CreateReservationCommand(
        request.CustomerId,
        request.StartDate,
        request.EndDate);
    
    var result = await mediator.Send(command);
    
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});
```

## Testing Strategy

### Unit Tests (Isolated Handler Testing)
- Mock IReservationRepository and IUnitOfWork
- Test success and error paths
- Verify correct business rules are enforced

### Integration Tests (with real Domain)
- Use real domain aggregates
- Verify handlers call domain methods correctly
- Test end-to-end orchestration

### Domain Tests
- Verify business rules in Reservation aggregate
- Test ReservationStatus value object transitions
- Validate domain event emissions

## Validation Approach

Currently, validation can be added via:

1. **MediatR Behavior Approach** (implemented):
   - Create validator using FluentValidation
   - ValidationBehavior checks before handler runs
   - Example:
   ```csharp
   public class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
   {
       public CreateReservationValidator()
       {
           RuleFor(x => x.StartDate).NotEmpty();
           RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
           RuleFor(x => x.CustomerId).NotEmpty();
       }
   }
   ```

2. **Domain Validation** (already implemented):
   - Domain aggregate validates in `Create()` factory
   - Throws InvalidOperationException on violation
   - Handler catches and returns error DTO

## File Organization

```
src/Reservation.Application/
├── Abstractions/
│   ├── ICommandHandler.cs
│   ├── IQueryHandler.cs
│   └── IDomainEventPublisher.cs
├── Behaviors/
│   ├── LoggingBehavior.cs
│   └── ValidationBehavior.cs
├── DTOs/
│   └── ReservationDto.cs
└── Features/Reservations/
    ├── CreateReservation/
    │   └── CreateReservationCommand.cs
    ├── ConfirmReservation/
    │   └── ConfirmReservationCommand.cs
    ├── CancelReservation/
    │   └── CancelReservationCommand.cs
    └── GetReservations/
        └── GetReservationsQuery.cs
```

This vertical slice organization groups all related code for a single use case together, improving discoverability and maintainability.

## Summary

✓ 3 Command handlers for write operations
✓ 1 Query handler for read operations
✓ CQRS separation with clean DTOs
✓ Error handling with domain exception translation
✓ Ready for MediatR pipeline behaviors (logging, validation)
✓ No infrastructure or EF Core code in Application layer
✓ Vertical slice feature-based organization
✓ Solution compiles successfully with no errors
