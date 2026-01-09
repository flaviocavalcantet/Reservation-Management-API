using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Reservation.API.Endpoints;

/// <summary>
/// Health check endpoint providing application liveness and readiness information.
/// Used for load balancer health checks and monitoring.
/// </summary>
public class HealthEndpoints : EndpointGroup
{
    /// <summary>
    /// Maps health check endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to map endpoints to.</param>
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup("/health")
            .WithName("Health")
            .WithOpenApi()
            .WithDescription("Health check endpoints");

        group.MapGet("/live", GetLiveness)
            .WithName("GetLiveness")
            .WithSummary("Liveness check - indicates if application is running")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/ready", GetReadiness)
            .WithName("GetReadiness")
            .WithSummary("Readiness check - indicates if application is ready for traffic")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/detailed", GetDetailedHealth)
            .WithName("GetDetailedHealth")
            .WithSummary("Detailed health status including dependencies")
            .Produces<DetailedHealthResponse>(StatusCodes.Status200OK)
            .Produces<DetailedHealthResponse>(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Liveness probe - indicates if the application process is running.
    /// Should be used by orchestration platforms to determine if container should be restarted.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A simple alive indicator and status code.</returns>
    private static IResult GetLiveness([FromServices] ILogger<HealthEndpoints> logger)
    {
        var result = new HealthResponse
        {
            Status = "alive",
            Timestamp = DateTime.UtcNow,
            Uptime = GC.GetTotalMemory(false) / 1024 / 1024 // Memory in MB
        };

        logger.LogInformation("Liveness check performed - Status: {Status}", result.Status);

        return Results.Ok(result);
    }

    /// <summary>
    /// Readiness probe - indicates if the application is ready to accept traffic.
    /// Should be used by load balancers to route traffic to this instance.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <returns>Readiness status indicating application is ready.</returns>
    private static IResult GetReadiness([FromServices] ILogger<HealthEndpoints> logger)
    {
        // Application is ready if it's running
        var response = new HealthResponse
        {
            Status = "ready",
            Timestamp = DateTime.UtcNow,
            Dependencies = new List<string> { "self" }
        };

        logger.LogInformation(
            "Readiness check performed - Status: {Status}",
            response.Status);

        return Results.Json(response, statusCode: StatusCodes.Status200OK);
    }

    /// <summary>
    /// Detailed health check - returns comprehensive health information including all dependencies.
    /// Useful for monitoring dashboards and diagnostics.
    /// </summary>
    /// <param name="healthCheckService">The health check service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>Detailed health response with all component statuses.</returns>
    private static async Task<IResult> GetDetailedHealth(
        [FromServices] HealthCheckService healthCheckService,
        [FromServices] ILogger<HealthEndpoints> logger)
    {
        var healthCheckResult = await healthCheckService.CheckHealthAsync();

        var response = new DetailedHealthResponse
        {
            Status = healthCheckResult.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Runtime = new RuntimeInfo
            {
                DotNetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount
            },
            Components = healthCheckResult.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComponentHealth
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description ?? "No description provided",
                    Duration = kvp.Value.Duration.TotalMilliseconds
                })
        };

        var statusCode = healthCheckResult.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        logger.LogInformation(
            "Detailed health check performed - Overall status: {Status}, Component count: {ComponentCount}",
            response.Status,
            response.Components.Count);

        return Results.Json(response, statusCode: statusCode);
    }
}

/// <summary>
/// Basic health check response.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check timestamp in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in MB.
    /// </summary>
    public long Uptime { get; set; }

    /// <summary>
    /// Gets or sets the list of checked dependencies.
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Detailed health check response with component status information.
/// </summary>
public class DetailedHealthResponse
{
    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check timestamp in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the runtime information.
    /// </summary>
    public RuntimeInfo Runtime { get; set; } = new();

    /// <summary>
    /// Gets or sets the component health statuses.
    /// </summary>
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Runtime environment information.
/// </summary>
public class RuntimeInfo
{
    /// <summary>
    /// Gets or sets the .NET runtime version.
    /// </summary>
    public string DotNetVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    public string OSDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor count.
    /// </summary>
    public int ProcessorCount { get; set; }
}

/// <summary>
/// Individual component health status.
/// </summary>
public class ComponentHealth
{
    /// <summary>
    /// Gets or sets the component status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the check duration in milliseconds.
    /// </summary>
    public double Duration { get; set; }
}
