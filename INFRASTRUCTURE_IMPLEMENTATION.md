# Infrastructure Layer Implementation - EF Core

## Overview

The Infrastructure layer implements data persistence using Entity Framework Core 8.0 with PostgreSQL. It translates domain models into database operations while maintaining complete isolation of the domain and application layers from persistence concerns.

## Architecture Principles

### 1. Dependency Inversion
- Infrastructure depends on Application and Domain abstractions
- Application and Domain have **zero** dependencies on Infrastructure
- EF Core is completely hidden behind repository interfaces
- Database changes don't affect business logic

### 2. No Leakage of Framework Code
- Domain entities don't have EF Core attributes
- Application layer only knows about repository interfaces
- Entity configuration lives in Infrastructure.Persistence.Configurations
- Value object conversions handled entirely in EF configuration

### 3. Repository Pattern
- Generic repository for standard CRUD (GenericRepository<TAggregate, TId>)
- Specialized repository for domain-specific queries (ReservationRepository)
- All database queries go through repositories
- Enables testing with mock repositories

## Database Configuration

### Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ReservationManagement;Username=postgres;Password=postgres"
  }
}
```

### PostgreSQL Configuration (Program.cs)
```csharp
builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Retry policy for transient failures (connection timeouts, etc.)
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelaySeconds: 10,
            errorCodesToAdd: null);
    }));
```

### Graceful Degradation
- If PostgreSQL is not available, the app starts but logs a warning
- Queries will fail at runtime, but server remains operational
- Allows development without requiring database

## Project Structure

```
src/Reservation.Infrastructure/
├── Persistence/
│   ├── ReservationDbContext.cs          # DbContext and Unit of Work
│   └── Configurations/
│       └── ReservationEntityConfiguration.cs
├── Repositories/
│   ├── GenericRepository.cs             # Base CRUD implementation
│   └── ReservationRepository.cs         # Domain-specific queries
├── Migrations/
│   ├── 20260108202228_InitialCreate.cs  # First migration
│   ├── 20260108202228_InitialCreate.Designer.cs
│   └── ReservationDbContextModelSnapshot.cs
├── InfrastructureDependencies.cs        # DI configuration
└── Reservation.Infrastructure.csproj
```

## DbContext Implementation

### ReservationDbContext
Implements both the EF Core DbContext and the IUnitOfWork interface:

```csharp
public class ReservationDbContext : DbContext, IUnitOfWork
{
    public DbSet<Reservation> Reservations { get; set; }
    
    // Implements IUnitOfWork contract
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
}
```

**Responsibilities:**
- Manages DbSet<Reservation> for aggregate access
- Implements Unit of Work pattern for transactions
- Configures entity mappings via Fluent API
- Handles change tracking and persistence

## Entity Configuration

### ReservationEntityConfiguration
Defines how the Reservation aggregate maps to the database:

**Table Structure:**
- Schema: `public`
- Table: `Reservations`
- Primary Key: `Id` (UUID)

**Columns:**
| Column | Type | Constraints |
|--------|------|------------|
| Id | uuid | PRIMARY KEY |
| CustomerId | uuid | NOT NULL |
| StartDate | timestamp | NOT NULL |
| EndDate | timestamp | NOT NULL |
| Status | varchar(50) | NOT NULL (Created, Confirmed, or Cancelled) |
| CreatedAt | timestamp | NOT NULL, DEFAULT CURRENT_TIMESTAMP |
| ModifiedAt | timestamp | NULLABLE, DEFAULT CURRENT_TIMESTAMP |

**Indexes for Query Optimization:**
- `idx_reservations_customer_id` - Fast lookups by customer
- `idx_reservations_dates` - Fast range queries by date
- `idx_reservations_customer_status` - Fast filtered queries (e.g., confirmed reservations by customer)

**Value Object Mapping:**
- ReservationStatus stored as string in database (Created, Confirmed, Cancelled)
- Converted to/from domain value object via:
  ```csharp
  .HasConversion(
      status => status.Name,
      name => ReservationStatus.FromName(name))
  ```

## Repository Implementation

### GenericRepository<TAggregate, TId>
Base class providing standard CRUD operations:

```csharp
// Create
public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken)

// Read
public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken)
public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken)

// Update
public async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken)

// Delete
public async Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken)
```

**Design Details:**
- Uses EF.Property for type-safe generic queries (avoids dynamic expressions)
- Only handles persistence, no business logic
- Async/await for scalability
- Cancellation token support for graceful shutdown

### ReservationRepository
Extends GenericRepository with domain-specific queries:

```csharp
// Get all reservations for a customer (ordered by start date, descending)
Task<IEnumerable<Reservation>> GetByCustomerIdAsync(
    Guid customerId, 
    CancellationToken cancellationToken)

// Get reservations in a date range
Task<IEnumerable<Reservation>> GetByDateRangeAsync(
    DateTime startDate, 
    DateTime endDate, 
    CancellationToken cancellationToken)

// Find overlapping reservations (prevent double-booking)
Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(
    DateTime startDate, 
    DateTime endDate, 
    Guid? excludeReservationId,
    CancellationToken cancellationToken)

// Count active reservations by customer
Task<int> CountActiveByCustomerAsync(
    Guid customerId, 
    CancellationToken cancellationToken)
```

**Query Patterns:**
- All queries exclude cancelled reservations by default
- Date overlap detection: `A.Start <= B.End AND A.End >= B.Start`
- Results ordered for consistency (by StartDate descending)
- Uses database indexes for performance

## Dependency Injection Configuration

### InfrastructureDependencies.cs
Registers all infrastructure services:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    // Specialized repository for Reservation domain
    services.AddScoped<IReservationRepository, ReservationRepository>();
    
    // Generic repository for future aggregates
    services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
    
    // Unit of Work via DbContext
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReservationDbContext>());
    
    return services;
}
```

**Lifetimes:**
- Scoped (per-request) for DbContext and repositories
- Ensures clean database connections and change tracking per request
- Transactional consistency within a request scope

**Called from API Layer (Program.cs):**
```csharp
builder.Services.AddInfrastructure();
```

## Database Migration

### Initial Migration: InitialCreate
Generated from entity configuration, creates:

1. **Reservations table**
   - All columns with proper types and constraints
   - Timestamp defaults for auditing

2. **Indexes for performance**
   - Customer ID lookup (common in queries)
   - Date range queries (calendar views)
   - Combined customer + status (filtered queries)

### Migration Commands
```bash
# Create a new migration
dotnet ef migrations add MigrationName --project src/Reservation.Infrastructure --startup-project src/Reservation.API

# Apply migrations to database
dotnet ef database update --project src/Reservation.Infrastructure --startup-project src/Reservation.API

# Remove last migration (undo)
dotnet ef migrations remove --project src/Reservation.Infrastructure --startup-project src/Reservation.API

# Generate SQL script without applying
dotnet ef migrations script --project src/Reservation.Infrastructure --startup-project src/Reservation.API
```

## EF Core Features Used

### 1. Fluent Configuration API
- Type-safe, compile-time verified configuration
- Defined in IEntityTypeConfiguration<T> implementations
- Applied automatically via ApplyConfigurationsFromAssembly

### 2. Value Object Conversion
- HasConversion() for mapping value objects to database types
- Custom converter: domain type ↔ database type
- Enables storing ReservationStatus as string, loading as object

### 3. Shadow Properties (Future)
- Can add timestamp, rowversion for optimistic concurrency
- Don't appear in domain entity but exist in database
- Useful for auditing without domain pollution

### 4. LINQ to Entities
- Type-safe queries compiled to SQL
- EF.Property<T> for generic queries
- Includes/projection for optimization

### 5. Change Tracking
- Automatic change tracking of aggregate modifications
- SaveChangesAsync() persists changes in transactions
- Transaction management via IUnitOfWork interface

## Transaction Management

### Unit of Work Pattern
```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitTransactionAsync(CancellationToken cancellationToken);
    Task RollbackTransactionAsync(CancellationToken cancellationToken);
}
```

**Usage in Command Handlers:**
```csharp
public async Task<Result> Handle(CreateReservationCommand command, CancellationToken ct)
{
    var reservation = Reservation.Create(command.CustomerId, command.StartDate, command.EndDate);
    await _repository.AddAsync(reservation, ct);
    await _unitOfWork.SaveChangesAsync(ct);  // Commits transaction
    return result;
}
```

## No PostgreSQL Installation Required

The application gracefully handles missing PostgreSQL:

```csharp
try
{
    await app.Services.GetRequiredService<ReservationDbContext>()
        .Database.MigrateAsync();
}
catch (Exception ex)
{
    logger.LogWarning("Database migration failed (PostgreSQL may not be running): {Message}", ex.Message);
    // App continues to run without database
}
```

**Benefits:**
- Development without PostgreSQL installed
- Docker/containerized databases work seamlessly
- Cloud database migrations work the same way
- Server remains operational even if database is temporarily unavailable

## Testing Strategy

### Unit Tests
- Mock IReservationRepository
- Test business logic in isolation
- Fast execution, no database

### Integration Tests  
- Use real DbContext with in-memory database (for simple tests)
- Or PostgreSQL test database for full integration
- Test database schema changes
- Verify entity configurations work correctly

### Example Mock Setup
```csharp
var mockRepo = new Mock<IReservationRepository>();
mockRepo
    .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new Reservation { /* ... */ });

var handler = new CreateReservationHandler(mockRepo.Object, mockUnitOfWork);
```

## Performance Considerations

### Indexes
- Three composite/single indexes created in migration
- Cover most common query patterns
- Prevent N+1 queries via proper repository design

### Async All The Way
- All repository operations are async
- Prevents blocking threads
- Scales well under load

### Change Tracking
- Only modified aggregates tracked
- Single SaveChangesAsync() call per request
- Efficient batching of database operations

## Security Considerations

### No SQL Injection
- LINQ to Entities parameterizes all queries
- Never concatenate user input into queries

### No Privilege Escalation
- Database user in appsettings should have minimal privileges
- Read/Write only to ReservationManagement database

### Connection String Management
- Stored in appsettings.json (development only)
- Use Azure Key Vault, AWS Secrets Manager, or environment variables in production
- Never commit sensitive credentials to version control

## Future Enhancements

### Event Sourcing
- Add EventLog table for domain event persistence
- Implement event store for audit trail
- Enable event replay for debugging

### Soft Deletes
- Add IsDeleted flag to entities
- Filter deleted records in queries
- Preserve data history

### Optimistic Concurrency
- Add RowVersion/Timestamp property
- Detect concurrent updates
- Prevent lost updates

### Read Models
- Add separate read-optimized database schema
- CQRS pattern with separate read/write models
- Materialized views for complex queries

### Caching
- Add distributed cache (Redis)
- Cache frequently accessed reservations
- Invalidate on updates

## Architecture Diagram

```
Application Layer (IReservationRepository, IUnitOfWork)
         ↑
         │ (interfaces only)
         │
Infrastructure Layer
    ├─ Repositories
    │   ├─ ReservationRepository ──→ IReservationRepository
    │   └─ GenericRepository
    │
    ├─ Persistence
    │   ├─ ReservationDbContext ──→ IUnitOfWork
    │   └─ Configurations
    │       └─ ReservationEntityConfiguration
    │
    └─ Migrations
         └─ Database Schema (PostgreSQL)
```

## Summary

✓ Entity Framework Core 8.0 configured for PostgreSQL
✓ Fluent API configuration for clean schema
✓ Generic and specialized repositories for data access
✓ Value object conversion (ReservationStatus)
✓ Unit of Work pattern for transactions
✓ Performance indexes for common queries
✓ Initial migration created and ready
✓ Graceful degradation without PostgreSQL
✓ Complete isolation from domain/application layers
✓ Type-safe LINQ queries with no SQL injection risk
