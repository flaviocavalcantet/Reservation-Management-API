# Reservation Management System - REST API

A production-ready REST API for managing reservations built with **.NET 8, ASP.NET Core, and Clean Architecture**.

> **📚 Documentation**: See [INDEX.md](INDEX.md) for complete documentation guide and quick navigation.

## Architecture Overview

This solution implements **Clean Architecture** with **Tactical Domain-Driven Design (DDD)** principles, ensuring separation of concerns, high testability, and maintainability.

### Project Structure

```
ReservationManagement/
├── src/
│   ├── Reservation.Domain/          # Core business logic (DDD)
│   ├── Reservation.Application/     # Use cases & orchestration (CQRS)
│   ├── Reservation.Infrastructure/  # Data access & external services
│   └── Reservation.API/             # REST endpoints & dependency injection
└── tests/
    └── Reservation.Tests/           # Unit and integration tests
```

## Layer Responsibilities

### 🎯 Domain Layer (`Reservation.Domain`)
**Core business logic with no external dependencies**

- **Value Objects**: Immutable, identity-less domain concepts (e.g., `Email`, `ReservationStatus`)
  - Compared by value, not reference
  - Encapsulate business rules
  - Provide type safety for primitives

- **Entities**: Mutable objects with unique identity (e.g., `Guest`, `TimeSlot`)
  - Have lifecycle and identity
  - Contain business logic specific to that entity

- **Aggregate Roots**: Top-level entities that enforce consistency boundaries (e.g., `Reservation`)
  - Guard access to child entities
  - Emit domain events
  - Define transaction boundaries

- **Domain Events**: Immutable records of something that happened in the domain
  - Enable event-driven architecture
  - Support event sourcing patterns
  - Decouple aggregates

- **Repositories**: Interfaces only (no implementation)
  - Define how aggregates are persisted
  - Keep domain isolated from persistence details

- **Unit of Work**: Interface for coordinating aggregate persistence
  - Ensures atomic operations across multiple aggregates
  - Manages transactions

```csharp
// Example: Value Object (identity-less)
public class Email : ValueObject
{
    public string Value { get; }
    // Compared by value, not by object reference
}

// Example: Aggregate Root (with identity)
public class Reservation : AggregateRoot
{
    public ReservationStatus Status { get; private set; }
    public IReadOnlyCollection<DomainEvent> GetDomainEvents()
    // Emits ReservationCreatedEvent, ReservationCancelledEvent, etc.
}
```

### 📋 Application Layer (`Reservation.Application`)
**Orchestration of domain logic using CQRS pattern**

- **Commands**: Write operations that change state
  - Implement business processes
  - Use repositories to load and save aggregates
  - Publish domain events after successful execution

- **Queries**: Read operations that retrieve data
  - Optimized for different read models
  - Don't modify state
  - Can query denormalized views or read-only databases

- **Handlers**: Process commands and queries
  - Enforce business rules
  - Coordinate cross-cutting concerns

- **Domain Event Publisher**: Publishes events to subscribers
  - Triggers side effects
  - Notifies other aggregates asynchronously

- **Pipeline Behaviors**: MediatR middleware for cross-cutting concerns
  - Logging behavior for request/response tracking
  - Validation behavior for input validation (FluentValidation-ready)

```csharp
// Example: Command (Write operation)
public record CreateReservationCommand(Guid GuestId, DateTime StartTime) 
    : ICommand<CreateReservationResponse>;

// Example: Query (Read operation)
public record GetReservationQuery(Guid ReservationId) 
    : IQuery<ReservationDto>;

// Example: Handler
public class CreateReservationHandler : ICommandHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly IRepository<Reservation, Guid> _repository;
    
    public async Task<CreateReservationResponse> Handle(CreateReservationCommand command, CancellationToken ct)
    {
        var reservation = Reservation.Create(command.GuestId, command.StartTime);
        await _repository.AddAsync(reservation, ct);
        // Domain events are automatically published
        return new CreateReservationResponse(reservation.Id);
    }
}
```

### 🔌 Infrastructure Layer (`Reservation.Infrastructure`)
**External dependencies and implementation details**

- **DbContext** (`ReservationDbContext`)
  - Implements EF Core's DbSet pattern
  - Configured for PostgreSQL
  - Implements the Unit of Work pattern

- **Entity Configurations**
  - EF Core fluent API configurations
  - Define table mappings, indexes, constraints
  - Handle value object conversions

- **Repository Implementations**
  - `GenericRepository<TAggregate, TId>`: Provides standard CRUD operations
  - Aggregate-specific repositories for complex queries

- **Dependency Registration**
  - Extension methods for clean DI setup
  - Keeps infrastructure decoupled from API layer

```csharp
// Example: Repository Implementation
public class ReservationRepository : GenericRepository<Reservation, Guid>
{
    public async Task<IEnumerable<Reservation>> GetByGuestAsync(Guid guestId)
    {
        return await DbSet.Where(r => r.GuestId == guestId).ToListAsync();
    }
}

// Example: Entity Configuration
public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>();
        builder.OwnsOne(r => r.TimeSlot);
    }
}
```

### 🌐 API Layer (`Reservation.API`)
**HTTP endpoints and dependency injection**

- **Endpoints**: Minimal APIs using Vertical Slice Architecture
  - Group endpoints by feature, not HTTP method
  - Map requests to commands/queries
  - Return HTTP responses

- **Program.cs**: Dependency Injection and middleware pipeline
  - Registers MediatR with behaviors
  - Configures DbContext with PostgreSQL
  - Sets up Swagger/OpenAPI
  - Auto-migrates database on startup (dev only)

```csharp
// Example: Endpoint Group
public class ReservationEndpoints : EndpointGroup
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/reservations");
        
        group.MapPost("/", CreateReservation)
             .WithName("CreateReservation")
             .WithOpenApi();
    }
    
    private async Task<IResult> CreateReservation(
        CreateReservationCommand command,
        IMediator mediator)
    {
        var result = await mediator.Send(command);
        return Results.Created($"/api/reservations/{result.Id}", result);
    }
}
```

## Design Principles

### SOLID Principles

| Principle | Implementation |
|-----------|-----------------|
| **S**ingle Responsibility | Each class has one reason to change |
| **O**pen/Closed | Open for extension via inheritance; closed for modification |
| **L**iskov Substitution | Implementations are substitutable for interfaces |
| **I**nterface Segregation | Clients depend on minimal, specific interfaces |
| **D**ependency Inversion | Depend on abstractions, not concrete implementations |

### DDD Tactical Patterns

| Pattern | Purpose | Example |
|---------|---------|---------|
| **Value Object** | Identity-less domain concept | `Email`, `Money`, `Status` |
| **Entity** | Object with unique identity | `Guest`, `TimeSlot` |
| **Aggregate Root** | Consistency boundary | `Reservation` (guards guest, time slot) |
| **Domain Event** | Records fact that occurred | `ReservationCreatedEvent` |
| **Repository** | Persistence abstraction | `IRepository<Reservation, Guid>` |
| **Unit of Work** | Atomic operation coordination | `IUnitOfWork`, `DbContext` |

### Architectural Constraints

```
API Layer
    ↓ depends on
Application Layer
    ↓ depends on
Domain Layer
    ↑ referenced by
Infrastructure Layer (via interfaces only)
```

**Key Rules:**
- Domain has NO dependencies on other layers
- Application depends ONLY on Domain
- Infrastructure implements interfaces defined in Domain
- API depends on Application (Infrastructure injected via DI)

## Project References

| From | To | Reason |
|------|-----|--------|
| Reservation.Application | Reservation.Domain | Uses domain logic |
| Reservation.Infrastructure | Reservation.Domain | Implements domain contracts |
| Reservation.Infrastructure | Reservation.Application | Implements application contracts |
| Reservation.API | Reservation.Application | Sends commands/queries |
| Reservation.API | Reservation.Infrastructure | Registers DI |
| Reservation.Tests | Reservation.Domain | Tests domain logic |
| Reservation.Tests | Reservation.Application | Tests use cases |

## Dependency Injection Configuration

The DI container is configured in `Program.cs` following this pattern:

```csharp
// MediatR for CQRS pattern
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(...);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// EF Core with PostgreSQL
builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Infrastructure services
builder.Services.AddInfrastructure(connectionString);
```

**Extension method** in `InfrastructureDependencies.cs` provides:
- Generic repository registration: `IRepository<TAggregate, TId>`
- Unit of Work: `IUnitOfWork` → DbContext
- Domain Event Publisher: `IDomainEventPublisher`

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **All** | .NET 8 | Runtime |
| **API** | ASP.NET Core 8 | Web framework |
| **API** | Swagger/OpenAPI | API documentation |
| **Application** | MediatR | CQRS pattern, pipeline behaviors |
| **Infrastructure** | EF Core 8 | ORM |
| **Infrastructure** | Npgsql | PostgreSQL driver |
| **Tests** | xUnit | Test framework |
| **Tests** | Moq | Mocking |
| **Tests** | FluentAssertions | Assertions |

## Future Evolution Support

This architecture is designed for future extensibility:

### Authentication & Authorization
```csharp
// Add to pipeline behaviors
config.AddOpenBehavior(typeof(AuthorizationBehavior<,>));

// Inject ICurrentUser into handlers
public class CreateReservationHandler
{
    private readonly ICurrentUser _currentUser;
    // Validates authorization in command handler
}
```

### Caching
```csharp
// Add caching behavior
config.AddOpenBehavior(typeof(CachingBehavior<,>));

// Decorate queries with cache policy
[Cacheable(Duration = 300)]
public class GetReservationQuery : IQuery<ReservationDto> { }
```

### Messaging (RabbitMQ, Azure Service Bus)
```csharp
// Domain event subscriber for async processing
public class ReservationCreatedEventHandler : 
    INotificationHandler<ReservationCreatedEvent>
{
    private readonly IMessagePublisher _messagePublisher;
    
    public async Task Handle(ReservationCreatedEvent @event, CancellationToken ct)
    {
        await _messagePublisher.PublishAsync(
            new SendConfirmationEmailCommand(@event.GuestId));
    }
}
```

### Event Sourcing
```csharp
// Store domain events as the source of truth
public class EventStoreRepository<T> : IRepository<T, Guid>
{
    // Save aggregate state as sequence of events
    // Replay events to reconstruct aggregate state
}
```

## File Structure

```
src/Reservation.Domain/
├── Abstractions/
│   ├── Entity.cs              # Base class for entities
│   ├── AggregateRoot.cs       # Base class for aggregate roots
│   ├── ValueObject.cs         # Base class for value objects
│   ├── DomainEvent.cs         # Base class for domain events
│   ├── IRepository.cs         # Repository interface
│   └── IUnitOfWork.cs         # Unit of Work interface
└── Common/                    # Placeholder for shared domain utilities

src/Reservation.Application/
├── Abstractions/
│   ├── ICommandHandler.cs     # Command handler interface
│   ├── IQueryHandler.cs       # Query handler interface
│   └── IDomainEventPublisher.cs
├── Behaviors/
│   ├── LoggingBehavior.cs     # Request/response logging
│   └── ValidationBehavior.cs  # Input validation
└── (Features will be organized by subdirectories)

src/Reservation.Infrastructure/
├── Persistence/
│   └── ReservationDbContext.cs
├── Repositories/
│   └── GenericRepository.cs
└── InfrastructureDependencies.cs

src/Reservation.API/
├── Endpoints/
│   └── EndpointGroup.cs       # Base for endpoint groups
├── Program.cs                 # DI and middleware setup
├── appsettings.json
└── appsettings.Development.json

tests/Reservation.Tests/
└── (Test files organized by layer being tested)
```

## Getting Started

### Prerequisites
- .NET 8 SDK or later
- PostgreSQL 12 or later
- Visual Studio 2022 or VS Code with C# extension

### Setup

1. **Clone and navigate to project**
   ```bash
   cd ReservationManagement
   ```

2. **Update PostgreSQL connection string**
   Edit `src/Reservation.API/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=ReservationManagement_Dev;Username=YOUR_USER;Password=YOUR_PASSWORD"
     }
   }
   ```

3. **Restore packages and build**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run migrations** (auto-runs on startup in development)
   ```bash
   dotnet run --project src/Reservation.API
   ```

5. **Access API documentation**
   ```
   https://localhost:7071/swagger
   ```

## API Endpoints

For complete API endpoint documentation including request/response examples, error handling, and workflow examples, see [API_ENDPOINTS.md](API_ENDPOINTS.md).

### Quick Endpoint Reference

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/v1/reservations` | Create a new reservation |
| `POST` | `/api/v1/reservations/{id}/confirm` | Confirm a reservation |
| `POST` | `/api/v1/reservations/{id}/cancel` | Cancel a reservation |
| `GET` | `/api/v1/reservations?customerId={id}` | Get reservations by customer |

## Code Organization Patterns

### Feature-Based Organization
```
src/Reservation.Application/Features/
├── Reservations/
│   ├── Create/
│   │   ├── CreateReservationCommand.cs
│   │   └── CreateReservationHandler.cs
│   ├── Get/
│   │   ├── GetReservationQuery.cs
│   │   └── GetReservationQueryHandler.cs
│   └── Cancel/
│       ├── CancelReservationCommand.cs
│       └── CancelReservationHandler.cs
```

### Domain Event Handling
```
// Domain event is defined in Domain layer
public class ReservationCreatedEvent : DomainEvent { }

// Handler is in Application layer
public class SendConfirmationEmailOnReservationCreated : 
    INotificationHandler<ReservationCreatedEvent>
{
    public async Task Handle(ReservationCreatedEvent @event, CancellationToken ct)
    {
        // Side effect: send email
    }
}
```

## Key Design Decisions

### 1. **Clean Architecture Layering**
- **Why**: Ensures separation of concerns and testability
- **Trade-off**: More projects and abstraction layers initially, but pays off with maintainability

### 2. **CQRS (Command Query Responsibility Segregation)**
- **Why**: Separates read and write models for independent optimization
- **Trade-off**: Requires eventual consistency patterns for complex scenarios

### 3. **Domain-Driven Design**
- **Why**: Business rules live in the domain, not scattered across layers
- **Trade-off**: Requires careful design of aggregates and bounded contexts

### 4. **Repository Pattern**
- **Why**: Abstracts persistence, enables testing with mocks
- **Trade-off**: Slightly more complex than direct DbContext usage

### 5. **Async/Await Throughout**
- **Why**: Provides scalability and proper resource utilization
- **Trade-off**: Requires understanding of async patterns and pitfalls

### 6. **Entity Framework Core with PostgreSQL**
- **Why**: Mature ORM with excellent tooling; PostgreSQL is production-grade
- **Trade-off**: Requires database migrations; less control than raw SQL

## Testing Strategy

This architecture supports multiple testing levels:

### Unit Tests
```csharp
[Fact]
public void Reservation_Create_WithValidData_ReturnsReservation()
{
    // Arrange
    var guestId = Guid.NewGuid();
    var startTime = DateTime.UtcNow.AddDays(1);
    
    // Act
    var reservation = Reservation.Create(guestId, startTime);
    
    // Assert
    Assert.NotNull(reservation);
    Assert.Single(reservation.GetDomainEvents());
}
```

### Integration Tests
```csharp
[Fact]
public async Task CreateReservationCommand_WithValidData_PersistsToDatabase()
{
    // Uses test database
    var handler = new CreateReservationHandler(_testRepository);
    var command = new CreateReservationCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
    
    var result = await handler.Handle(command, CancellationToken.None);
    
    Assert.NotEqual(Guid.Empty, result.Id);
}
```

## Monitoring & Observability

Ready for integration with:
- **Application Insights**: Telemetry and performance monitoring
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing
- **HealthChecks**: Application health endpoints

## Contributing

This codebase follows these principles:
- Clean code with clear intent
- Comprehensive comments for complex logic
- SOLID principles adherence
- Test-driven development encouraged
- Architecture decision records (ADRs) for significant changes

## License

[Your License Here]

---

**Build date**: 2026-01-08  
**Architecture Version**: 1.0  
**Target Framework**: .NET 8
