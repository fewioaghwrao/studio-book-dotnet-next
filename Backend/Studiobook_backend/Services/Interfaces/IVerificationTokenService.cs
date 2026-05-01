using Studiobook_backend.Entities;

namespace Studiobook_backend.Services.Interfaces
{
    public interface IVerificationTokenService
    {
        Task<VerificationToken> CreateAsync(int userId);
        Task<VerificationToken?> GetValidTokenAsync(string token);
        Task MarkAsUsedAsync(VerificationToken verificationToken);
    }
}