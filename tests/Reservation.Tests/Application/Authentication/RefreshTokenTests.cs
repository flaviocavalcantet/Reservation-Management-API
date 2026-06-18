using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Reservation.Application.Authentication;
using Reservation.Infrastructure.Authentication;
using Xunit;

namespace Reservation.Tests.Application.Authentication;

/// <summary>
/// Unit tests for the stateless refresh-token flow in <see cref="JwtTokenService"/>.
///
/// Covers the round-trip (refresh token -> user id), the guard that stops an access
/// token from being replayed at the refresh endpoint, and rejection of expired or
/// malformed tokens.
/// </summary>
public class RefreshTokenTests
{
    // 32-byte (256-bit) Base64 key - the minimum length JwtSettings.Validate() requires.
    private const string TestSecretKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string Issuer = "ReservationAPI";
    private const string Audience = "ReservationWebApp";

    private static JwtTokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = TestSecretKey,
                ["JwtSettings:Issuer"] = Issuer,
                ["JwtSettings:Audience"] = Audience,
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["JwtSettings:ValidateIssuerSigningKey"] = "true",
                ["JwtSettings:ValidateIssuer"] = "true",
                ["JwtSettings:ValidateAudience"] = "true",
                ["JwtSettings:ValidateLifetime"] = "true",
                ["JwtSettings:ClockSkewSeconds"] = "0"
            })
            .Build();

        return new JwtTokenService(configuration);
    }

    [Fact]
    public void ValidateRefreshToken_WithFreshRefreshToken_ReturnsUserId()
    {
        var service = CreateTokenService();
        var userId = Guid.NewGuid();

        var refreshToken = service.GenerateRefreshToken(userId);
        var result = service.ValidateRefreshToken(refreshToken);

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_WithAccessToken_ReturnsNull()
    {
        var service = CreateTokenService();
        var user = new FakeUser(Guid.NewGuid(), "user@example.com");

        // An access token shares the signature/issuer/audience but carries
        // token_type "access" - it must not be accepted at the refresh endpoint.
        var accessToken = service.GenerateAccessToken(user, user.Roles);
        var result = service.ValidateRefreshToken(accessToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("aaa.bbb.ccc")]
    public void ValidateRefreshToken_WithMalformedToken_ReturnsNull(string token)
    {
        var service = CreateTokenService();

        service.ValidateRefreshToken(token).Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_WithExpiredRefreshToken_ReturnsNull()
    {
        var service = CreateTokenService();

        // Correctly signed refresh-type token, but its lifetime has already lapsed.
        var expiredToken = CreateRefreshToken(
            Guid.NewGuid(),
            notBefore: DateTime.UtcNow.AddDays(-2),
            expires: DateTime.UtcNow.AddDays(-1));

        service.ValidateRefreshToken(expiredToken).Should().BeNull();
    }

    /// <summary>
    /// Builds a refresh-type JWT (token_type = "refresh") signed with the test key,
    /// allowing the lifetime window to be set explicitly for expiry testing.
    /// </summary>
    private static string CreateRefreshToken(Guid userId, DateTime notBefore, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "refresh")
            }),
            NotBefore = notBefore,
            Expires = expires,
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = credentials
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private sealed class FakeUser : IApplicationUser
    {
        public FakeUser(Guid id, string email)
        {
            Id = id;
            Email = email;
        }

        public Guid Id { get; }
        public string Email { get; }
        public IList<string> Roles { get; } = new List<string> { "User" };
    }
}
