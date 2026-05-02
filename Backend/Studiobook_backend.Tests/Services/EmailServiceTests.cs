using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class EmailServiceTests
{
    private static EmailService CreateService(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new EmailService(
            configuration,
            NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task SendSignupVerificationEmailAsync_Mailtrap設定不足の場合_InvalidOperationExceptionを投げる()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Mailtrap:Host"] = "",
            ["Mailtrap:Port"] = "2525",
            ["Mailtrap:UserName"] = "user",
            ["Mailtrap:Password"] = "password",
            ["Mailtrap:From"] = "noreply@example.com"
        });

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendSignupVerificationEmailAsync(
                "test@example.com",
                "https://example.com/verify"));

        // Assert
        Assert.Equal("Mailtrap settings are missing.", ex.Message);
    }

    [Fact]
    public async Task SendSignupVerificationEmailAsync_Portが不正な場合_InvalidOperationExceptionを投げる()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Mailtrap:Host"] = "localhost",
            ["Mailtrap:Port"] = "invalid-port",
            ["Mailtrap:UserName"] = "user",
            ["Mailtrap:Password"] = "password",
            ["Mailtrap:From"] = "noreply@example.com"
        });

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendSignupVerificationEmailAsync(
                "test@example.com",
                "https://example.com/verify"));

        // Assert
        Assert.Equal("Mailtrap:Port is invalid.", ex.Message);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_Mailtrap設定不足の場合_InvalidOperationExceptionを投げる()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Mailtrap:Host"] = "localhost",
            ["Mailtrap:Port"] = "2525",
            ["Mailtrap:UserName"] = "",
            ["Mailtrap:Password"] = "password",
            ["Mailtrap:From"] = "noreply@example.com"
        });

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendPasswordResetEmailAsync(
                "test@example.com",
                "https://example.com/reset"));

        // Assert
        Assert.Equal("Mailtrap settings are missing.", ex.Message);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_Portが不正な場合_InvalidOperationExceptionを投げる()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Mailtrap:Host"] = "localhost",
            ["Mailtrap:Port"] = "abc",
            ["Mailtrap:UserName"] = "user",
            ["Mailtrap:Password"] = "password",
            ["Mailtrap:From"] = "noreply@example.com"
        });

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendPasswordResetEmailAsync(
                "test@example.com",
                "https://example.com/reset"));

        // Assert
        Assert.Equal("Mailtrap:Port is invalid.", ex.Message);
    }
}