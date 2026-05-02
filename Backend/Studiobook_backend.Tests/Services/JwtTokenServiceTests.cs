using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsValidJwt_WithExpectedClaims()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "this-is-a-test-signing-key-for-jwt-1234567890",
            AccessTokenMinutes = 60
        };

        var service = new JwtTokenService(Options.Create(settings));

        var user = new User
        {
            Id = 123,
            Name = "テストユーザー",
            Kana = "テストユーザー",
            Email = "test@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = true
        };

        var roles = new List<string>
        {
            "Host",
            "Admin"
        };

        // Act
        var token = service.GenerateToken(user, roles, out var expiresAtUtc);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);

        Assert.Equal("123", jwt.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("test@example.com", jwt.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("テストユーザー", jwt.Claims.Single(x => x.Type == "name").Value);

        var roleClaims = jwt.Claims
            .Where(x => x.Type == ClaimTypes.Role || x.Type == "role")
            .Select(x => x.Value)
            .ToList();

        Assert.Contains("Host", roleClaims);
        Assert.Contains("Admin", roleClaims);

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);

        var now = DateTime.UtcNow;
        Assert.True(expiresAtUtc > now.AddMinutes(59));
        Assert.True(expiresAtUtc <= now.AddMinutes(61));
    }

    [Fact]
    public void GenerateToken_SetsExpiresAtUtc_BasedOnAccessTokenMinutes()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "this-is-a-test-signing-key-for-jwt-1234567890",
            AccessTokenMinutes = 15
        };

        var service = new JwtTokenService(Options.Create(settings));
        var user = CreateUser();

        var before = DateTime.UtcNow;

        // Act
        var token = service.GenerateToken(
            user,
            roles: new List<string> { "Host" },
            out var expiresAtUtc);

        var after = DateTime.UtcNow;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        Assert.True(expiresAtUtc >= before.AddMinutes(15));
        Assert.True(expiresAtUtc <= after.AddMinutes(15).AddSeconds(1));
    }

    [Fact]
    public void GenerateToken_IncludesAllRoles()
    {
        // Arrange
        var settings = CreateJwtSettings();
        var service = new JwtTokenService(Options.Create(settings));
        var user = CreateUser();

        var roles = new List<string>
        {
            "Admin",
            "Host",
            "GeneralUser"
        };

        // Act
        var token = service.GenerateToken(user, roles, out _);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roleClaims = jwt.Claims
            .Where(x => x.Type == ClaimTypes.Role || x.Type == "role")
            .Select(x => x.Value)
            .ToList();

        Assert.Equal(3, roleClaims.Count);
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Host", roleClaims);
        Assert.Contains("GeneralUser", roleClaims);
    }

    [Fact]
    public void GenerateToken_CanBeValidated_WithConfiguredSigningKey()
    {
        // Arrange
        var settings = CreateJwtSettings();
        var service = new JwtTokenService(Options.Create(settings));
        var user = CreateUser();

        var token = service.GenerateToken(
            user,
            roles: new List<string> { "Host" },
            out _);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,

            ValidateAudience = true,
            ValidAudience = settings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(settings.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Act
        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, validationParameters, out var validatedToken);

        // Assert
        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal(
            "1",
            principal.FindFirstValue(ClaimTypes.NameIdentifier));

        Assert.Equal(
            "test@example.com",
            principal.FindFirstValue(ClaimTypes.Email));

        Assert.Equal(
            "テストユーザー",
            principal.FindFirstValue("name"));

        Assert.True(principal.IsInRole("Host"));
    }

    private static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "this-is-a-test-signing-key-for-jwt-1234567890",
            AccessTokenMinutes = 60
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = 1,
            Name = "テストユーザー",
            Kana = "テストユーザー",
            Email = "test@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = true
        };
    }
}