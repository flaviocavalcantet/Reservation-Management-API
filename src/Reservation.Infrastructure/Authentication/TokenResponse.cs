namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Represents a JWT token response returned to the client.
/// Contains both access token (short-lived) and refresh token (long-lived).
/// 
/// Used in:
/// - Login endpoint response
/// - Token refresh endpoint response
/// - Registration endpoint response
/// 
/// Flow:
/// 1. Client receives AccessToken and RefreshToken
/// 2. Client uses AccessToken for Authorization header: "Bearer {AccessToken}"
/// 3. When AccessToken expires, client uses RefreshToken to get new AccessToken
/// 4. RefreshToken doesn't change; can be reused multiple times until it expires
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// JWT access token for API authentication.
    /// Short-lived token (default: 15 minutes).
    /// Sent in Authorization header: "Authorization: Bearer {AccessToken}"
    /// 
    /// Contains claims:
    /// - sub (subject/UserId)
    /// - email
    /// - role(s)
    /// - iat (issued at)
    /// - exp (expiration)
    /// - iss (issuer)
    /// - aud (audience)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// Long-lived token (default: 7 days).
    /// Should be stored securely on client (HttpOnly cookie or secure storage).
    /// Never sent in Authorization header; used in refresh endpoint only.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (always "Bearer" for Bearer token authentication).
    /// Used in Authorization header: "Authorization: Bearer {AccessToken}"
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Access token expiration time in seconds.
    /// Helps client know when to refresh the token.
    /// Example: 900 (15 minutes)
    /// 
    /// Client should refresh token before this time expires.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User ID for reference.
    /// Included in response for client convenience.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email for reference.
    /// Included in response for client convenience.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User roles as array.
    /// Example: ["User"] or ["Admin", "User"]
    /// Included in response for client convenience.
    /// </summary>
    public string[] Roles { get; set; } = [];
}
