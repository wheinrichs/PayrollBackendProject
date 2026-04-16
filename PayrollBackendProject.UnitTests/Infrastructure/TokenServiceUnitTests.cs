using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PayrollBackendProject.UnitTests;

public class JwtTokenServiceUnitTests
{
    private IConfiguration CreateConfig(
        string key = "super_secret_key_1234567890123456",
        string issuer = "test_issuer",
        string audience = "test_audience",
        string expiration = "60")
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", key },
            { "Jwt:Issuer", issuer },
            { "Jwt:Audience", audience },
            { "Jwt:ExpirationMinutes", expiration }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    private UserAccount CreateUser()
    {
        return UserAccount.GenerateUserAccount("test@test.com", "password", "A", "B", RoleEnum.ADMIN, null);
    }

    /*
    Test that generating a token returns a non-empty string
    */
    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtTokenService(config);
        var user = CreateUser();

        // Act
        var token = service.GenerateToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    /*
    Test that the generated token contains the correct claims
    */
    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtTokenService(config);
        var user = CreateUser();

        var handler = new JwtSecurityTokenHandler();

        // Act
        var tokenString = service.GenerateToken(user);
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    /*
    Test that the token contains the correct issuer and audience
    */
    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var config = CreateConfig(issuer: "my_issuer", audience: "my_audience");
        var service = new JwtTokenService(config);
        var user = CreateUser();

        var handler = new JwtSecurityTokenHandler();

        // Act
        var tokenString = service.GenerateToken(user);
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        Assert.Equal("my_issuer", token.Issuer);
        Assert.Contains("my_audience", token.Audiences);
    }

    /*
    Test that the token expiration is set correctly
    */
    [Fact]
    public void GenerateToken_SetsExpirationCorrectly()
    {
        // Arrange
        var expirationMinutes = 30;
        var config = CreateConfig(expiration: expirationMinutes.ToString());
        var service = new JwtTokenService(config);
        var user = CreateUser();

        var handler = new JwtSecurityTokenHandler();

        var before = DateTime.UtcNow;

        // Act
        var tokenString = service.GenerateToken(user);
        var token = handler.ReadJwtToken(tokenString);

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(token.ValidTo >= before.AddMinutes(expirationMinutes - 1));
        Assert.True(token.ValidTo <= after.AddMinutes(expirationMinutes + 1));
    }

    /*
    Test that missing JWT key throws an exception
    */
    [Fact]
    public void GenerateToken_MissingKey_ThrowsException()
    {
        // Arrange
        var config = CreateConfig(key: null!);
        var service = new JwtTokenService(config);
        var user = CreateUser();

        // Act & Assert
        Assert.Throws<Exception>(() => service.GenerateToken(user));
    }

    /*
    Test that missing expiration throws an exception
    */
    [Fact]
    public void GenerateToken_MissingExpiration_ThrowsException()
    {
        // Arrange
        var config = CreateConfig(expiration: null!);
        var service = new JwtTokenService(config);
        var user = CreateUser();

        // Act & Assert
        Assert.Throws<Exception>(() => service.GenerateToken(user));
    }
}