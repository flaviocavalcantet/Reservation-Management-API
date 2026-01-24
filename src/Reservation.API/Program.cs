using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;
using Reservation.Application.Behaviors;
using Reservation.Infrastructure;
using Reservation.Infrastructure.Persistence;
using Reservation.Infrastructure.Identity;
using Reservation.Infrastructure.Authentication;
using Reservation.API.Endpoints;
using Reservation.API.Middleware;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// ============= STRUCTURED LOGGING CONFIGURATION =============
// Configure Serilog before building the application
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting Reservation Management API...");

    // Create WebApplication builder with Serilog logging
    var builder = WebApplication.CreateBuilder(args);

    // ============= SERILOG INTEGRATION =============
    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // ============= DEPENDENCY INJECTION CONFIGURATION =============
    // Follows Clean Architecture principle: Only Application layer knows about Domain layer
    // Infrastructure is injected via interfaces to maintain loose coupling

    // Add FluentValidation for request validation
    builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

    // Add MediatR for CQRS pattern with behaviors
    builder.Services
        .AddMediatR(config =>
        {
            // Register handlers from both API and Application assemblies
            config.RegisterServicesFromAssembly(typeof(Program).Assembly);
            config.RegisterServicesFromAssembly(typeof(Reservation.Application.Abstractions.ICommandHandler<,>).Assembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

    // ============= JWT AUTHENTICATION CONFIGURATION =============
    // Load and validate JWT settings before service registration
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
        ?? throw new InvalidOperationException(
            "JwtSettings section not found in configuration. " +
            "Add JwtSettings to appsettings.json with SecretKey, Issuer, and Audience.");

    try
    {
        jwtSettings.Validate();  // Validates secret key length, issuer, audience, etc.
    }
    catch (InvalidOperationException ex)
    {
        Log.Fatal(ex, "JWT settings validation failed");
        throw;
    }

    // Add Health Checks - REQUIRED for /health/detailed endpoint
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
        .AddDbContextCheck<ReservationDbContext>("database");

    // Add EF Core with PostgreSQL
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ReservationDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable error resilience in development - retries transient failures
            if (builder.Environment.IsDevelopment())
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }
        }));

    // ============= IDENTITY CONFIGURATION =============
    // Register ASP.NET Core Identity services for user management
    builder.Services.AddIdentityServices(builder.Configuration, connectionString);

    // ============= JWT BEARER AUTHENTICATION =============
    // Register JWT token service and configure Bearer scheme
    builder.Services.AddAuthenticationServices(builder.Configuration, jwtSettings);

    // Register infrastructure services
    builder.Services.AddInfrastructure();

    // Add Swagger/OpenAPI with professional configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // API metadata
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Reservation Management API",
            Version = "v1.0.0",
            Description = "REST API for managing reservations with Clean Architecture and Domain-Driven Design patterns",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Reservation Team",
                Email = "support@reservations.local"
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = "MIT License"
            }
        });

        // Include XML documentation from code comments
        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Security/Authorization definition (JWT - for future auth implementation)
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token authentication (to be implemented)"
        });

        // Apply security requirement globally (when auth is implemented)
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Order endpoints by name for better organization
        options.OrderActionsBy((description) => description.RelativePath);
    });

    // Add CORS for development
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
    // Observability middleware - ordered for proper correlation ID handling
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    // Register custom health endpoints from HealthEndpoints
    // Provides /health/live, /health/ready, and /health/detailed
    // new HealthEndpoints().Map(app);

    if (app.Environment.IsDevelopment())
    {
        // Enable Swagger documentation and UI
        app.UseSwagger(options =>
        {
            // Customize Swagger endpoint
            options.SerializeAsV2 = false;
        });
        
        app.UseSwaggerUI(options =>
        {
            // Configure Swagger UI
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reservation Management API v1.0");
            options.RoutePrefix = "swagger"; // Serve at /swagger path
            options.DocumentTitle = "Reservation API - Swagger UI";
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DefaultModelsExpandDepth(1);
            options.DisplayRequestDuration();
            options.EnableFilter();
        });
        
        app.UseCors("DevelopmentPolicy");

        Log.Information("Development environment detected - Swagger UI enabled at /swagger");
    }

    app.UseHttpsRedirection();

    // ============= AUTHENTICATION & AUTHORIZATION MIDDLEWARE =============
    // CRITICAL: UseAuthentication MUST come before UseAuthorization
    // Authentication extracts JWT claims, Authorization checks permissions
    app.UseAuthentication();    // Validate JWT Bearer tokens
    app.UseAuthorization();     // Check role-based permissions

    // Map all endpoint groups (Vertical Slice Architecture)
    var endpointGroups = typeof(Program).Assembly
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(EndpointGroup)) && !t.IsAbstract);

    foreach (var endpointGroup in endpointGroups)
    {
        var instance = Activator.CreateInstance(endpointGroup) as EndpointGroup;
        instance?.Map(app);
    }

    Log.Information("Endpoint mapping completed");

    // Migrate database on startup (development only)
    // NOTE: Database migration is optional - app can run without it
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Attempting to migrate database...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Database connection failed. The application will run without a database. " +
                    "To enable database features: " +
                    "1. Install PostgreSQL or start it if installed " +
                    "2. Verify connection string in appsettings.Development.json: {ConnectionString} " +
                    "3. Restart the application " +
                    "Error details: {ExceptionMessage}",
                    app.Configuration.GetConnectionString("DefaultConnection"),
                    ex.GetBaseException().Message);
            }
        }
    }

    Log.Information("Reservation Management API is ready to accept requests");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
