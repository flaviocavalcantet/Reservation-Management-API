# Development Guidelines

## Code Style and Standards

### Naming Conventions
- **Namespaces**: PascalCase, organized by layer then feature
  ```csharp
  Reservation.Domain.Reservations        // Feature-based
  Reservation.Application.Reservations.Create
  ```

- **Classes/Interfaces**: PascalCase
  - Interfaces start with `I`: `IRepository`, `IUnitOfWork`
  - Commands end with `Command`: `CreateReservationCommand`
  - Queries end with `Query`: `GetReservationQuery`
  - Handlers end with `Handler`: `CreateReservationHandler`

- **Methods/Properties**: PascalCase
- **Parameters/Local variables**: camelCase

### File Organization
```
Feature/
├── Domain/                 # Domain logic (if in separate folder)
│   └── Reservation.cs
├── Commands/              # Write operations
│   ├── CreateReservation/
│   │   ├── CreateReservationCommand.cs
│   │   └── CreateReservationHandler.cs
├── Queries/               # Read operations
│   ├── GetReservation/
│   │   ├── GetReservationQuery.cs
│   │   └── GetReservationQueryHandler.cs
└── Endpoints/             # REST mapping (API layer)
    ├── ReservationEndpoints.cs
    └── ReservationRequestDtos.cs
```

## Writing Domain Logic

### Value Objects
```csharp
/// <summary>
/// Email value object. Ensures email format is always valid.
/// </summary>
public class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));
        Value = value;
    }

    public static Email Create(string value) => new(value);

    private static bool IsValidEmail(string email)
    {
        // Validation logic
        return true;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}
```

### Entities
```csharp
/// <summary>
/// Guest aggregate root. Encapsulates guest data and business logic.
/// </summary>
public class Guest : AggregateRoot
{
    public Email Email { get; private set; }
    public string Name { get; private set; }

    private Guest() { } // EF Core constructor

    public static Guest Create(string name, string email)
    {
        var guest = new Guest
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = Email.Create(email),
            CreatedAt = DateTime.UtcNow
        };

        guest.AddDomainEvent(new GuestCreatedEvent(guest.Id, guest.Email.Value));
        return guest;
    }
}
```

### Domain Events
```csharp
/// <summary>
/// Raised when a reservation is created
/// </summary>
public class ReservationCreatedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid GuestId { get; }
    public DateTime StartTime { get; }

    public ReservationCreatedEvent(Guid reservationId, Guid guestId, DateTime startTime)
    {
        ReservationId = reservationId;
        GuestId = guestId;
        StartTime = startTime;
    }
}
```

## Writing Application Logic

### Commands
```csharp
/// <summary>
/// Command to create a new reservation
/// </summary>
public record CreateReservationCommand(
    Guid GuestId,
    DateTime StartTime,
    DateTime EndTime)
    : ICommand<CreateReservationResponse>;

public record CreateReservationResponse(Guid ReservationId);
```

### Command Handlers
```csharp
/// <summary>
/// Handles reservation creation. Enforces business rules and persists aggregate.
/// </summary>
public class CreateReservationHandler : ICommandHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly IRepository<Reservation, Guid> _reservationRepository;
    private readonly IRepository<Guest, Guid> _guestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReservationHandler(
        IRepository<Reservation, Guid> reservationRepository,
        IRepository<Guest, Guid> guestRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateReservationResponse> Handle(
        CreateReservationCommand command,
        CancellationToken cancellationToken)
    {
        // Verify guest exists
        var guest = await _guestRepository.GetByIdAsync(command.GuestId, cancellationToken)
            ?? throw new InvalidOperationException("Guest not found");

        // Create reservation aggregate
        var reservation = Reservation.Create(
            guestId: command.GuestId,
            startTime: command.StartTime,
            endTime: command.EndTime);

        // Persist
        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Domain events are automatically published by infrastructure layer

        return new CreateReservationResponse(reservation.Id);
    }
}
```

### Queries
```csharp
/// <summary>
/// Query to retrieve a reservation by ID
/// </summary>
public record GetReservationQuery(Guid ReservationId) : IQuery<ReservationDto?>;
```

### Query Handlers
```csharp
/// <summary>
/// Handles reservation retrieval. Optimized for read operations.
/// </summary>
public class GetReservationQueryHandler : IQueryHandler<GetReservationQuery, ReservationDto?>
{
    private readonly IRepository<Reservation, Guid> _reservationRepository;

    public GetReservationQueryHandler(IRepository<Reservation, Guid> reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<ReservationDto?> Handle(
        GetReservationQuery query,
        CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            query.ReservationId,
            cancellationToken);

        return reservation is null
            ? null
            : new ReservationDto(
                reservation.Id,
                reservation.GuestId,
                reservation.StartTime,
                reservation.EndTime,
                reservation.Status.ToString());
    }
}
```

## Writing Tests

### Unit Tests (Domain Logic)
```csharp
public class ReservationTests
{
    [Fact]
    public void Create_WithValidData_ReturnsReservation()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);

        // Act
        var reservation = Reservation.Create(guestId, startTime, endTime);

        // Assert
        Assert.NotNull(reservation);
        Assert.Equal(guestId, reservation.GuestId);
        Assert.Single(reservation.GetDomainEvents()); // Created event
    }

    [Fact]
    public void Create_WithPastDateTime_ThrowsException()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var pastTime = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Reservation.Create(guestId, pastTime, pastTime.AddHours(1)));
    }
}
```

### Integration Tests (Command Handlers)
```csharp
public class CreateReservationHandlerTests
{
    private readonly ReservationDbContext _dbContext;
    private readonly IRepository<Reservation, Guid> _repository;

    public CreateReservationHandlerTests()
    {
        _dbContext = new ReservationDbContext(CreateInMemoryOptions());
        _repository = new GenericRepository<Reservation, Guid>(_dbContext);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesReservation()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var command = new CreateReservationCommand(
            guestId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2));

        var handler = new CreateReservationHandler(_repository, _repository, _dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ReservationId);
        var saved = await _repository.GetByIdAsync(result.ReservationId);
        Assert.NotNull(saved);
    }

    private static DbContextOptions<ReservationDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ReservationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }
}
```

## Configuration Management

### Development vs Production
- **Development**: Use `appsettings.Development.json`
- **Production**: Use `appsettings.json`
- **Secrets**: Use User Secrets or environment variables

```bash
# Set user secret
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."

# Environment variable
export ASPNETCORE_ENVIRONMENT=Production
```

## Logging Best Practices

```csharp
private readonly ILogger<MyClass> _logger;

public async Task DoSomething()
{
    _logger.LogInformation("Starting operation {OperationName}", nameof(DoSomething));

    try
    {
        // Do work
        _logger.LogInformation("Operation completed successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed with error");
        throw;
    }
}
```

## Performance Considerations

### Database Queries
- Use projections to select only needed fields
- Use `IAsyncEnumerable` for large result sets
- Implement pagination for list endpoints

```csharp
public async Task<IEnumerable<ReservationDto>> Handle(
    GetReservationsQuery query,
    CancellationToken cancellationToken)
{
    var page = (query.PageNumber - 1) * query.PageSize;

    return await DbSet
        .AsNoTracking() // Read-only query
        .Skip(page)
        .Take(query.PageSize)
        .Select(r => new ReservationDto(...))
        .ToListAsync(cancellationToken);
}
```

### Async Operations
- Always use `async`/`await`, never `.Result` or `.Wait()`
- Properly configure `DbContext` pooling for connection reuse

## Security Practices

### Input Validation
- Validate in handlers before using data
- Use FluentValidation for complex rules
- Sanitize user input

### Authorization
- Implement authorization behaviors in MediatR pipeline
- Check permissions before modifying aggregates

### Data Protection
- Never log sensitive information (passwords, tokens, PII)
- Use HTTPS everywhere
- Implement rate limiting for public endpoints

## Code Review Checklist

- [ ] Code follows naming conventions
- [ ] Comments explain "why", not "what"
- [ ] No circular dependencies
- [ ] Tests cover happy path and error cases
- [ ] No hardcoded values (use configuration)
- [ ] Async/await used correctly
- [ ] Error handling is appropriate
- [ ] Database queries are efficient
- [ ] No secrets in code (use configuration)

---

**Last Updated**: 2026-01-08
