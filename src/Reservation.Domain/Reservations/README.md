# Reservation Domain Model - Tactical DDD Implementation

## Overview

The Reservation domain model has been implemented using Tactical Domain-Driven Design patterns. This ensures business logic is properly encapsulated in the domain layer with **zero framework dependencies**.

## Domain Components

### 1. ReservationStatus (Value Object)

**File**: `Reservation.Domain/Reservations/ReservationStatus.cs`

**Purpose**: Represents the lifecycle state of a reservation using a type-safe value object.

**Characteristics**:
- ✅ Immutable (cannot be changed after creation)
- ✅ Value-based equality (compared by state, not reference)
- ✅ No identity (no ID property)
- ✅ Encapsulates validation logic
- ✅ Prevents invalid states (only valid statuses can exist)

**Valid Statuses**:
```
Created   → Initial state when reservation is first created
Confirmed → State after reservation is confirmed by system/admin
Cancelled → Terminal state - reservation is cancelled
```

**Business Rules Encoded**:
- Only "Created" status can transition to "Confirmed"
- Only non-"Cancelled" reservations can be cancelled
- "Cancelled" is terminal (cannot change from it)

**Type Safety Example**:
```csharp
// ❌ Impossible - no way to create invalid status
var invalid = new ReservationStatus("InvalidStatus"); // Private constructor

// ✅ Only valid states possible
var status = ReservationStatus.Created;
status = ReservationStatus.Confirmed;
status = ReservationStatus.Cancelled;

// ✅ Query business rules from status
if (status.CanBeConfirmed) { /* ... */ }
if (status.CanBeCancelled) { /* ... */ }
```

---

### 2. Reservation (Aggregate Root)

**File**: `Reservation.Domain/Reservations/Reservation.cs`

**Purpose**: Core business entity managing the reservation lifecycle.

**Aggregate Boundary**: 
- Everything needed to manage a complete reservation is contained here
- External code cannot directly modify internal state
- All changes go through defined business operations

**Properties**:
```csharp
Id              // Unique identifier (assigned on creation)
CustomerId      // Who made the reservation
StartDate       // When reservation period starts
EndDate         // When reservation period ends
Status          // Current lifecycle state (value object)
CreatedAt       // Audit - when created
ModifiedAt      // Audit - when last modified
```

**Business Rules** (Enforced in Domain):

1. **Date Validity Rule**
   ```
   EndDate >= StartDate (always enforced)
   If violated: InvalidOperationException thrown
   ```

2. **Confirmation Rule**
   ```
   Only "Created" reservations can be confirmed
   If violated: InvalidOperationException thrown
   ```

3. **Cancellation Rule**
   ```
   Confirmed reservations cannot be cancelled after StartDate
   Already-cancelled reservations cannot be re-cancelled
   If violated: InvalidOperationException thrown
   ```

**Factory Method** (Creation):
```csharp
// Only way to create a valid Reservation
var reservation = Reservation.Create(
    customerId: customerId,
    startDate: DateTime.UtcNow.AddDays(1),
    endDate: DateTime.UtcNow.AddDays(3)
);
```

**Business Operations** (Rich Behavior):

```csharp
// Confirm operation
reservation.Confirm();
// - Validates: reservation status must be "Created"
// - Changes: status to "Confirmed"
// - Emits: ReservationConfirmedEvent

// Cancel operation
reservation.Cancel(reason: "Guest requested cancellation");
// - Validates: not already cancelled, and if confirmed then must be before start date
// - Changes: status to "Cancelled"
// - Emits: ReservationCancelledEvent
```

**Query Methods** (Business Logic Queries):
```csharp
reservation.IsActive          // Not cancelled and not expired
reservation.HasStarted        // Reservation period has begun
reservation.HasEnded          // Reservation period has ended
reservation.DurationDays      // How many days the reservation covers
```

---

### 3. Domain Events

**File**: `Reservation.Domain/Reservations/ReservationEvents.cs`

**Purpose**: Record important business facts for audit trail and event-driven processing.

**Events Defined**:

#### ReservationCreatedEvent
- Raised when: New reservation is created
- Subscribers might: Send confirmation email, record in audit log, update statistics
- Data captured: ReservationId, CustomerId, StartDate, EndDate

#### ReservationConfirmedEvent
- Raised when: Reservation is confirmed
- Subscribers might: Send confirmation notification, block calendar slots, update inventory
- Data captured: ReservationId, ConfirmedAt timestamp

#### ReservationCancelledEvent
- Raised when: Reservation is cancelled
- Subscribers might: Process refund, send cancellation notice, release booked slots
- Data captured: ReservationId, CancelledAt timestamp, cancellation reason

**Event Benefits**:
- ✅ Complete audit trail of what happened
- ✅ Enables event-driven architecture (side effects don't block business logic)
- ✅ Easy to add new features (new event subscribers) without modifying domain
- ✅ Supports event sourcing patterns

---

### 4. IReservationRepository (Repository Interface)

**File**: `Reservation.Domain/Reservations/IReservationRepository.cs`

**Purpose**: Defines data access contract without exposing implementation details.

**Base Operations** (from IRepository):
- `AddAsync(aggregate)` - Create new reservation
- `GetByIdAsync(id)` - Retrieve specific reservation
- `UpdateAsync(aggregate)` - Save changes
- `DeleteAsync(aggregate)` - Remove reservation
- `ExistsAsync(id)` - Check if exists

**Specialized Operations** (Reservation-specific):
```csharp
GetByCustomerIdAsync(customerId)
    // Find all reservations for a customer
    // Use case: Show customer their reservations

GetByDateRangeAsync(startDate, endDate)
    // Find reservations within a date range
    // Use case: Calendar view, availability checking

GetConflictingReservationsAsync(startDate, endDate)
    // Find confirmed reservations that overlap a date range
    // Critical for: Preventing double-bookings

CountActiveByCustomerAsync(customerId)
    // Count non-cancelled, non-expired reservations
    // Use case: Analytics, quota enforcement
```

---

## Domain Logic Flow - Example: Creating and Confirming a Reservation

```
1. APPLICATION LAYER requests reservation creation
   Command: CreateReservationCommand(customerId, startDate, endDate)

2. DOMAIN LAYER creates aggregate with factory method
   Reservation.Create(customerId, startDate, endDate)
   ├─ Validates: endDate >= startDate
   ├─ Sets: Id, Status=Created, CreatedAt
   └─ Emits: ReservationCreatedEvent
       └─ Result: New Reservation aggregate with state

3. APPLICATION LAYER persists aggregate
   await repository.AddAsync(reservation)
   └─ Result: Saved to database

4. INFRASTRUCTURE LAYER publishes domain events
   await eventPublisher.PublishAsync(reservation)
   └─ Result: Event subscribers notified (emails, logging, etc.)

5. APPLICATION LAYER handles confirmation request
   Command: ConfirmReservationCommand(reservationId)

6. DOMAIN LAYER confirms reservation
   reservation = await repository.GetByIdAsync(reservationId)
   reservation.Confirm()
   ├─ Validates: Status.CanBeConfirmed (must be "Created")
   ├─ Sets: Status=Confirmed, ModifiedAt=now
   └─ Emits: ReservationConfirmedEvent

7. APPLICATION LAYER persists changes
   await repository.UpdateAsync(reservation)
   await unitOfWork.SaveChangesAsync()

8. INFRASTRUCTURE LAYER publishes event
   └─ Result: Subscribers notified of confirmation
```

---

## Key Design Decisions

### 1. Value Object for Status (vs Enum)
**Why Value Object instead of C# enum?**
- ✅ Encapsulates validation logic: `CanBeConfirmed`, `CanBeCancelled`
- ✅ Consistent with DDD patterns
- ✅ Can add behavior without modifying other code
- ✅ Database-friendly (persists as string, not integer)
- ✅ Better for future enhancements

### 2. Aggregate Root Pattern for Reservation
**Why AggregateRoot?**
- ✅ Clear consistency boundary (all reservation data managed together)
- ✅ Domain transactions at aggregate level
- ✅ Prevents inconsistent states
- ✅ Enables event emission
- ✅ Testable without database

### 3. Factory Methods for Creation
**Why Reservation.Create() instead of constructor?**
- ✅ Enforces validation on creation
- ✅ Ensures domain events are always emitted
- ✅ Prevents partially initialized objects
- ✅ Clear business intent (Create is domain language)
- ✅ Easy to extend with more creation scenarios

### 4. Business Operations (Confirm, Cancel)
**Why methods instead of property setters?**
- ✅ Validates all business rules before state change
- ✅ Emits appropriate domain events
- ✅ Impossible to set invalid state
- ✅ Documents business operations clearly
- ✅ Makes refusal to complete explicit

### 5. Domain Events
**Why emit events from domain?**
- ✅ Complete audit trail without coupling to audit service
- ✅ Side effects (emails, notifications) don't block business logic
- ✅ Easy to add new features (new event subscribers)
- ✅ Supports event sourcing and eventual consistency patterns
- ✅ Decouples domain from infrastructure concerns

---

## Testing Domain Logic

The domain is completely framework-independent, making it trivial to test:

```csharp
// ✅ Unit test - no database, no mocks needed
[Fact]
public void Create_WithValidDates_CreatesReservation()
{
    var customerId = Guid.NewGuid();
    var startDate = DateTime.UtcNow.AddDays(1);
    var endDate = startDate.AddDays(2);

    var reservation = Reservation.Create(customerId, startDate, endDate);

    Assert.NotNull(reservation);
    Assert.Equal(ReservationStatus.Created, reservation.Status);
    Assert.Single(reservation.GetDomainEvents());
}

// ✅ Unit test - business rule enforcement
[Fact]
public void Create_WithEndDateBeforeStart_ThrowsException()
{
    var customerId = Guid.NewGuid();
    var startDate = DateTime.UtcNow.AddDays(2);
    var endDate = startDate.AddDays(-1); // Invalid!

    var ex = Assert.Throws<InvalidOperationException>(
        () => Reservation.Create(customerId, startDate, endDate));

    Assert.Contains("cannot be earlier than start date", ex.Message);
}

// ✅ Unit test - business operation validation
[Fact]
public void Cancel_ConfirmedReservationAfterStart_ThrowsException()
{
    var reservation = Reservation.Create(
        Guid.NewGuid(),
        DateTime.UtcNow.AddHours(-1), // Already started
        DateTime.UtcNow.AddHours(1));
    
    reservation.Confirm();

    var ex = Assert.Throws<InvalidOperationException>(() => reservation.Cancel());
    Assert.Contains("cannot be cancelled after its start date", ex.Message);
}
```

---

## Integration with Application Layer

The domain is used by Application layer handlers:

```csharp
// ✅ Application layer orchestrates domain and persistence
public class CreateReservationHandler : ICommandHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<CreateReservationResponse> Handle(
        CreateReservationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create aggregate (domain layer)
        var reservation = Reservation.Create(
            command.CustomerId,
            command.StartDate,
            command.EndDate); // Throws if invalid

        // 2. Persist (infrastructure layer)
        await _repository.AddAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Events are published automatically by infrastructure
        // (subscribers process side effects)

        return new CreateReservationResponse(reservation.Id);
    }
}
```

---

## No Framework Dependencies

The Domain layer contains:
- ✅ Domain logic and business rules
- ✅ Validation and invariant enforcement
- ✅ Rich behavior (Confirm, Cancel, Query methods)
- ✅ Domain events
- ✅ Interfaces (contracts only, no implementation)

The Domain layer does NOT contain:
- ❌ No using statements for data access frameworks
- ❌ No DbContext or Entity Framework references
- ❌ No HTTP or API frameworks
- ❌ No logging frameworks
- ❌ No external service calls

Result: **100% testable without external dependencies** ✅

---

## Next Steps

1. **Infrastructure Implementation**: Create `ReservationRepository` in Infrastructure layer
2. **Application Layer**: Create commands/queries for Create, Confirm, Cancel operations
3. **API Endpoints**: Create REST endpoints mapping to commands/queries
4. **Tests**: Write comprehensive unit tests for domain logic
5. **Database Schema**: EF Core entity configuration for Reservation table

All domain logic is complete and ready for integration!
