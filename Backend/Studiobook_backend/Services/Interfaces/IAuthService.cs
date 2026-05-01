using Studiobook_backend.Dtos;
using System.Security.Claims;

namespace Studiobook_backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<MeResponseDto> GetCurrentUserAsync(ClaimsPrincipal user);

        Task SignupAsync(SignupRequestDto request, string requestBaseUrl);
        Task<bool> VerifySignupAsync(string token);

        Task<MeResponseDto?> UpdateCurrentUserAsync(ClaimsPrincipal user, UpdateCurrentUserRequestDto request);
    }
}