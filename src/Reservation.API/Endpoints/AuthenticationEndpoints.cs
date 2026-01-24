using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Authentication;
using Reservation.Infrastructure.Identity;
using System.ComponentModel.DataAnnotations;

namespace Reservation.API.Endpoints;

/// <summary>
/// JWT authentication endpoints for user login and token management.
/// Provides token generation for securing subsequent API requests.
/// </summary>
public class AuthenticationEndpoints : EndpointGroup
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithName("Authentication")
            .WithOpenApi()
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Generate JWT token for authentication")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .AllowAnonymous();
    }

    /// <summary>
    /// Authenticate user and generate JWT access token.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="userManager">User manager service</param>
    /// <param name="tokenService">Token generation service</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>JWT token if credentials are valid</returns>
    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<AuthenticationEndpoints> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            logger.LogWarning("Login attempt with missing credentials");
            return Results.BadRequest(new ErrorResponse { Message = "Email and password are required" });
        }

        // Find user by email
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            return Results.Unauthorized();
        }

        // Verify password
        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            logger.LogWarning("Invalid password for user: {Email}", request.Email);
            return Results.Unauthorized();
        }

        // Get user roles
        var roles = await userManager.GetRolesAsync(user);

        // Generate JWT token
        var token = tokenService.GenerateAccessToken(user, roles);

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Results.Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email ?? string.Empty,
            UserId = user.Id
        });
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="userManager">User manager service</param>
    /// <param name="tokenService">Token generation service</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>JWT token after successful registration</returns>
    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<AuthenticationEndpoints> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
        {
            return Results.BadRequest(new ErrorResponse { Message = "Email, password, and full name are required" });
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return Results.Conflict(new ErrorResponse { Message = "User with this email already exists" });
        }

        // Create new user
        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newUser, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
            return Results.BadRequest(new ErrorResponse { Message = $"Registration failed: {errors}" });
        }

        // Generate JWT token for new user
        var roles = new List<string>();
        var token = tokenService.GenerateAccessToken(newUser, roles);

        logger.LogInformation("New user registered successfully: {Email}", newUser.Email);

        return Results.Ok(new LoginResponse
        {
            Token = token,
            Email = newUser.Email ?? string.Empty,
            UserId = newUser.Id
        });
    }
}

/// <summary>
/// Login request model.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (plain text - will be hashed).
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// User registration request model.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's full name.
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User email address.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (plain text - will be hashed).
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Successful login response with JWT token.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for authenticating subsequent requests.
    /// Include in Authorization header as: Bearer {token}
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user's email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user's unique identifier.
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// Error response model.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
