using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;
using Reservation.Application.Behaviors;
using Reservation.Infrastructure;
using Reservation.Infrastructure.Persistence;
using Reservation.API.Endpoints;

// Create WebApplication builder - this creates the ASP.NET Core application host
var builder = WebApplication.CreateBuilder(args);

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
}

app.UseHttpsRedirection();

// TODO: Add authentication and authorization middleware when implementing auth feature
// app.UseAuthentication();
// app.UseAuthorization();

// Map all endpoint groups (Vertical Slice Architecture)
var endpointGroups = typeof(Program).Assembly
    .GetTypes()
    .Where(t => t.IsAssignableTo(typeof(EndpointGroup)) && !t.IsAbstract);

foreach (var endpointGroup in endpointGroups)
{
    var instance = Activator.CreateInstance(endpointGroup) as EndpointGroup;
    instance?.Map(app);
}

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

app.Run();
