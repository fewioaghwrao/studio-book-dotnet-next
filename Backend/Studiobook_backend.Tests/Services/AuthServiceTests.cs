using Moq;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task IssueTokenAsync_ユーザーのロールを重複なしで渡してトークンを発行する()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var hostRole = new Role { Id = 1, Name = "Host" };
        var userRole = new Role { Id = 2, Name = "User" };

        context.Users.Add(new User
        {
            Id = 10,
            Name = "テストユーザー",
            Kana = "テストユーザー",
            Email = "user10@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "host",
            Enabled = true,
            UserRoles = new List<UserRole>
            {
                new UserRole { Id = 1, Role = hostRole },
                new UserRole { Id = 2, Role = userRole },
                new UserRole { Id = 3, Role = hostRole }
            }
        });

        await context.SaveChangesAsync();

        var jwtTokenService = new FakeJwtTokenService();
        var service = CreateService(context, jwtTokenService);

        // Act
        var result = await service.IssueTokenAsync(10);

        // Assert
        Assert.Equal("fake-token", result.Token);
        Assert.Equal(FakeJwtTokenService.ExpiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal(10, result.UserId);
        Assert.Equal("テストユーザー", result.UserName);

        Assert.Equal(10, jwtTokenService.ReceivedUser?.Id);
        Assert.Equal(
            new[] { "Host", "User" },
            jwtTokenService.ReceivedRoles!.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task IssueTokenAsync_ユーザーが存在しない場合_例外を投げる()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = CreateService(context, new FakeJwtTokenService());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IssueTokenAsync(999));
    }

    private static AuthService CreateService(
        IAppDbContext context,
        IJwtTokenService jwtTokenService)
    {
        return new AuthService(
            context,
            Mock.Of<IPasswordHasherService>(),
            Mock.Of<IVerificationTokenService>(),
            Mock.Of<IEmailService>(),
            jwtTokenService);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public static readonly DateTime ExpiresAtUtc = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        public User? ReceivedUser { get; private set; }

        public List<string>? ReceivedRoles { get; private set; }

        public string GenerateToken(User user, List<string> roles, out DateTime expiresAtUtc)
        {
            ReceivedUser = user;
            ReceivedRoles = roles;
            expiresAtUtc = ExpiresAtUtc;

            return "fake-token";
        }
    }
}
