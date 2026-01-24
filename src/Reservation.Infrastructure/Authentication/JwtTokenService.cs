using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Reservation.Application.Authentication;
using Reservation.Infrastructure.Identity;

namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// JWT token service implementation.
/// 
/// Generates and validates JWT Bearer tokens for API authentication.
/// Uses HMAC-SHA256 for signing and validation.
/// 
/// Flow:
/// 1. GenerateAccessToken() - Create short-lived JWT for API requests
/// 2. GenerateRefreshToken() - Create long-lived JWT for token renewal
/// 3. ValidateToken() - Verify token signature and claims
/// 4. GetPrincipalFromExpiredToken() - Extract claims from expired tokens (for refresh)
/// 
/// Security:
/// - Secret key validated at startup (must be 256+ bits)
/// - HMAC-SHA256 algorithm
/// - Standard JWT claims (iss, aud, iat, exp)
/// - Custom claims (sub, email, role)
/// - No token blacklist/revocation (stateless)
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Creates a new JwtTokenService instance.
    /// Called via dependency injection with IConfiguration.
    /// </summary>
    /// <param name="configuration">Configuration provider for reading JwtSettings from appsettings.json.</param>
    /// <exception cref="InvalidOperationException">Thrown if JWT settings are missing or invalid.</exception>
    public JwtTokenService(IConfiguration configuration)
    {
        _jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section not found in configuration.");

        _jwtSettings.Validate();

        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Generates a JWT access token for the given user.
    /// 
    /// Token structure:
    /// Header:
    ///   - typ: "JWT"
    ///   - alg: "HS256"
    /// 
    /// Payload (Claims):
    ///   - sub: User ID (as string GUID)
    ///   - email: User's email
    ///   - role: User's roles (repeated claim for each role)
    ///   - iss: Issuer (from settings)
    ///   - aud: Audience (from settings)
    ///   - iat: Issued at (now in Unix time)
    ///   - exp: Expires (issued at + AccessTokenExpirationMinutes)
    /// 
    /// Signature:
    ///   - Algorithm: HMAC-SHA256
    ///   - Key: Base64-decoded SecretKey
    /// 
    /// Example token payload (decoded):
    /// {
    ///   "sub": "550e8400-e29b-41d4-a716-446655440000",
    ///   "email": "user@example.com",
    ///   "role": ["User"],
    ///   "iss": "ReservationAPI",
    ///   "aud": "ReservationWebApp",
    ///   "iat": 1705939200,
    ///   "exp": 1705940100
    /// }
    /// </summary>
    /// <param name="user">The ApplicationUser to generate token for.</param>
    /// <param name="roles">Collection of role names assigned to user.</param>
    /// <returns>Signed JWT token as base64 string.</returns>
    public string GenerateAccessToken(IApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // sub
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)   // email
        };

        // Add role claims (multiple claims with same type, one per role)
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a JWT refresh token for obtaining new access tokens.
    /// 
    /// Refresh token is similar to access token but:
    /// - Longer expiration (default: 7 days vs 15 minutes)
    /// - Simpler claims (just sub for user ID)
    /// - Not sent in API requests, only used in /refresh endpoint
    /// 
    /// Purpose:
    /// - Client uses RefreshToken to get new AccessToken without re-authenticating
    /// - AccessToken can be short-lived for security
    /// - RefreshToken is longer-lived but should be stored securely
    /// 
    /// Example flow:
    /// 1. Client logs in → receives AccessToken (15 min) + RefreshToken (7 days)
    /// 2. Client uses AccessToken for API requests
    /// 3. AccessToken expires after 15 minutes
    /// 4. Client sends RefreshToken to /api/auth/refresh
    /// 5. Server validates RefreshToken, generates new AccessToken
    /// 6. Client continues using API with new AccessToken
    /// </summary>
    /// <param name="userId">The user ID to embed in the refresh token.</param>
    /// <returns>Signed JWT refresh token as base64 string.</returns>
    public string GenerateRefreshToken(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()) // sub
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates a token and extracts its claims.
    /// 
    /// Validation checks:
    /// ✓ Token format (valid JWT structure)
    /// ✓ Signature (HMAC-SHA256 with correct key)
    /// ✓ Issuer (matches configured issuer)
    /// ✓ Audience (matches configured audience)
    /// ✓ Lifetime (not expired, accounting for clock skew)
    /// 
    /// Does NOT check:
    /// ✗ Token blacklist/revocation (stateless validation)
    /// ✗ User account status (caller's responsibility)
    /// ✗ Role authorization (done by [Authorize(Roles = "...")] attributes)
    /// 
    /// Success returns ClaimsPrincipal with:
    /// - Claims from token payload
    /// - Identity.IsAuthenticated = true
    /// - Can be cast to HttpContext.User
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>ClaimsPrincipal containing token claims; null if invalid.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                IssuerSigningKey = key,
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ClockSkew = TimeSpan.FromSeconds(_jwtSettings.ClockSkewSeconds)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Additional check: ensure it's actually a JWT (not another token type)
            if (validatedToken is not JwtSecurityToken)
                return null;

            return principal;
        }
        catch (SecurityTokenException)
        {
            // Token validation failed (expired, invalid signature, etc.)
            return null;
        }
        catch (ArgumentException)
        {
            // Invalid token format
            return null;
        }
    }

    /// <summary>
    /// Extracts claims from a token without validating lifetime.
    /// 
    /// Used in refresh token flow:
    /// 1. AccessToken is expired
    /// 2. Call GetPrincipalFromExpiredToken(expiredAccessToken)
    /// 3. Extract UserId from claims
    /// 4. Validate RefreshToken separately
    /// 5. Generate new AccessToken with same UserId
    /// 
    /// WARNING: Only validates signature, not expiration!
    /// This is intentional - we need to extract claims from expired tokens.
    /// Caller is responsible for validating RefreshToken lifetime.
    /// 
    /// Security considerations:
    /// - Only used in specific refresh endpoint, not general API auth
    /// - Still validates signature (prevents token tampering)
    /// - Expired AccessToken cannot be used for API requests
    /// </summary>
    /// <param name="token">The JWT token (possibly expired) to extract claims from.</param>
    /// <returns>ClaimsPrincipal with claims; null if signature invalid.</returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,  // Don't validate issuer in expired token
                ValidateAudience = false, // Don't validate audience in expired token
                ValidateLifetime = false, // Don't validate lifetime - we want expired tokens!
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, 
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
