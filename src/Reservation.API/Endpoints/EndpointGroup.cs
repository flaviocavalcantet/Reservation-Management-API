namespace Reservation.API.Endpoints;

/// <summary>
/// Base class for endpoint groups. Follows the Vertical Slice Architecture pattern
/// for organizing API endpoints by feature rather than by layer.
/// </summary>
public abstract class EndpointGroup
{
    /// <summary>
    /// Maps all endpoints for this group to the application
    /// </summary>
    public abstract void Map(WebApplication app);
}
