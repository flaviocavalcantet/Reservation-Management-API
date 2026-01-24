using Microsoft.AspNetCore.Identity;

namespace Reservation.Infrastructure.Identity;

/// <summary>
/// Application user extending ASP.NET Core Identity.
/// Represents a user account in the Reservation Management System.
/// 
/// Design Notes:
/// - Extends IdentityUser{Guid} for GUID-based user IDs
/// - Contains only Identity-related concerns (not domain logic)
/// - Maps to AspNetUsers and related Identity tables
/// - Password hashing handled automatically by Identity
/// - Role management through Identity's role system
/// 
/// This class lives in Infrastructure layer to keep Identity decoupled from Domain.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Full name of the user for display purposes.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// User creation timestamp (UTC).
    /// Redundant with CreatedDate from IdentityUser but kept for clarity.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login timestamp (UTC).
    /// Used for tracking user activity and security audits.
    /// </summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Indicates if the user account is active.
    /// Can be set to false for soft-delete or user deactivation.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User deactivation/deletion timestamp (UTC).
    /// Set when user account is deactivated or deleted.
    /// </summary>
    public DateTime? DeactivatedAtUtc { get; set; }
}
