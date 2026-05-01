using Studiobook_backend.Entities;

namespace Studiobook_backend.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, List<string> roles, out DateTime expiresAtUtc);
    }
}
