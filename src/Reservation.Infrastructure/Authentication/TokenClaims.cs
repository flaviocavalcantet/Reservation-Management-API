namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Custom JWT claim names and values used by <see cref="JwtTokenService"/>.
///
/// The "token_type" claim distinguishes access tokens from refresh tokens so that:
/// - An access token cannot be replayed at the /refresh endpoint
///   (see <see cref="JwtTokenService.ValidateRefreshToken"/>).
/// - A long-lived refresh token cannot be used as a Bearer token to call the API
///   (see AuthenticationServiceConfiguration's OnTokenValidated handler).
///
/// Both kinds of token are otherwise structurally identical (same signature, issuer,
/// audience), which is why an explicit discriminator is required.
/// </summary>
internal static class TokenClaims
{
    /// <summary>The claim name carrying the token kind.</summary>
    public const string TokenType = "token_type";

    /// <summary>Value of <see cref="TokenType"/> for short-lived API access tokens.</summary>
    public const string AccessTokenType = "access";

    /// <summary>Value of <see cref="TokenType"/> for long-lived refresh tokens.</summary>
    public const string RefreshTokenType = "refresh";
}
