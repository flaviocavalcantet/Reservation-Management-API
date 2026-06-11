using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Reservation.Infrastructure.Authentication;
using Xunit;

namespace Reservation.Tests.Application.Authentication;

/// <summary>
/// Unit tests for Auth0 (OIDC) settings validation and the scheme-selection
/// logic that routes incoming requests to either the custom JWT scheme
/// ("Bearer") or the Auth0 scheme ("Auth0") based on the token's issuer.
/// </summary>
public class Auth0AuthenticationTests
{
    #region Auth0Settings Validation

    [Fact]
    public void Auth0Settings_NotConfigured_IsConfiguredIsFalse()
    {
        var settings = new Auth0Settings();

        settings.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Auth0Settings_NotConfigured_ValidateDoesNotThrow()
    {
        var settings = new Auth0Settings();

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Auth0Settings_WithAuthorityAndAudience_IsConfiguredIsTrue()
    {
        var settings = new Auth0Settings
        {
            Authority = "https://tenant.us.auth0.com/",
            Audience = "https://reservation-api/"
        };

        settings.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Auth0Settings_AuthorityWithoutAudience_ThrowsOnValidate()
    {
        var settings = new Auth0Settings
        {
            Authority = "https://tenant.us.auth0.com/"
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audience*");
    }

    [Fact]
    public void Auth0Settings_NonHttpsAuthority_ThrowsOnValidate()
    {
        var settings = new Auth0Settings
        {
            Authority = "http://tenant.us.auth0.com/",
            Audience = "https://reservation-api/"
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public void Auth0Settings_DefaultRoleClaimType_IsNamespacedRolesClaim()
    {
        var settings = new Auth0Settings();

        settings.RoleClaimType.Should().Be("https://reservation-api/roles");
    }

    #endregion

    #region AuthenticationSchemeSelector

    private const string Auth0Authority = "https://tenant.us.auth0.com/";

    [Fact]
    public void SelectScheme_NoAuthorizationHeader_ReturnsCustomBearerScheme()
    {
        var scheme = AuthenticationSchemeSelector.SelectScheme(null, Auth0Authority);

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void SelectScheme_Auth0NotConfigured_ReturnsCustomBearerScheme()
    {
        var token = CreateUnsignedJwt(Auth0Authority);

        var scheme = AuthenticationSchemeSelector.SelectScheme($"Bearer {token}", auth0Authority: "");

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void SelectScheme_TokenIssuedByAuth0_ReturnsAuth0Scheme()
    {
        var token = CreateUnsignedJwt(Auth0Authority);

        var scheme = AuthenticationSchemeSelector.SelectScheme($"Bearer {token}", Auth0Authority);

        scheme.Should().Be("Auth0");
    }

    [Fact]
    public void SelectScheme_TokenIssuedByOwnApi_ReturnsCustomBearerScheme()
    {
        var token = CreateUnsignedJwt("ReservationAPI");

        var scheme = AuthenticationSchemeSelector.SelectScheme($"Bearer {token}", Auth0Authority);

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void SelectScheme_MalformedToken_ReturnsCustomBearerScheme()
    {
        var scheme = AuthenticationSchemeSelector.SelectScheme("Bearer not-a-jwt", Auth0Authority);

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void SelectScheme_NonBearerScheme_ReturnsCustomBearerScheme()
    {
        var token = CreateUnsignedJwt(Auth0Authority);

        var scheme = AuthenticationSchemeSelector.SelectScheme($"Basic {token}", Auth0Authority);

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Creates a JWT with the given issuer claim. The token is unsigned -
    /// SelectScheme only reads claims and never validates the signature.
    /// </summary>
    private static string CreateUnsignedJwt(string issuer)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: issuer,
            audience: "https://reservation-api/",
            subject: new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }),
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);

        return handler.WriteToken(token);
    }

    #endregion
}
