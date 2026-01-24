using System.Security.Claims;

namespace Reservation.Application.Authentication;

/// <summary>
/// Represents a user for token generation.
/// This interface decouples Application from Infrastructure Identity types.
/// </summary>
public interface IApplicationUser
{
    /// <summary>Gets the unique user identifier.</summary>
    Guid Id { get; }

    /// <summary>Gets the user's email address.</summary>
    string Email { get; }

    /// <summary>Gets the user's assigned roles.</summary>
    IList<string> Roles { get; }
}

/// <summary>
/// Interface for JWT token generation and validation.
/// 
/// Responsibilities:
/// - Generate access tokens (short-lived, for API requests)
/// - Generate refresh tokens (long-lived, for obtaining new access tokens)
/// - Validate token signatures
/// - Extract claims from tokens
/// 
/// Design:
/// - Lives in Application layer as abstraction
/// - Implementation in Infrastructure layer
/// - Used by Application handlers for token operations
/// - Depends on IConfiguration for JWT settings
/// - Uses System.IdentityModel.Tokens.Jwt for token creation
/// 
/// Security:
/// - Signs with HMAC-SHA256 using secret key
/// - Includes standard claims: sub, email, role, iat, exp, iss, aud
/// - Validates issuer, audience, signature, lifetime
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a new JWT access token for the given user.
    /// 
    /// Token contains:
    /// - Unique subject identifier (UserId as GUID string)
    /// - User email
    /// - User roles
    /// - Standard JWT claims (iss, aud, iat, exp)
    /// 
    /// The token is signed with HMAC-SHA256 using the configured secret key.
    /// Short-lived (default: 15 minutes).
    /// </summary>
    /// <param name="user">The user to generate token for.</param>
    /// <param name="roles">User's assigned roles (e.g., ["User"], ["Admin", "User"]).</param>
    /// <returns>Signed JWT access token as a string.</returns>
    string GenerateAccessToken(IApplicationUser user, IEnumerable<string> roles);

    /// <summary>
    /// Generates a new refresh token for obtaining new access tokens.
    /// 
    /// Refresh tokens are:
    /// - Longer-lived than access tokens (default: 7 days)
    /// - Stateless (can be validated without database lookup)
    /// - Opaque to client (no claims information)
    /// - Used only in refresh endpoint
    /// 
    /// Typically stored securely on client (HttpOnly cookie).
    /// Should NOT be sent in every API request.
    /// </summary>
    /// <param name="userId">The user ID to bind token to.</param>
    /// <returns>Signed JWT refresh token as a string.</returns>
    string GenerateRefreshToken(Guid userId);

    /// <summary>
    /// Validates a token and extracts its claims.
    /// 
    /// Validation includes:
    /// - Signature verification (HMAC-SHA256)
    /// - Issuer validation
    /// - Audience validation
    /// - Lifetime validation (not expired)
    /// - Format validation
    /// 
    /// Does NOT check database or token blacklist.
    /// For refresh tokens, caller should verify UserId in claims.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>ClaimsPrincipal with extracted claims if valid; null if invalid.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts the principal (claims) from a token without full validation.
    /// Used when you want claims even from expired tokens (e.g., refresh flow).
    /// 
    /// WARNING: Only validates signature, not lifetime. Use ValidateToken() for normal flow.
    /// </summary>
    /// <param name="token">The JWT token to extract claims from.</param>
    /// <returns>ClaimsPrincipal with claims; null if signature invalid.</returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
