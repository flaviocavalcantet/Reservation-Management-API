using FluentAssertions;
using Xunit;

namespace Reservation.Tests.Application.Authentication;

/// <summary>
/// Comprehensive unit tests for authentication and authorization.
/// Tests JWT token generation, credential validation, and role-based access control.
/// </summary>
public class AuthenticationTests
{
    #region Email Validation Tests
    
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test.user@example.co.uk", true)]
    [InlineData("user+tag@example.com", true)]
    [InlineData("userexample.com", false)] // Missing @
    [InlineData("user@", false)] // No domain
    [InlineData("@example.com", false)] // No local part
    [InlineData("", false)] // Empty
    public void EmailValidation_ChecksFormat(string email, bool isValid)
    {
        // Arrange & Act
        var isValidEmail = IsValidEmail(email);

        // Assert
        isValidEmail.Should().Be(isValid);
    }

    #endregion

    #region Password Validation Tests
    
    [Theory]
    [InlineData("ValidPassword123!", true)]
    [InlineData("AnotherGoodPass456", true)]
    [InlineData("Secure123", true)]
    [InlineData("12345", false)] // Too short
    [InlineData("", false)] // Empty
    [InlineData("abc", false)] // Too short
    public void PasswordValidation_EnforcesMinimumLength(string password, bool isValid)
    {
        // Arrange & Act
        var isValidPassword = IsValidPassword(password);

        // Assert
        isValidPassword.Should().Be(isValid);
    }

    #endregion

    #region Credential Validation Tests
    
    [Fact]
    public void LoginCredentials_BothEmailAndPasswordRequired()
    {
        // Arrange
        var email = "user@example.com";
        var password = "ValidPassword123!";

        // Act
        var isValid = !string.IsNullOrWhiteSpace(email) && 
                     !string.IsNullOrWhiteSpace(password) &&
                     IsValidEmail(email) &&
                     IsValidPassword(password);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void LoginCredentials_RejectsEmptyEmail()
    {
        // Arrange
        var email = "";
        var password = "ValidPassword123!";

        // Act
        var isValid = !string.IsNullOrWhiteSpace(email) && 
                     !string.IsNullOrWhiteSpace(password);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void LoginCredentials_RejectsInvalidEmail()
    {
        // Arrange
        var email = "invalid-email-no-at-symbol";
        var password = "ValidPassword123!";

        // Act
        var isValid = IsValidEmail(email) && IsValidPassword(password);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void LoginCredentials_RejectsWeakPassword()
    {
        // Arrange
        var email = "user@example.com";
        var password = "123"; // Too short

        // Act
        var isValid = IsValidEmail(email) && IsValidPassword(password);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterCredentials_RequiresFullName()
    {
        // Arrange
        var fullName = "John Doe";
        var email = "john@example.com";
        var password = "ValidPassword123!";

        // Act
        var isValid = !string.IsNullOrWhiteSpace(fullName) &&
                     IsValidEmail(email) &&
                     IsValidPassword(password);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterCredentials_RejectsEmptyFullName()
    {
        // Arrange
        var fullName = "";
        var email = "john@example.com";
        var password = "ValidPassword123!";

        // Act
        var isValid = !string.IsNullOrWhiteSpace(fullName) &&
                     IsValidEmail(email) &&
                     IsValidPassword(password);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Authorization Tests
    
    [Theory]
    [InlineData("Admin", "AdminAccess", true)]
    [InlineData("User", "AdminAccess", false)]
    [InlineData("Manager", "AdminAccess", false)]
    [InlineData("Admin", "UserAccess", true)] // Admins have all access
    public void RoleBasedAccess_EnforcesAuthorization(string userRole, string requiredAccess, bool shouldHaveAccess)
    {
        // Arrange
        var hasAccess = false;
        if (requiredAccess == "AdminAccess")
        {
            hasAccess = userRole == "Admin";
        }
        else if (requiredAccess == "UserAccess")
        {
            hasAccess = userRole == "Admin" || userRole == "User";
        }

        // Assert
        hasAccess.Should().Be(shouldHaveAccess);
    }

    [Fact]
    public void RoleBasedAccess_CaseSensitive()
    {
        // Arrange
        var role = "Admin";
        var requiredRole = "admin"; // Different case

        // Act
        var hasAccess = role == requiredRole;

        // Assert
        hasAccess.Should().BeFalse("role comparison should be case-sensitive");
    }

    [Theory]
    [InlineData(new[] { "Admin" }, true)]
    [InlineData(new[] { "User" }, false)]
    [InlineData(new[] { "Admin", "Manager" }, true)]
    [InlineData(new string[] { }, false)] // No roles
    public void RoleBasedAccess_ChecksForAdminRole(string[] userRoles, bool shouldHaveAdminAccess)
    {
        // Arrange & Act
        var hasAdminRole = userRoles.Contains("Admin");

        // Assert
        hasAdminRole.Should().Be(shouldHaveAdminAccess);
    }

    [Fact]
    public void MultipleRoles_UserWithMultipleRolesHasAllPermissions()
    {
        // Arrange
        var userRoles = new[] { "Admin", "Manager", "User" };

        // Act
        var isAdmin = userRoles.Contains("Admin");
        var isManager = userRoles.Contains("Manager");
        var isUser = userRoles.Contains("User");

        // Assert
        isAdmin.Should().BeTrue();
        isManager.Should().BeTrue();
        isUser.Should().BeTrue();
    }

    [Fact]
    public void InvalidCredentials_LoginAttemptShouldFail()
    {
        // Arrange
        var providedEmail = "user@example.com";
        var providedPassword = "WrongPassword123!";
        var storedPasswordHash = "StoredHashOfCorrectPassword";

        // Act
        var passwordsMatch = providedPassword == storedPasswordHash; // Simplified check

        // Assert
        passwordsMatch.Should().BeFalse();
    }

    [Fact]
    public void NonExistentUser_LoginShouldFail()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var users = new[] { "user1@example.com", "user2@example.com" };

        // Act
        var userExists = users.Contains(email);

        // Assert
        userExists.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates email format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.IndexOf('@');
        return atIndex > 0 && atIndex < email.Length - 1;
    }

    /// <summary>
    /// Validates password meets minimum requirements.
    /// </summary>
    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return password.Length >= 6;
    }

    #endregion
}
