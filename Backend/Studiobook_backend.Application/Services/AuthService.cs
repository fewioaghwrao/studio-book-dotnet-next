using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Studiobook_backend.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IVerificationTokenService _verificationTokenService;
        private readonly IEmailService _emailService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
            IAppDbContext dbContext,
            IPasswordHasherService passwordHasherService,
            IVerificationTokenService verificationTokenService,
            IEmailService emailService,
            IJwtTokenService jwtTokenService)
        {
            _dbContext = dbContext;
            _passwordHasherService = passwordHasherService;
            _verificationTokenService = verificationTokenService;
            _emailService = emailService;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// ログイン成功後、ユーザーのロールを読み込んでアクセストークン（JWT）を発行します。
        /// </summary>
        public async Task<AuthTokenResultDto> IssueTokenAsync(int userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == userId);

            var roles = user.UserRoles
                .Select(x => x.Role.Name)
                .Distinct()
                .ToList();

            var token = _jwtTokenService.GenerateToken(user, roles, out var expiresAtUtc);

            return new AuthTokenResultDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                UserName = user.Name
            };
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                return null;
            }

            if (!user.Enabled)
            {
                throw new UnauthorizedAccessException("USER_DISABLED");
            }

            var passwordValid = _passwordHasherService.Verify(user.PasswordHash, request.Password);
            if (!passwordValid)
            {
                return null;
            }

            var roles = user.UserRoles
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToList();

            var redirectTo = ResolveRedirectPath(roles);

            return new LoginResponseDto
            {
                User = new CurrentUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Kana = user.Kana,
                    Email = user.Email,
                    PostalCode = user.PostalCode,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    UsageType = user.UsageType,
                    Roles = roles
                },
                RedirectTo = redirectTo
            };
        }

        public async Task<MeResponseDto> GetCurrentUserAsync(ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue(JwtClaimNames.Sub)
                              ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return new MeResponseDto
                {
                    IsAuthenticated = false,
                    User = null
                };
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.Enabled)
            {
                return new MeResponseDto
                {
                    IsAuthenticated = false,
                    User = null
                };
            }

            var roles = user.UserRoles
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToList();

            return new MeResponseDto
            {
                IsAuthenticated = true,
                User = new CurrentUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Kana = user.Kana,
                    Email = user.Email,
                    PostalCode = user.PostalCode,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    UsageType = user.UsageType,
                    Roles = roles
                }
            };
        }

        public async Task SignupAsync(SignupRequestDto request, string requestBaseUrl)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail))
            {
                throw new InvalidOperationException("EMAIL_ALREADY_REGISTERED");
            }

            if (request.Password != request.PasswordConfirmation)
            {
                throw new InvalidOperationException("PASSWORD_MISMATCH");
            }

            var generalUserRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "GeneralUser");
            if (generalUserRole == null)
            {
                throw new InvalidOperationException("ROLE_NOT_FOUND");
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Kana = request.Kana.Trim(),
                Email = normalizedEmail,
                PasswordHash = _passwordHasherService.Hash(request.Password),
                PostalCode = request.PostalCode.Trim(),
                Address = request.Address.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                UsageType = request.UsageType.Trim(),
                Enabled = false
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = generalUserRole.Id
            });
            await _dbContext.SaveChangesAsync();

            var verificationToken = await _verificationTokenService.CreateAsync(user.Id);

            var verifyUrl = $"{requestBaseUrl}/api/auth/verify?token={verificationToken.Token}";
            await _emailService.SendSignupVerificationEmailAsync(user.Email, verifyUrl);
        }

        public async Task<bool> VerifySignupAsync(string token)
        {
            var verificationToken = await _verificationTokenService.GetValidTokenAsync(token);
            if (verificationToken == null)
            {
                return false;
            }

            verificationToken.User.Enabled = true;
            verificationToken.User.EmailVerifiedAtUtc = DateTime.UtcNow;

            await _verificationTokenService.MarkAsUsedAsync(verificationToken);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static string ResolveRedirectPath(List<string> roles)
        {
            if (roles.Contains("Admin"))
            {
                return "/admin";
            }

            if (roles.Contains("Host"))
            {
                return "/host";
            }

            return "/";
        }

        public async Task<MeResponseDto?> UpdateCurrentUserAsync(
    ClaimsPrincipal principal,
    UpdateCurrentUserRequestDto request)
        {
            var userIdClaim = principal.FindFirstValue(JwtClaimNames.Sub)
                              ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailAlreadyUsed = await _dbContext.Users
                .AnyAsync(u => u.Email == normalizedEmail && u.Id != userId);

            if (emailAlreadyUsed)
            {
                throw new InvalidOperationException("EMAIL_ALREADY_REGISTERED");
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.Enabled)
            {
                return null;
            }

            user.Name = request.Name.Trim();
            user.Kana = request.Kana.Trim();
            user.PostalCode = request.PostalCode.Trim();
            user.Address = request.Address.Trim();
            user.PhoneNumber = request.PhoneNumber.Trim();
            user.Email = normalizedEmail;

            await _dbContext.SaveChangesAsync();

            var roles = user.UserRoles
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToList();

            return new MeResponseDto
            {
                IsAuthenticated = true,
                User = new CurrentUserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Kana = user.Kana,
                    Email = user.Email,
                    PostalCode = user.PostalCode,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    UsageType = user.UsageType,
                    Roles = roles
                }
            };
        }
    }
}