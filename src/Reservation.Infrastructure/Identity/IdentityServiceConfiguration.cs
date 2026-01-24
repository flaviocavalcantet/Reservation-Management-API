using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reservation.Infrastructure.Identity;

/// <summary>
/// Dependency injection configuration for ASP.NET Core Identity.
/// 
/// This extension method registers all Identity services with production-grade configuration:
/// - Password policy enforcement
/// - User manager and role manager
/// - Identity entity framework stores
/// - Token providers
/// 
/// Password Policy Requirements:
/// - Minimum 8 characters
/// - At least 1 uppercase letter (A-Z)
/// - At least 1 lowercase letter (a-z)
/// - At least 1 digit (0-9)
/// - At least 1 non-alphanumeric character (!@#$%^&*-_+=...)
/// 
/// Design Notes:
/// - Isolated in this module to prevent Identity references in Domain
/// - Called from API layer's Program.cs
/// - Uses IdentityContext for Identity table management
/// - Configurable password policies via IConfiguration
/// 
/// This follows the pattern of Infrastructure layer managing framework-specific setup
/// while Application layer provides abstractions (IPasswordService, ITokenService).
/// </summary>
public static class IdentityServiceConfiguration
{
    /// <summary>
    /// Adds ASP.NET Core Identity services with production configuration.
    /// 
    /// This method:
    /// 1. Registers IdentityContext for EF Core
    /// 2. Configures ApplicationUser and roles
    /// 3. Sets password policy requirements
    /// 4. Registers token providers
    /// 5. Configures user and role managers
    /// 
    /// Usage in Program.cs:
    /// <code>
    /// services.AddIdentityServices(builder.Configuration, connectionString);
    /// </code>
    /// </summary>
    /// <param name="services">Service collection for dependency injection.</param>
    /// <param name="configuration">Application configuration for settings.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <returns>Modified service collection for chaining.</returns>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        // Register IdentityContext for EF Core
        // Uses PostgreSQL and connection string from appsettings or environment
        services.AddDbContext<IdentityContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Enable retry on failure for transient issues in development
                if (configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }
            }));

        // Configure ASP.NET Core Identity
        // ApplicationUser: the user entity
        // IdentityRole<Guid>: roles using GUID for consistency
        // Guid: primary key type
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Password policy configuration (production-grade)
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                
                // Allow configuration override from appsettings
                ConfigurePasswordPolicyFromConfig(options, configuration);

                // User options
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = 
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-@";

                // SignIn options
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Lockout options (prevent brute force attacks)
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<EmailTokenProvider<ApplicationUser>>("Email");

        return services;
    }

    /// <summary>
    /// Applies password policy configuration from appsettings.json.
    /// 
    /// Allows overriding default policies via configuration without code changes.
    /// 
    /// Example appsettings.json:
    /// <code>
    /// "Identity": {
    ///   "PasswordRequireDigit": true,
    ///   "PasswordRequireLowercase": true,
    ///   "PasswordRequireUppercase": true,
    ///   "PasswordRequireNonAlphanumeric": true,
    ///   "PasswordMinimumLength": 8
    /// }
    /// </code>
    /// </summary>
    private static void ConfigurePasswordPolicyFromConfig(
        IdentityOptions options,
        IConfiguration configuration)
    {
        var passwordConfig = configuration.GetSection("Identity");

        if (passwordConfig.Exists())
        {
            // These are optional overrides; defaults are already set
            if (passwordConfig["PasswordMinimumLength"] is string minLength && 
                int.TryParse(minLength, out var minLengthValue))
            {
                options.Password.RequiredLength = minLengthValue;
            }

            if (bool.TryParse(passwordConfig["PasswordRequireDigit"], out var requireDigit))
                options.Password.RequireDigit = requireDigit;

            if (bool.TryParse(passwordConfig["PasswordRequireLowercase"], out var requireLowercase))
                options.Password.RequireLowercase = requireLowercase;

            if (bool.TryParse(passwordConfig["PasswordRequireUppercase"], out var requireUppercase))
                options.Password.RequireUppercase = requireUppercase;

            if (bool.TryParse(passwordConfig["PasswordRequireNonAlphanumeric"], out var requireNonAlpha))
                options.Password.RequireNonAlphanumeric = requireNonAlpha;
        }
    }
}
