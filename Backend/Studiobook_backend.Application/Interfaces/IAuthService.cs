using Studiobook_backend.Application.Dtos;
using System.Security.Claims;

namespace Studiobook_backend.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<AuthTokenResultDto> IssueTokenAsync(int userId);
        Task<MeResponseDto> GetCurrentUserAsync(ClaimsPrincipal user);

        Task SignupAsync(SignupRequestDto request, string requestBaseUrl);
        Task<bool> VerifySignupAsync(string token);

        Task<MeResponseDto?> UpdateCurrentUserAsync(ClaimsPrincipal user, UpdateCurrentUserRequestDto request);
    }
}