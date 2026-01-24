using Reservation.Application.Authentication;
using Reservation.Infrastructure.Identity;

namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Extension methods for ApplicationUser to IApplicationUser conversion.
/// 
/// Since ApplicationUser now implements IApplicationUser directly,
/// these methods are optional but provide explicit conversion support.
/// </summary>
public static class ApplicationUserAdapter
{
    /// <summary>
    /// Converts an ApplicationUser to IApplicationUser interface.
    /// Since ApplicationUser implements IApplicationUser, this is a simple cast.
    /// </summary>
    /// <param name="user">The ApplicationUser to adapt.</param>
    /// <returns>The user as IApplicationUser.</returns>
    public static IApplicationUser ToApplicationUser(this ApplicationUser user)
    {
        return user;
    }
}
