using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Reservation.Application.Authentication;

namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Extension methods for configuring JWT Bearer authentication.
/// 
/// Called from Program.cs to:
/// 1. Register ITokenService in DI
/// 2. Configure JWT Bearer authentication scheme
/// 3. Set up token validation parameters
/// 4. Configure challenge and forbidden response handlers
/// 
/// Usage in Program.cs:
/// <code>
/// var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
///     ?? throw new InvalidOperationException("JwtSettings not configured");
/// 
/// services.AddAuthenticationServices(builder.Configuration, jwtSettings);
/// 
/// // In middleware pipeline:
/// app.UseAuthentication();
/// app.UseAuthorization();
/// </code>
/// 
/// appsettings.json example:
/// <code>
/// "JwtSettings": {
///   "SecretKey": "your-base64-encoded-256-bit-key",
///   "Issuer": "ReservationAPI",
///   "Audience": "ReservationWebApp",
///   "AccessTokenExpirationMinutes": 15,
///   "RefreshTokenExpirationDays": 7,
///   "ValidateIssuerSigningKey": true,
///   "ValidateIssuer": true,
///   "ValidateAudience": true,
///   "ValidateLifetime": true,
///   "ClockSkewSeconds": 0
/// }
/// </code>
/// </summary>
public static class AuthenticationServiceConfiguration
{
    /// <summary>
    /// Adds JWT Bearer authentication to the service collection.
    /// 
    /// Registers:
    /// 1. ITokenService → JwtTokenService (DI for token generation/validation)
    /// 2. Authentication scheme "Bearer" with JWT Bearer handler
    /// 3. Token validation parameters (signature, issuer, audience, lifetime)
    /// 
    /// Called from Program.cs after Identity configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration provider with JwtSettings section.</param>
    /// <param name="jwtSettings">JWT settings object (pre-validated).</param>
    /// <returns>IServiceCollection for fluent API chaining.</returns>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        JwtSettings jwtSettings)
    {
        // Register token service in DI
        // ITokenService is used by handlers to generate/validate tokens
        services.AddScoped<ITokenService, JwtTokenService>();

        // Get secret key from configuration
        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        // Configure JWT Bearer authentication
        services.AddAuthentication(options =>
        {
            // Set the default authentication scheme
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Token validation parameters
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validate signature using the secret key
                ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                // Validate issuer (token must be from our application)
                ValidateIssuer = jwtSettings.ValidateIssuer,
                ValidIssuer = jwtSettings.Issuer,

                // Validate audience (token must be for our application/clients)
                ValidateAudience = jwtSettings.ValidateAudience,
                ValidAudience = jwtSettings.Audience,

                // Validate token hasn't expired
                ValidateLifetime = jwtSettings.ValidateLifetime,

                // Clock skew - tolerance for time differences between servers
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds),

                // Require claims to be present
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Event handlers for authentication flow

            // OnTokenValidated: Called when token is successfully validated
            // Use this to perform additional checks (database lookups, etc.)
            options.Events = new JwtBearerEvents
            {
                // Called when token validation succeeds
                OnTokenValidated = context =>
                {
                    // Token is already validated at this point
                    // Can add additional logic here:
                    // - Verify user still exists
                    // - Verify user is not locked out
                    // - Perform audit logging
                    
                    return Task.CompletedTask;
                },

                // Called when authentication fails
                OnAuthenticationFailed = context =>
                {
                    // Log failed authentication attempts
                    // This is called for expired tokens, invalid signatures, etc.
                    
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Add("WWW-Authenticate", 
                            "Bearer error=\"token_expired\"");
                    }
                    else if (context.Exception is SecurityTokenInvalidSignatureException)
                    {
                        context.Response.Headers.Add("WWW-Authenticate", 
                            "Bearer error=\"invalid_signature\"");
                    }
                    else
                    {
                        context.Response.Headers.Add("WWW-Authenticate", 
                            "Bearer error=\"invalid_token\"");
                    }

                    return Task.CompletedTask;
                },

                // Called when challenge is issued (401 Unauthorized)
                OnChallenge = context =>
                {
                    // Customize 401 response
                    // By default, ASP.NET Core returns empty response
                    // Here we can add custom error details if needed
                    
                    return Task.CompletedTask;
                },

                // Called when access is forbidden (403 Forbidden)
                OnForbidden = context =>
                {
                    // Customize 403 response
                    // Typically when user has valid token but lacks required role
                    
                    return Task.CompletedTask;
                }
            };

            // Require HTTPS for production
            options.RequireHttpsMetadata = !IsEnvironmentDevelopment();
        });

        // Register authorization service (required for [Authorize] attribute)
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Checks if environment is Development for conditional configuration.
    /// In development, we might allow HTTP for testing.
    /// In production, always use HTTPS.
    /// </summary>
    private static bool IsEnvironmentDevelopment()
    {
        var aspnetcore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return aspnetcore?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
