using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Authentication;
using Reservation.Infrastructure.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
            .Produces<LoginResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapPost("/refresh", Refresh)
            .WithName("Refresh")
            .WithSummary("Exchange a valid refresh token for a new access token")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get identity claims for the currently authenticated user")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    /// <summary>
    /// Validates a request model against its DataAnnotations attributes.
    /// </summary>
    /// <returns>A list of validation error messages; empty if the model is valid.</returns>
    private static List<string> Validate<T>(T model) where T : notnull
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);
        return validationResults
            .Select(r => r.ErrorMessage ?? "Invalid value")
            .ToList();
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
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Login attempt with invalid request: {Errors}", string.Join("; ", validationErrors));
            return Results.BadRequest(new ErrorResponse { Message = string.Join(" ", validationErrors) });
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

        // Generate access + refresh tokens
        var token = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken(user.Id);

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Results.Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenService.AccessTokenExpirationSeconds,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="userManager">User manager service</param>
    /// <param name="roleManager">Role manager service</param>
    /// <param name="tokenService">Token generation service</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>JWT token after successful registration</returns>
    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] RoleManager<IdentityRole<Guid>> roleManager,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<AuthenticationEndpoints> logger)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Registration attempt with invalid request: {Errors}", string.Join("; ", validationErrors));
            return Results.BadRequest(new ErrorResponse { Message = string.Join(" ", validationErrors) });
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

        // Assign the default "User" role to new accounts
        const string DefaultRole = "User";
        if (!await roleManager.RoleExistsAsync(DefaultRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));
        }

        await userManager.AddToRoleAsync(newUser, DefaultRole);

        // Generate access + refresh tokens for new user
        var roles = await userManager.GetRolesAsync(newUser);
        var token = tokenService.GenerateAccessToken(newUser, roles);
        var refreshToken = tokenService.GenerateRefreshToken(newUser.Id);

        logger.LogInformation("New user registered successfully: {Email}", newUser.Email);

        return Results.Json(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenService.AccessTokenExpirationSeconds,
            UserId = newUser.Id,
            Email = newUser.Email ?? string.Empty,
            Roles = roles.ToList()
        }, statusCode: StatusCodes.Status201Created);
    }

    /// <summary>
    /// Exchange a valid refresh token for a new access token (and a rotated refresh token).
    ///
    /// Lets a client keep a session alive without re-entering credentials once the
    /// short-lived access token expires. The refresh token is validated for signature,
    /// issuer, audience, lifetime, and token type; the user is then re-loaded so a
    /// deactivated account can no longer obtain new tokens.
    /// </summary>
    /// <param name="request">The refresh token to exchange.</param>
    /// <param name="userManager">User manager service.</param>
    /// <param name="tokenService">Token generation/validation service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A new access token and refresh token if the refresh token is valid.</returns>
    private static async Task<IResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<AuthenticationEndpoints> logger)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Refresh attempt with invalid request: {Errors}", string.Join("; ", validationErrors));
            return Results.BadRequest(new ErrorResponse { Message = string.Join(" ", validationErrors) });
        }

        // Validate signature, issuer, audience, lifetime, and that this is a refresh token.
        var userId = tokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
        {
            logger.LogWarning("Refresh attempt with an invalid or expired refresh token");
            return Results.Unauthorized();
        }

        // Re-load the user so deactivated/deleted accounts cannot refresh.
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Refresh attempt for missing or inactive user: {UserId}", userId);
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);

        // Rotate both tokens: a fresh refresh token gives active users a sliding session.
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken(user.Id);

        logger.LogInformation("Issued new access token via refresh for user {UserId}", user.Id);

        return Results.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = tokenService.AccessTokenExpirationSeconds,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Returns identity claims for the currently authenticated request.
    ///
    /// Works with both the custom JWT ("Bearer") and Auth0 ("Auth0") schemes -
    /// whichever one validated the token, the resulting claims are mapped onto
    /// the same <see cref="ClaimTypes"/> (see AuthenticationSchemeSelector and
    /// AuthenticationServiceConfiguration). Useful for a frontend to verify a
    /// token is valid right after login and to inspect the user's roles.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The authenticated user's id, email, roles, and token issuer.</returns>
    private static IResult GetCurrentUser(HttpContext httpContext)
    {
        var user = httpContext.User;

        return Results.Ok(new CurrentUserResponse
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = user.FindFirstValue(ClaimTypes.Email),
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Issuer = user.FindFirstValue("iss")
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
/// Refresh token exchange request model.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token issued at login/registration (or by a previous refresh).
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Successful login response with JWT token.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for authenticating subsequent requests.
    /// Include in Authorization header as: Bearer {accessToken}
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Long-lived refresh token used to obtain a new access token once it expires.
    /// Send it to POST /api/v1/auth/refresh; store it securely (e.g. HttpOnly cookie)
    /// and never put it in the Authorization header.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Number of seconds until the access token expires.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Authenticated user's unique identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Authenticated user's email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Roles assigned to the authenticated user.
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// Identity claims for the currently authenticated user, derived from the
/// validated access token (custom JWT or Auth0 OIDC).
/// </summary>
public class CurrentUserResponse
{
    /// <summary>
    /// Subject identifier from the token (GUID for the custom JWT scheme;
    /// Auth0 user/client id, e.g. "auth0|..." or "...@clients", for the Auth0 scheme).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email, if present in the token. Auth0 access tokens do not
    /// include email by default.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Roles assigned to the user. For Auth0 tokens, populated only if
    /// Auth0Settings:RoleClaimType has been configured via an Auth0 Action
    /// (see AUTHENTICATION.md).
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>
    /// The "iss" (issuer) claim from the token - identifies whether the
    /// request was authenticated via the custom JWT or Auth0.
    /// </summary>
    public string? Issuer { get; set; }
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
