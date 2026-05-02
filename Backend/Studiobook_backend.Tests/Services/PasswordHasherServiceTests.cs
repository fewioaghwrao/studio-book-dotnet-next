using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class PasswordHasherServiceTests
{
    [Fact]
    public void Hash_ReturnsHashedPassword()
    {
        // Arrange
        var service = new PasswordHasherService();
        var password = "Password123!";

        // Act
        var hashed = service.Hash(password);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hashed));
        Assert.NotEqual(password, hashed);
    }

    [Fact]
    public void Verify_ReturnsTrue_WhenPasswordIsCorrect()
    {
        // Arrange
        var service = new PasswordHasherService();
        var password = "Password123!";
        var hashed = service.Hash(password);

        // Act
        var result = service.Verify(hashed, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordIsWrong()
    {
        // Arrange
        var service = new PasswordHasherService();
        var hashed = service.Hash("Password123!");

        // Act
        var result = service.Verify(hashed, "WrongPassword!");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_GeneratesDifferentHashes_ForSamePassword()
    {
        // Arrange
        var service = new PasswordHasherService();
        var password = "Password123!";

        // Act
        var hash1 = service.Hash(password);
        var hash2 = service.Hash(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(service.Verify(hash1, password));
        Assert.True(service.Verify(hash2, password));
    }
}