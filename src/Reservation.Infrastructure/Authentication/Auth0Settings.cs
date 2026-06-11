namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Auth0 (OIDC) configuration settings loaded from appsettings.json.
///
/// These settings configure a second JWT Bearer scheme ("Auth0") that validates
/// access tokens issued by an Auth0 tenant via the OAuth2 Authorization Code +
/// PKCE flow. This scheme runs alongside the existing custom "Bearer" (HS256)
/// scheme - neither replaces the other.
///
/// Example appsettings.json:
/// <code>
/// "Auth0Settings": {
///   "Authority": "https://YOUR_TENANT.us.auth0.com/",
///   "Audience": "https://reservation-api/",
///   "RoleClaimType": "https://reservation-api/roles"
/// }
/// </code>
///
/// If <see cref="Authority"/> is left empty, the Auth0 scheme is not registered
/// and the API behaves exactly as before (custom JWT only). This keeps the
/// feature opt-in for environments (tests, CI, local dev) that do not have an
/// Auth0 tenant configured.
/// </summary>
public class Auth0Settings
{
    /// <summary>
    /// The Auth0 tenant issuer URL, e.g. "https://your-tenant.us.auth0.com/".
    /// Used as both the OIDC discovery authority (for fetching signing keys via
    /// JWKS) and the expected "iss" claim on incoming tokens.
    /// Must include the trailing slash to match Auth0's "iss" claim format.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The API identifier configured for this API in Auth0 (Applications -> APIs).
    /// Used to validate the "aud" claim on incoming access tokens.
    /// Example: "https://reservation-api/"
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The claim type used to read role information from Auth0 access tokens.
    ///
    /// Auth0 does not include roles in access tokens by default. A namespaced
    /// custom claim must be added via an Auth0 Action (see AUTHENTICATION.md,
    /// "OIDC Integration" section). This value tells the JWT Bearer handler
    /// which claim to map onto <see cref="System.Security.Claims.ClaimTypes.Role"/>
    /// so that existing <c>[Authorize(Roles = "...")]</c> checks keep working.
    ///
    /// Default: "https://reservation-api/roles"
    /// </summary>
    public string RoleClaimType { get; set; } = "https://reservation-api/roles";

    /// <summary>
    /// Whether the Auth0 scheme should be registered.
    /// True when <see cref="Authority"/> has been configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority);

    /// <summary>
    /// Validates that required settings are provided when Auth0 is configured.
    /// Called during startup to fail fast if the configuration is incomplete.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if Authority is set but Audience is missing.</exception>
    public void Validate()
    {
        if (!IsConfigured)
            return;

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "Auth0Settings.Audience is required when Auth0Settings.Authority is configured.");

        if (!Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Auth0Settings.Authority must be an HTTPS URL, e.g. \"https://your-tenant.us.auth0.com/\".");
    }
}
