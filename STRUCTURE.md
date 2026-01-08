# Solution Structure Summary

## 🏗️ Project Architecture

```
ReservationManagement/
│
├── 📄 ReservationManagement.sln              # Solution file
├── 📚 Documentation/
│   ├── README.md                            # Complete architecture guide
│   ├── ARCHITECTURE.md                      # Architecture Decision Records (ADRs)
│   ├── QUICKSTART.md                        # Setup and quick start guide
│   ├── DEVELOPMENT.md                       # Development guidelines
│   └── STRUCTURE.md                         # This file
│
├── src/
│   │
│   ├── 🎯 Reservation.Domain/               # DOMAIN LAYER
│   │   ├── Reservation.Domain.csproj
│   │   ├── Abstractions/
│   │   │   ├── Entity.cs                    # Base class for entities
│   │   │   ├── AggregateRoot.cs             # Base class for aggregate roots
│   │   │   ├── ValueObject.cs               # Base class for value objects
│   │   │   ├── DomainEvent.cs               # Base class for domain events
│   │   │   ├── IRepository.cs               # Repository interface pattern
│   │   │   └── IUnitOfWork.cs               # Unit of Work interface
│   │   └── Common/                          # Shared utilities (placeholder)
│   │   
│   │   Key Characteristics:
│   │   • No external dependencies
│   │   • Pure business logic
│   │   • Framework-agnostic
│   │   • DDD tactical patterns
│   │
│   ├── 📋 Reservation.Application/          # APPLICATION LAYER
│   │   ├── Reservation.Application.csproj
│   │   ├── Abstractions/
│   │   │   ├── ICommandHandler.cs           # Command handler interface
│   │   │   ├── IQueryHandler.cs             # Query handler interface
│   │   │   └── IDomainEventPublisher.cs     # Event publishing abstraction
│   │   └── Behaviors/
│   │       ├── LoggingBehavior.cs           # Request/response logging
│   │       └── ValidationBehavior.cs        # Input validation
│   │   
│   │   Dependencies:
│   │   • Reservation.Domain
│   │   • MediatR (CQRS pattern)
│   │
│   │   Key Characteristics:
│   │   • Orchestration of domain logic
│   │   • CQRS pattern with MediatR
│   │   • Command/Query handlers
│   │   • Cross-cutting behavior pipeline
│   │
│   ├── 🔌 Reservation.Infrastructure/       # INFRASTRUCTURE LAYER
│   │   ├── Reservation.Infrastructure.csproj
│   │   ├── Persistence/
│   │   │   └── ReservationDbContext.cs      # EF Core DbContext
│   │   ├── Repositories/
│   │   │   └── GenericRepository.cs         # Generic repository implementation
│   │   └── InfrastructureDependencies.cs    # DI extension methods
│   │   
│   │   Dependencies:
│   │   • Reservation.Domain
│   │   • Reservation.Application
│   │   • EF Core 8
│   │   • Npgsql (PostgreSQL driver)
│   │
│   │   Key Characteristics:
│   │   • Data access implementation
│   │   • Repository pattern
│   │   • EF Core with PostgreSQL
│   │   • Unit of Work implementation
│   │
│   └── 🌐 Reservation.API/                  # API LAYER
│       ├── Reservation.API.csproj
│       ├── Program.cs                       # DI container configuration
│       ├── Endpoints/
│       │   └── EndpointGroup.cs             # Base endpoint abstraction
│       ├── appsettings.json                 # Production settings
│       └── appsettings.Development.json     # Development settings
│       
│       Dependencies:
│       • Reservation.Application
│       • Reservation.Infrastructure (DI registration)
│       • ASP.NET Core 8
│       • Swagger/OpenAPI
│       • MediatR
│
│       Key Characteristics:
│       • REST API endpoints
│       • Dependency Injection setup
│       • Middleware pipeline
│       • Database migrations
│
└── tests/
    │
    └── 🧪 Reservation.Tests/                # TEST LAYER
        ├── Reservation.Tests.csproj
        └── (Test files organized by layer)
        
        Dependencies:
        • Reservation.Domain
        • Reservation.Application
        • xUnit
        • Moq
        • FluentAssertions
        
        Key Characteristics:
        • Unit tests for domain logic
        • Integration tests for handlers
        • Test doubles and mocks
```

## 📊 Dependency Flow

```
┌─────────────────┐
│   API LAYER     │ (REST endpoints, DI setup)
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│ APPLICATION LAYER           │ (Commands, Queries, Handlers)
└────────┬────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│   DOMAIN LAYER               │ (Pure business logic)
│ (Entities, Value Objects)    │
└──────────────────────────────┘
         ▲
         │
┌────────┴────────────────────┐
│ INFRASTRUCTURE LAYER         │
│ (Repositories, DbContext)    │
└─────────────────────────────┘

Dependency Rules:
• API → Application → Domain (strict)
• Infrastructure → Domain (via interfaces)
• Infrastructure ↔ Application (implementation)
• No circular dependencies allowed
```

## 🎯 Layer Responsibilities

### Domain Layer (Reservation.Domain)
**Pure Business Logic - No Framework Dependencies**

Provides:
- `Entity`: Base class for mutable objects with identity
- `AggregateRoot`: Consistency boundary for aggregates
- `ValueObject`: Immutable domain concepts
- `DomainEvent`: Event notification within domain
- `IRepository<T>`: Data access abstraction
- `IUnitOfWork`: Atomic operation coordination

**Example Usage:**
```csharp
// Create aggregate with domain event
var reservation = Reservation.Create(guestId, startTime);
reservation.GetDomainEvents(); // Returns ReservationCreatedEvent
```

### Application Layer (Reservation.Application)
**Use Case Orchestration - CQRS Pattern**

Provides:
- `ICommand<TResponse>`: Write operation interface
- `IQuery<TResponse>`: Read operation interface
- `ICommandHandler<TCommand, TResponse>`: Command processing
- `IQueryHandler<TQuery, TResponse>`: Query processing
- `IDomainEventPublisher`: Event propagation
- Pipeline behaviors for logging and validation

**Example Usage:**
```csharp
// Send command through MediatR pipeline
var result = await mediator.Send(new CreateReservationCommand(...));
```

### Infrastructure Layer (Reservation.Infrastructure)
**Data Access & External Dependencies**

Provides:
- `ReservationDbContext`: EF Core database context
- `GenericRepository<T>`: CRUD implementation
- Entity configurations for EF Core
- Database migrations management
- Dependency registration

**Example Usage:**
```csharp
// Repository provides data access abstraction
var reservation = await repository.GetByIdAsync(id);
```

### API Layer (Reservation.API)
**HTTP Endpoints & Dependency Injection**

Provides:
- REST endpoints for HTTP clients
- Dependency Injection container setup
- Middleware pipeline configuration
- Swagger/OpenAPI documentation
- Application startup configuration

**Example Usage:**
```
POST /api/reservations
{
  "guestId": "...",
  "startTime": "...",
  "endTime": "..."
}
```

## 🔧 Key Technologies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Runtime | .NET | 8.0 | Modern, high-performance runtime |
| Web Framework | ASP.NET Core | 8 | REST API development |
| ORM | Entity Framework Core | 8.0.0 | Data access abstraction |
| Database | PostgreSQL | 12+ | Relational database |
| Database Driver | Npgsql | 8.0.0 | PostgreSQL connector |
| CQRS | MediatR | 12.2.0 | Command/Query pattern |
| Testing | xUnit | 2.6.6 | Unit test framework |
| Mocking | Moq | 4.20.70 | Object mocking |
| Assertions | FluentAssertions | 6.12.0 | Test assertions |

## 📁 Project File References

### Clean Architecture Dependency Enforcement

**Reservation.Domain.csproj**
```xml
<!-- NO project references - domain layer is independent -->
```

**Reservation.Application.csproj**
```xml
<ProjectReference Include="../Reservation.Domain/..." />
```

**Reservation.Infrastructure.csproj**
```xml
<ProjectReference Include="../Reservation.Domain/..." />
<ProjectReference Include="../Reservation.Application/..." />
```

**Reservation.API.csproj**
```xml
<ProjectReference Include="../Reservation.Application/..." />
<ProjectReference Include="../Reservation.Infrastructure/..." />
```

## 🚀 Getting Started

### Step 1: Setup Environment
```bash
# Clone repository
git clone <repo-url>
cd ReservationManagement

# Restore packages
dotnet restore

# Build solution
dotnet build
```

### Step 2: Configure Database
```bash
# Create PostgreSQL database
# Update connection string in appsettings.Development.json
```

### Step 3: Run Application
```bash
dotnet run --project src/Reservation.API
```

### Step 4: Access API
```
https://localhost:7071/swagger
```

## 📖 Documentation Files

| File | Purpose | Audience |
|------|---------|----------|
| README.md | Complete architecture guide with design patterns | All developers |
| ARCHITECTURE.md | Architecture Decision Records (ADRs) explaining key decisions | Architects, seniors |
| QUICKSTART.md | Setup and initial run instructions | New developers |
| DEVELOPMENT.md | Code style, patterns, guidelines, best practices | All developers |
| STRUCTURE.md | This file - project structure overview | All developers |

## 🎨 Design Patterns Used

| Pattern | Layer | Purpose |
|---------|-------|---------|
| **Clean Architecture** | All | Separation of concerns |
| **Domain-Driven Design** | Domain | Business logic encapsulation |
| **CQRS** | Application | Command/Query separation |
| **Repository** | Infrastructure | Data access abstraction |
| **Unit of Work** | Infrastructure | Atomic operation coordination |
| **Pipeline Behaviors** | Application | Cross-cutting concerns |
| **Dependency Injection** | API | Loose coupling |
| **Entity Framework** | Infrastructure | ORM abstraction |
| **Async/Await** | All | Non-blocking I/O |

## ✅ What's Ready to Extend

- **Reservation Aggregate**: Create Reservation entity, value objects, business logic
- **Guest Aggregate**: Model guest data and behavior
- **TimeSlot Aggregate**: Manage time availability
- **Commands**: CreateReservation, CancelReservation, etc.
- **Queries**: GetReservation, GetGuestReservations, etc.
- **API Endpoints**: Map commands/queries to HTTP routes
- **Validations**: Implement with FluentValidation
- **Tests**: Unit tests for domain, integration tests for handlers
- **Caching**: Add caching behavior to queries
- **Authentication**: Add authorization behavior
- **Events**: Subscribe to domain events for side effects
- **Migrations**: Create database schema with EF Core migrations

## 🔐 Security Architecture

```
HTTP Request
    ↓
[CORS Middleware]
    ↓
[Authorization Header]
    ↓
[Endpoint Group]
    ↓
[MediatR Pipeline]
    ├─ [Logging Behavior]
    ├─ [Validation Behavior]
    └─ [Command/Query Handler]
         ↓
    [Domain Logic - Enforces Rules]
         ↓
    [Repository - Data Access]
         ↓
    [DbContext - Persistence]
```

## 🎓 Learning Path

1. **Start with Domain**: Understand Entity, AggregateRoot, ValueObject
2. **Understand Commands**: Create sample command and handler
3. **Understand Queries**: Create sample query and handler
4. **Build Endpoints**: Map commands/queries to REST endpoints
5. **Add Tests**: Write unit and integration tests
6. **Explore Infrastructure**: Understand EF Core configuration
7. **Master Architecture**: Review ADRs and design decisions

## 📊 Project Statistics

- **Projects**: 5 (Domain, Application, Infrastructure, API, Tests)
- **Total Files**: 25+
- **Base Classes**: 4 (Entity, AggregateRoot, ValueObject, DomainEvent)
- **Interfaces**: 6 (IRepository, IUnitOfWork, ICommand, IQuery, ICommandHandler, IQueryHandler)
- **NuGet Packages**: 12+
- **Lines of Code** (initial): 500+ (will grow with features)

---

**Created**: January 2026  
**Architecture**: Clean Architecture + Tactical DDD  
**Framework**: .NET 8 + ASP.NET Core  
**Database**: PostgreSQL with EF Core
