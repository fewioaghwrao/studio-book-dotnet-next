using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services.Interfaces;

namespace Studiobook_backend.Services
{
    public class PasswordResetService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PasswordResetService(
            AppDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task RequestResetAsync(string email)
        {
            var normalizedEmail = email.Trim();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Enabled);

            // セキュリティのため、存在しないメールでも成功扱い
            if (user == null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var activeTokens = await _dbContext.PasswordResetTokens
                .Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.ExpiresAtUtc > now)
                .ToListAsync();

            foreach (var oldToken in activeTokens)
            {
                oldToken.UsedAtUtc = now;
            }

            var token = GenerateToken();

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(1)
            };

            _dbContext.PasswordResetTokens.Add(resetToken);
            await _dbContext.SaveChangesAsync();

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

            await _emailService.SendPasswordResetEmailAsync(user.Email, resetUrl);
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword)
        {
            var now = DateTime.UtcNow;

            var resetToken = await _dbContext.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Token == token &&
                    x.UsedAtUtc == null &&
                    x.ExpiresAtUtc > now);

            if (resetToken == null)
            {
                return ResetPasswordResult.Fail("INVALID_TOKEN");
            }

            if (!resetToken.User.Enabled)
            {
                return ResetPasswordResult.Fail("USER_DISABLED");
            }

            resetToken.User.PasswordHash =
                _passwordHasher.HashPassword(resetToken.User, newPassword);

            resetToken.UsedAtUtc = now;

            await _dbContext.SaveChangesAsync();

            return ResetPasswordResult.Ok();
        }

        private static string GenerateToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        public sealed record ResetPasswordResult(bool IsSuccess, string? ErrorCode)
        {
            public static ResetPasswordResult Ok() => new(true, null);
            public static ResetPasswordResult Fail(string errorCode) => new(false, errorCode);
        }
    }
}