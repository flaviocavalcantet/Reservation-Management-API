using Microsoft.AspNetCore.Identity;
using Reservation.Application.Repositories;
using Reservation.Infrastructure.Identity;

namespace Reservation.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Identity users.
/// 
/// This repository wraps ASP.NET Core Identity's UserManager to provide
/// a clean abstraction for application-layer use.
/// 
/// Responsibilities:
/// - User creation with password hashing
/// - User lookup (by email, by ID)
/// - Role management
/// - Password verification
/// - Last login tracking
/// 
/// Design Notes:
/// - Implements IIdentityUserRepository (Application abstraction)
/// - Uses UserManager{ApplicationUser} from Identity
/// - Converts Identity results to application-friendly format
/// - No domain logic; purely Identity framework operations
/// - Thread-safe through UserManager's internal synchronization
/// </summary>
public class IdentityUserRepository : IIdentityUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Initializes repository with UserManager dependency.
    /// </summary>
    public IdentityUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>
    /// Creates a new Identity user with hashed password.
    /// 
    /// The password is automatically hashed by UserManager using PBKDF2.
    /// </summary>
    public async Task<IdentityUserResult> CreateUserAsync(
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(password);

        // CreateAsync handles password hashing internally
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => $"{e.Code}: {e.Description}")
                .ToArray();
            return IdentityUserResult.Failure(errors);
        }

        return IdentityUserResult.Success();
    }

    /// <summary>
    /// Finds a user by email address (case-insensitive).
    /// </summary>
    public async Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(email);

        // UserManager.FindByEmailAsync is case-insensitive
        return await _userManager.FindByEmailAsync(email);
    }

    /// <summary>
    /// Finds a user by ID.
    /// </summary>
    public async Task<ApplicationUser?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    /// <summary>
    /// Checks if a user with the given email exists.
    /// </summary>
    public async Task<bool> UserExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    /// <summary>
    /// Updates user's last login timestamp and saves to database.
    /// </summary>
    public async Task<IdentityUserResult> UpdateLastLoginAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return IdentityUserResult.Failure("User not found");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => $"{e.Code}: {e.Description}")
                .ToArray();
            return IdentityUserResult.Failure(errors);
        }

        return IdentityUserResult.Success();
    }

    /// <summary>
    /// Adds a user to a role (creates association if role exists).
    /// </summary>
    public async Task<IdentityUserResult> AddToRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(roleName);

        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return IdentityUserResult.Failure("User not found");
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => $"{e.Code}: {e.Description}")
                .ToArray();
            return IdentityUserResult.Failure(errors);
        }

        return IdentityUserResult.Success();
    }

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    public async Task<List<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return new List<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    /// <summary>
    /// Verifies a user's password using constant-time comparison.
    /// </summary>
    public async Task<bool> VerifyPasswordAsync(
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(password);

        // CheckPasswordAsync uses constant-time comparison to prevent timing attacks
        return await _userManager.CheckPasswordAsync(user, password);
    }
}
