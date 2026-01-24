// ASP.NET Core Identity Integration Guide for Program.cs
// This file shows where and how to integrate Identity services
// Add these sections to your existing Program.cs file

// ============= LOCATION IN PROGRAM.CS =============
// Add these AFTER existing Database/DbContext registration
// but BEFORE calling builder.Build()

// Step 1: Add Identity services after DbContext registration
// Insert this code block around line 50-60 in Program.cs:

/*
// ============= ASPNETCORE IDENTITY CONFIGURATION =============
// Register ASP.NET Core Identity for user authentication and management
// This includes UserManager, RoleManager, and all Identity services

var identityConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

services.AddIdentityServices(builder.Configuration, identityConnectionString);

// Add JWT authentication (implement next phase)
// services.AddJwtAuthentication(builder.Configuration);

// Add authorization policies
builder.Services.AddAuthorizationPolicies();
*/

// Step 2: Add Authorization middleware in the app pipeline
// Insert this code block around line 150-160 in Program.cs (in the middleware section):

/*
// Enable authentication and authorization middleware
// IMPORTANT: Order matters!
// 1. Authentication must come before Authorization
// 2. Authentication must come before Cors
// 3. Both should come before MapControllers/MapEndpoints

app.UseAuthentication();
app.UseAuthorization();
*/

// Step 3: Update Swagger/OpenAPI to show authentication (optional but recommended)
// Already configured in existing Program.cs with security scheme
// No additional changes needed - it's already set up

// ============= DATABASE MIGRATION COMMANDS =============
// After adding these files, create migrations:

// Terminal Command 1: Create migration for Identity tables
// dotnet ef migrations add AddIdentityTables --project src/Reservation.Infrastructure --startup-project src/Reservation.API

// Terminal Command 2: Apply migration to database
// dotnet ef database update --project src/Reservation.Infrastructure --startup-project src/Reservation.API

// ============= CONFIGURATION IN APPSETTINGS.JSON =============
// Add these settings to appsettings.json:

/*
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=reservation_db;Username=postgres;Password=postgres"
  },
  "Identity": {
    "PasswordRequireDigit": true,
    "PasswordRequireLowercase": true,
    "PasswordRequireUppercase": true,
    "PasswordRequireNonAlphanumeric": true,
    "PasswordMinimumLength": 8
  }
}
*/

// ============= ENVIRONMENT VARIABLES (PRODUCTION) =============
// For production, use environment variables instead:

/*
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=prod-db.example.com;...
IDENTITY__PASSWORDMINIMUMNLENGTH=8
*/

// ============= COMPLETE PROGRAM.CS INTEGRATION EXAMPLE =============
// Here's how the full Program.cs should look with Identity integrated:

/*
using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;
using Reservation.Application.Behaviors;
using Reservation.Infrastructure;
using Reservation.Infrastructure.Identity;
using Reservation.Infrastructure.Persistence;
using Reservation.API.Endpoints;
using Reservation.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============= LOGGING =============
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

builder.Host.UseSerilog();

// ============= DEPENDENCY INJECTION =============
// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

// Add MediatR with behaviors
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.RegisterServicesFromAssembly(typeof(Reservation.Application.Abstractions.ICommandHandler<,>).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add Health Checks
builder.Services.AddHealthChecks();

// ============= DATABASE CONFIGURATION =============
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============= IDENTITY CONFIGURATION - NEW =============
// Register ASP.NET Core Identity services
builder.Services.AddIdentityServices(builder.Configuration, connectionString);

// ============= INFRASTRUCTURE SERVICES =============
builder.Services.AddInfrastructure();

// ============= SWAGGER CONFIGURATION =============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============= CORS CONFIGURATION =============
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevelopmentPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

var app = builder.Build();

// ============= MIDDLEWARE PIPELINE =============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();

// ============= AUTHENTICATION & AUTHORIZATION - NEW =============
// IMPORTANT: Order matters! Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ============= ENDPOINTS =============
var apiGroup = app.MapGroup("/api/v1");
typeof(IEndpointGroup)
    .Assembly
    .GetTypes()
    .Where(t => typeof(IEndpointGroup).IsAssignableFrom(t) && !t.IsInterface)
    .ForEach(t => ((IEndpointGroup)Activator.CreateInstance(t)!).Map(app));

app.MapHealthChecks("/health");

app.Run();
*/

// ============= KEY INTEGRATION POINTS =============
// 1. AddIdentityServices() - Registers Identity, UserManager, RoleManager
// 2. AddInfrastructure() - Now also registers IIdentityUserRepository
// 3. app.UseAuthentication() - Validates JWT tokens
// 4. app.UseAuthorization() - Enforces [Authorize] attributes
// 5. appsettings.json - Password policy configuration

// ============= TESTING THE INTEGRATION =============
// After integration, test with these endpoints (once implemented):

/*
1. Register:
   POST /api/v1/auth/register
   {
     "email": "user@example.com",
     "username": "user",
     "password": "SecurePassword123!"
   }

2. Login:
   POST /api/v1/auth/login
   {
     "email": "user@example.com",
     "password": "SecurePassword123!"
   }

3. Protected endpoint (with JWT token):
   GET /api/v1/reservations
   Authorization: Bearer {accessToken}
*/

// ============= TROUBLESHOOTING =============
// Issue: "DbContext not registered" error
// Solution: Ensure AddDbContext<IdentityContext> is called in AddIdentityServices

// Issue: Password creation fails
// Solution: Check password meets requirements (8+ chars, uppercase, lowercase, digit, special)

// Issue: User creation fails with "DuplicateUserName"
// Solution: Username must be unique; check database for existing user

// Issue: Migration fails
// Solution: Run "dotnet ef database update" to apply pending migrations

// ============= NEXT STEPS =============
// 1. Update Program.cs with Identity registration
// 2. Add appsettings configuration
// 3. Create and apply migrations
// 4. Implement AuthEndpoints (POST /auth/register, /auth/login, etc.)
// 5. Implement JWT token service (ITokenService)
// 6. Test authentication flow end-to-end
