using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Interfaces
{
    public interface IVerificationTokenService
    {
        Task<VerificationToken> CreateAsync(int userId);
        Task<VerificationToken?> GetValidTokenAsync(string token);
        Task MarkAsUsedAsync(VerificationToken verificationToken);
    }
}