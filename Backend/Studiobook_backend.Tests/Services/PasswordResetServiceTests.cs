using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Services.Interfaces;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestResetAsync_CreatesTokenAndSendsEmail_WhenUserExistsAndEnabled()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        var fakeEmailService = new FakeEmailService();
        var service = CreateService(context, fakeEmailService);

        // Act
        await service.RequestResetAsync(" user@example.com ");

        // Assert
        var token = await context.PasswordResetTokens.SingleAsync();

        Assert.Equal(1, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.Null(token.UsedAtUtc);
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
        Assert.True(token.ExpiresAtUtc <= DateTime.UtcNow.AddHours(1).AddMinutes(1));

        var sent = Assert.Single(fakeEmailService.PasswordResetEmails);
        Assert.Equal("user@example.com", sent.ToEmail);
        Assert.StartsWith("https://frontend.example.com/reset-password?token=", sent.ResetUrl);
        Assert.Contains(Uri.EscapeDataString(token.Token), sent.ResetUrl);
    }

    [Fact]
    public async Task RequestResetAsync_DoesNothing_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var fakeEmailService = new FakeEmailService();
        var service = CreateService(context, fakeEmailService);

        // Act
        await service.RequestResetAsync("notfound@example.com");

        // Assert
        Assert.Empty(context.PasswordResetTokens);
        Assert.Empty(fakeEmailService.PasswordResetEmails);
    }

    [Fact]
    public async Task RequestResetAsync_DoesNothing_WhenUserIsDisabled()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "disabled@example.com",
            enabled: false);

        var fakeEmailService = new FakeEmailService();
        var service = CreateService(context, fakeEmailService);

        // Act
        await service.RequestResetAsync("disabled@example.com");

        // Assert
        Assert.Empty(context.PasswordResetTokens);
        Assert.Empty(fakeEmailService.PasswordResetEmails);
    }

    [Fact]
    public async Task RequestResetAsync_MarksExistingActiveTokensAsUsed()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        var now = DateTime.UtcNow;

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            UserId = 1,
            Token = "old-active-token",
            CreatedAtUtc = now.AddMinutes(-10),
            ExpiresAtUtc = now.AddMinutes(50),
            UsedAtUtc = null
        });

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 2,
            UserId = 1,
            Token = "old-expired-token",
            CreatedAtUtc = now.AddHours(-2),
            ExpiresAtUtc = now.AddHours(-1),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var fakeEmailService = new FakeEmailService();
        var service = CreateService(context, fakeEmailService);

        // Act
        await service.RequestResetAsync("user@example.com");

        // Assert
        var tokens = await context.PasswordResetTokens
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(3, tokens.Count);

        Assert.Equal("old-active-token", tokens[0].Token);
        Assert.NotNull(tokens[0].UsedAtUtc);

        Assert.Equal("old-expired-token", tokens[1].Token);
        Assert.Null(tokens[1].UsedAtUtc);

        Assert.NotEqual("old-active-token", tokens[2].Token);
        Assert.NotEqual("old-expired-token", tokens[2].Token);
        Assert.Null(tokens[2].UsedAtUtc);

        Assert.Single(fakeEmailService.PasswordResetEmails);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndMarksTokenUsed_WhenTokenIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new PasswordHasher<User>();

        var user = CreateUser(
            userId: 1,
            email: "user@example.com",
            enabled: true);

        user.PasswordHash = passwordHasher.HashPassword(user, "OldPassword123!");

        context.Users.Add(user);

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            UserId = 1,
            Token = "valid-token",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(50),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var fakeEmailService = new FakeEmailService();
        var service = CreateService(context, fakeEmailService, passwordHasher);

        // Act
        var result = await service.ResetPasswordAsync(
            token: "valid-token",
            newPassword: "NewPassword123!");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);

        var updatedUser = await context.Users.SingleAsync(x => x.Id == 1);
        var verifyNewPassword = passwordHasher.VerifyHashedPassword(
            updatedUser,
            updatedUser.PasswordHash,
            "NewPassword123!");

        var verifyOldPassword = passwordHasher.VerifyHashedPassword(
            updatedUser,
            updatedUser.PasswordHash,
            "OldPassword123!");

        Assert.True(
            verifyNewPassword == PasswordVerificationResult.Success ||
            verifyNewPassword == PasswordVerificationResult.SuccessRehashNeeded);

        Assert.Equal(PasswordVerificationResult.Failed, verifyOldPassword);

        var resetToken = await context.PasswordResetTokens.SingleAsync(x => x.Token == "valid-token");
        Assert.NotNull(resetToken.UsedAtUtc);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsInvalidToken_WhenTokenDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = CreateService(context, new FakeEmailService());

        // Act
        var result = await service.ResetPasswordAsync(
            token: "not-found-token",
            newPassword: "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.ErrorCode);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsInvalidToken_WhenTokenIsExpired()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            UserId = 1,
            Token = "expired-token",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var service = CreateService(context, new FakeEmailService());

        // Act
        var result = await service.ResetPasswordAsync(
            token: "expired-token",
            newPassword: "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.ErrorCode);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsInvalidToken_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "user@example.com",
            enabled: true);

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            UserId = 1,
            Token = "used-token",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(50),
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        await context.SaveChangesAsync();

        var service = CreateService(context, new FakeEmailService());

        // Act
        var result = await service.ResetPasswordAsync(
            token: "used-token",
            newPassword: "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.ErrorCode);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsUserDisabled_WhenUserIsDisabled()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedUserAsync(
            context,
            userId: 1,
            email: "disabled@example.com",
            enabled: false);

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            UserId = 1,
            Token = "valid-token",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(50),
            UsedAtUtc = null
        });

        await context.SaveChangesAsync();

        var service = CreateService(context, new FakeEmailService());

        // Act
        var result = await service.ResetPasswordAsync(
            token: "valid-token",
            newPassword: "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_DISABLED", result.ErrorCode);

        var resetToken = await context.PasswordResetTokens.SingleAsync(x => x.Token == "valid-token");
        Assert.Null(resetToken.UsedAtUtc);
    }

    private static PasswordResetService CreateService(
        AppDbContext context,
        FakeEmailService fakeEmailService,
        IPasswordHasher<User>? passwordHasher = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://frontend.example.com"
            })
            .Build();

        return new PasswordResetService(
            context,
            passwordHasher ?? new PasswordHasher<User>(),
            fakeEmailService,
            configuration);
    }

    private static async Task SeedUserAsync(
        AppDbContext context,
        int userId,
        string email,
        bool enabled)
    {
        context.Users.Add(CreateUser(userId, email, enabled));
        await context.SaveChangesAsync();
    }

    private static User CreateUser(
        int userId,
        string email,
        bool enabled)
    {
        return new User
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
        };
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<(string ToEmail, string VerifyUrl)> SignupVerificationEmails { get; } = new();

        public List<(string ToEmail, string ResetUrl)> PasswordResetEmails { get; } = new();

        public Task SendSignupVerificationEmailAsync(string toEmail, string verifyUrl)
        {
            SignupVerificationEmails.Add((toEmail, verifyUrl));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
        {
            PasswordResetEmails.Add((toEmail, resetUrl));
            return Task.CompletedTask;
        }
    }
}