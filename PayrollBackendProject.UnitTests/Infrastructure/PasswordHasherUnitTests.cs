using Xunit;
using PayrollBackendProject.Infrastructure.Auth;

namespace PayrollBackendProject.UnitTests;

public class PasswordHasherUnitTests
{
    /*
    Test that hashing a password returns a non-null, non-empty string
    */
    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";

        // Act
        var hash = hasher.Hash(password);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    /*
    Test that hashing the same password twice produces different hashes (salted)
    */
    [Fact]
    public void Hash_SamePassword_ProducesDifferentHashes()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";

        // Act
        var hash1 = hasher.Hash(password);
        var hash2 = hasher.Hash(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    /*
    Test that verifying a correct password returns true
    */
    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";
        var hash = hasher.Hash(password);

        // Act
        var result = hasher.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    /*
    Test that verifying an incorrect password returns false
    */
    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword!";
        var hash = hasher.Hash(password);

        // Act
        var result = hasher.Verify(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    /*
    Test that verifying against a tampered hash returns false
    */
    [Fact]
    public void Verify_TamperedHash_ReturnsFalse()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";
        var hash = hasher.Hash(password);

        // Tamper with the hash
        var tamperedHash = hash.Substring(0, hash.Length - 1) + "X";

        // Act
        var result = hasher.Verify(password, tamperedHash);

        // Assert
        Assert.False(result);
    }
}