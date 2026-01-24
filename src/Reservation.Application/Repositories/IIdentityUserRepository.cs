using Reservation.Application.Authentication;

namespace Reservation.Application.Repositories;

/// <summary>
/// Repository interface for managing Identity users.
/// 
/// Lives in Application layer (abstraction) with implementation in Infrastructure.
/// This keeps Identity decoupled from Domain while allowing Application to work with users.
/// 
/// Design Pattern:
/// - Interface in Application.Repositories (abstraction boundary)
/// - Implementation in Infrastructure.Repositories (concrete implementation)
/// - Injected via dependency injection in API layer
/// - Used by command/query handlers in Application layer
/// 
/// Note:
/// - This works with ApplicationUser (Infrastructure Identity concept)
/// - NOT with Domain User aggregate (business entity)
/// - Purely for Identity/authentication concerns
/// </summary>
public interface IIdentityUserRepository
{
    /// <summary>
    /// Creates a new Identity user account.
    /// </summary>
    /// <param name="user">ApplicationUser to create.</param>
    /// <param name="password">Plain-text password (will be hashed by UserManager).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure.</returns>
    Task<IdentityUserResult> CreateUserAsync(
        IApplicationUser user,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    /// <param name="email">Email to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ApplicationUser if found, null otherwise.</returns>
    Task<IApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by ID.
    /// </summary>
    /// <param name="userId">GUID of user to find.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ApplicationUser if found, null otherwise.</returns>
    Task<IApplicationUser?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email exists.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if user exists, false otherwise.</returns>
    Task<bool> UserExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user's last login timestamp.
    /// </summary>
    /// <param name="userId">GUID of user to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure.</returns>
    Task<IdentityUserResult> UpdateLastLoginAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    /// <param name="userId">GUID of user.</param>
    /// <param name="roleName">Name of role (e.g., "Admin", "User").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure.</returns>
    Task<IdentityUserResult> AddToRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles for a user.
    /// </summary>
    /// <param name="userId">GUID of user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of role names.</returns>
    Task<List<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's password.
    /// </summary>
    /// <param name="user">ApplicationUser to verify password for.</param>
    /// <param name="password">Plain-text password to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if password is correct, false otherwise.</returns>
    Task<bool> VerifyPasswordAsync(
        IApplicationUser user,
        string password,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result type for Identity operations.
/// 
/// Wraps Identity result handling to provide clean error information.
/// Similar to IdentityResult but more application-friendly.
/// </summary>
public class IdentityUserResult
{
    /// <summary>
    /// Indicates if the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Error messages if operation failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static IdentityUserResult Success() => new() { Succeeded = true };

    /// <summary>
    /// Creates a failure result with error message(s).
    /// </summary>
    public static IdentityUserResult Failure(params string[] errors) => new()
    {
        Succeeded = false,
        Errors = errors.ToList()
    };
}
