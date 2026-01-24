namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// JWT configuration settings loaded from appsettings.json.
/// 
/// These settings control token generation and validation for Bearer authentication.
/// Example appsettings.json:
/// <code>
/// "JwtSettings": {
///   "SecretKey": "your-256-bit-base64-key-here",
///   "Issuer": "YourAppName",
///   "Audience": "YourAppClients",
///   "AccessTokenExpirationMinutes": 15,
///   "RefreshTokenExpirationDays": 7,
///   "ValidateIssuerSigningKey": true,
///   "ValidateIssuer": true,
///   "ValidateAudience": true,
///   "ValidateLifetime": true,
///   "ClockSkewSeconds": 0
/// }
/// </code>
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens.
    /// Must be at least 256 bits (32 bytes) for HMAC-SHA256.
    /// Typically stored in secure configuration (environment variables, Key Vault).
    /// 
    /// Generate a secure key:
    /// <code>
    /// using System.Security.Cryptography;
    /// var key = new byte[32];
    /// using (var rng = RandomNumberGenerator.Create())
    /// {
    ///     rng.GetBytes(key);
    /// }
    /// string base64Key = Convert.ToBase64String(key);
    /// </code>
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (typically your application name).
    /// Used in JWT "iss" claim for validation.
    /// Example: "ReservationAPI"
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (typically your application/client name).
    /// Used in JWT "aud" claim for validation.
    /// Example: "ReservationWebApp" or "ReservationMobileApp"
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// Short-lived tokens (typically 5-30 minutes).
    /// Recommended: 15 minutes for security.
    /// Default: 15
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days.
    /// Longer-lived tokens for obtaining new access tokens.
    /// Recommended: 7 days for security.
    /// Default: 7
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Whether to validate that the token is signed with the expected key.
    /// Should be true in production.
    /// Default: true
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Whether to validate the token's issuer claim.
    /// Should be true to prevent tokens from other issuers.
    /// Default: true
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the token's audience claim.
    /// Should be true to ensure tokens are intended for this application.
    /// Default: true
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token's expiration time.
    /// Should be true to reject expired tokens.
    /// Default: true
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in seconds for lifetime validation.
    /// Accounts for small time differences between servers.
    /// Default: 0 (no skew - strict validation)
    /// Recommended for production: 0-5 seconds
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 0;

    /// <summary>
    /// Validates that required settings are provided.
    /// Called during configuration to fail fast on startup if settings are missing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if required settings are missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("JwtSettings.SecretKey is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JwtSettings.Issuer is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JwtSettings.Audience is required and cannot be empty.");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("JwtSettings.AccessTokenExpirationMinutes must be greater than 0.");

        if (RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("JwtSettings.RefreshTokenExpirationDays must be greater than 0.");

        // Validate secret key length for HMAC-SHA256 (minimum 256 bits = 32 bytes)
        try
        {
            var keyBytes = Convert.FromBase64String(SecretKey);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException(
                    "JwtSettings.SecretKey must be at least 256 bits (32 bytes). " +
                    $"Current length: {keyBytes.Length * 8} bits.");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "JwtSettings.SecretKey must be a valid Base64-encoded string.");
        }
    }
}
