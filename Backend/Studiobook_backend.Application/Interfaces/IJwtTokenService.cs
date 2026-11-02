using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, List<string> roles, out DateTime expiresAtUtc);
    }
}
