using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class VerificationTokenServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesVerificationToken()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        var service = new VerificationTokenService(context);

        var before = DateTime.UtcNow;

        // Act
        var token = await service.CreateAsync(userId: 1);

        var after = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.Equal(32, token.Token.Length);
        Assert.Null(token.UsedAtUtc);

        Assert.True(token.ExpiresAtUtc >= before.AddHours(24));
        Assert.True(token.ExpiresAtUtc <= after.AddHours(24).AddSeconds(1));

        var savedToken = await context.VerificationTokens.SingleAsync();

        Assert.Equal(token.Id, savedToken.Id);
        Assert.Equal(1, savedToken.UserId);
        Assert.Equal(token.Token, savedToken.Token);
    }

    [Fact]
    public async Task CreateAsync_CreatesDifferentTokens_WhenCalledMultipleTimes()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        var service = new VerificationTokenService(context);

        // Act
        var token1 = await service.CreateAsync(userId: 1);
        var token2 = await service.CreateAsync(userId: 1);

        // Assert
        Assert.NotEqual(token1.Token, token2.Token);
        Assert.Equal(2, await context.VerificationTokens.CountAsync());
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsTokenWithUser_WhenTokenIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        context.VerificationTokens.Add(new VerificationToken
        {
            Id = 1,
            UserId = 1,
            Token = "valid-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var service = new VerificationTokenService(context);

        // Act
        var result = await service.GetValidTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.UserId);
        Assert.Equal("valid-token", result.Token);
        Assert.Null(result.UsedAtUtc);
        Assert.NotNull(result.User);
        Assert.Equal("user@example.com", result.User.Email);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsNull_WhenTokenDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new VerificationTokenService(context);

        // Act
        var result = await service.GetValidTokenAsync("not-found-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsNull_WhenTokenIsUsed()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        context.VerificationTokens.Add(new VerificationToken
        {
            Id = 1,
            UserId = 1,
            Token = "used-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        await context.SaveChangesAsync();

        var service = new VerificationTokenService(context);

        // Act
        var result = await service.GetValidTokenAsync("used-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsNull_WhenTokenIsExpired()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        context.VerificationTokens.Add(new VerificationToken
        {
            Id = 1,
            UserId = 1,
            Token = "expired-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var service = new VerificationTokenService(context);

        // Act
        var result = await service.GetValidTokenAsync("expired-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAsUsedAsync_SetsUsedAtUtc()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        var verificationToken = new VerificationToken
        {
            Id = 1,
            UserId = 1,
            Token = "valid-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UsedAtUtc = null
        };

        context.VerificationTokens.Add(verificationToken);
        await context.SaveChangesAsync();

        var service = new VerificationTokenService(context);

        var before = DateTime.UtcNow;

        // Act
        await service.MarkAsUsedAsync(verificationToken);

        var after = DateTime.UtcNow;

        // Assert
        var savedToken = await context.VerificationTokens.SingleAsync(x => x.Id == 1);

        Assert.NotNull(savedToken.UsedAtUtc);
        Assert.True(savedToken.UsedAtUtc >= before);
        Assert.True(savedToken.UsedAtUtc <= after.AddSeconds(1));
    }

    private static async Task SeedUserAsync(
        AppDbContext context,
        int userId,
        string email,
        bool enabled)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = $"ユーザー{userId}",
            Kana = $"ユーザー{userId}",
            Email = email,
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = enabled
        });

        await context.SaveChangesAsync();
    }
}
