using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services.Interfaces;

namespace Studiobook_backend.Services
{
    public class VerificationTokenService : IVerificationTokenService
    {
        private readonly AppDbContext _dbContext;

        public VerificationTokenService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<VerificationToken> CreateAsync(int userId)
        {
            var token = new VerificationToken
            {
                UserId = userId,
                Token = Guid.NewGuid().ToString("N"),
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            };

            _dbContext.VerificationTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return token;
        }

        public async Task<VerificationToken?> GetValidTokenAsync(string token)
        {
            return await _dbContext.VerificationTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Token == token &&
                    x.UsedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);
        }

        public async Task MarkAsUsedAsync(VerificationToken verificationToken)
        {
            verificationToken.UsedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}