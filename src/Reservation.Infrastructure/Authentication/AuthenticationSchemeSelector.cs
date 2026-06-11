using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Reservation.Infrastructure.Authentication;

/// <summary>
/// Selects which JWT Bearer authentication scheme should validate an incoming
/// request, based on the (unvalidated) "iss" claim of the bearer token.
///
/// This allows the API to expose a single default authentication scheme to
/// <c>[Authorize]</c> while transparently supporting two token issuers:
/// - The application's own custom JWT (HS256, scheme "Bearer")
/// - Auth0-issued OIDC access tokens (RS256, scheme "Auth0")
///
/// The selector only inspects the token's claims to pick a scheme; it performs
/// NO signature validation here. Actual validation (signature, audience,
/// lifetime, etc.) is performed afterwards by the selected scheme's handler.
/// </summary>
public static class AuthenticationSchemeSelector
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Determines the authentication scheme to forward to.
    /// </summary>
    /// <param name="authorizationHeader">The raw "Authorization" request header value, if present.</param>
    /// <param name="auth0Authority">The configured Auth0 Authority (issuer URL), or empty if Auth0 is not configured.</param>
    /// <returns>"Auth0" if the token's issuer matches the configured Auth0 Authority; otherwise the default custom JWT scheme.</returns>
    public static string SelectScheme(string? authorizationHeader, string? auth0Authority)
    {
        if (!string.IsNullOrWhiteSpace(auth0Authority)
            && !string.IsNullOrWhiteSpace(authorizationHeader)
            && authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorizationHeader[BearerPrefix.Length..].Trim();

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

                if (string.Equals(jwt.Issuer, auth0Authority, StringComparison.OrdinalIgnoreCase))
                {
                    return "Auth0";
                }
            }
            catch (ArgumentException)
            {
                // Not a well-formed JWT - fall through to the default scheme,
                // whose handler will reject it with a proper 401.
            }
        }

        return JwtBearerDefaults.AuthenticationScheme;
    }
}
