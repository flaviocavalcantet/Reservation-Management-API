# Quick Start Guide

## Prerequisites
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download))
- PostgreSQL 12+ ([Download](https://www.postgresql.org/download/))
- Visual Studio 2022 or VS Code
- Git

## Setup

### 1. Clone the repository
```bash
git clone https://github.com/yourusername/ReservationManagement.git
cd ReservationManagement
```

### 2. Create PostgreSQL database and user
```sql
-- Connect to PostgreSQL
psql -U postgres

-- Create user
CREATE USER reservation_user WITH PASSWORD 'your_secure_password';

-- Create database
CREATE DATABASE ReservationManagement_Dev OWNER reservation_user;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE ReservationManagement_Dev TO reservation_user;
```

### 3. Update connection string
Edit `src/Reservation.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ReservationManagement_Dev;Username=reservation_user;Password=your_secure_password"
  }
}
```

### 4. Restore and build
```bash
dotnet restore
dotnet build
```

### 5. Run the application
```bash
dotnet run --project src/Reservation.API
```

The API will start at `https://localhost:7071` (or the port shown in the terminal).

### 6. Access Swagger UI
Open your browser and navigate to:
```
https://localhost:7071/swagger
```

## Project Structure

```
ReservationManagement/
├── src/
│   ├── Reservation.Domain/          # Business rules (no dependencies)
│   ├── Reservation.Application/     # Use cases (depends on Domain)
│   ├── Reservation.Infrastructure/  # Data access (implements Domain interfaces)
│   └── Reservation.API/             # REST endpoints (DI configuration)
├── tests/
│   └── Reservation.Tests/           # Unit and integration tests
├── README.md                        # Full architecture documentation
├── ARCHITECTURE.md                  # Architecture Decision Records
└── QUICKSTART.md                    # This file
```

## Common Commands

### Build
```bash
dotnet build
```

### Run tests
```bash
dotnet test
```

### Run specific project
```bash
dotnet run --project src/Reservation.API
```

### Create EF Core migration
```bash
dotnet ef migrations add MigrationName --project src/Reservation.Infrastructure
```

### Apply migrations
```bash
dotnet ef database update --project src/Reservation.Infrastructure
```

## Architecture at a Glance

- **Domain**: Pure business logic, no dependencies
- **Application**: Use cases (Commands/Queries), orchestrates Domain
- **Infrastructure**: Database, repositories, implements Domain interfaces
- **API**: REST endpoints, DI configuration

Dependency flow: `API → Application → Domain` and `Infrastructure → Domain`

## Next Steps

1. **Create Domain Aggregates**: Define `Reservation`, `Guest`, `TimeSlot` entities
2. **Create Commands/Queries**: Add `CreateReservationCommand`, `GetReservationsQuery`, etc.
3. **Create API Endpoints**: Map commands/queries to HTTP endpoints
4. **Add Validation**: Implement validators in Application layer
5. **Add Tests**: Create unit tests for domain logic and integration tests for handlers

## Troubleshooting

### PostgreSQL connection fails
- Verify PostgreSQL is running: `psql --version`
- Check connection string in `appsettings.Development.json`
- Ensure database and user exist

### Migration fails
- Delete existing database and recreate: `DROP DATABASE ReservationManagement_Dev;`
- Run application again to auto-apply migrations

### Port already in use
- Change port in `Properties/launchSettings.json` (API project)
- Or kill process: `netstat -ano | findstr :7071` (Windows)

## Documentation

- **Full Architecture**: See [README.md](README.md)
- **Decision Records**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **Authentication & Authorization**: See [AUTHENTICATION.md](AUTHENTICATION.md)
- **API Documentation**: Available at `/swagger` when app is running
- **Documentation Index**: See [INDEX.md](INDEX.md) for complete guide

## Support

For issues or questions:
1. Check the [README.md](README.md) for detailed documentation
2. Review [ARCHITECTURE.md](ARCHITECTURE.md) for design decisions
3. Create an issue on GitHub

---

**Happy coding!** 🚀
