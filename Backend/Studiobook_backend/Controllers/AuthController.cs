using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos;
using Studiobook_backend.Services;
using Studiobook_backend.Services.Interfaces;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string AuthCookieName = "auth_token";
        private readonly AuditLogService _auditLogService;

        private readonly IAuthService _authService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _dbContext;
        private readonly PasswordResetService _passwordResetService;

        public AuthController(
            IAuthService authService,
            IJwtTokenService jwtTokenService,
            AppDbContext dbContext,
            PasswordResetService passwordResetService,
            AuditLogService auditLogService)
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
            _dbContext = dbContext;
            _passwordResetService = passwordResetService;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "VALIDATION_ERROR",
                    Message = "入力内容を確認してください。"
                });
            }

            try
            {
                var loginResult = await _authService.LoginAsync(request);
                if (loginResult == null)
                {
                    return Unauthorized(new ApiErrorResponseDto
                    {
                        Code = "AUTH_INVALID_CREDENTIALS",
                        Message = "メールアドレスまたはパスワードが正しくありません。"
                    });
                }

                var user = await _dbContext.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstAsync(u => u.Id == loginResult.User.Id);

                var roles = user.UserRoles
                    .Select(x => x.Role.Name)
                    .Distinct()
                    .ToList();

                var token = _jwtTokenService.GenerateToken(user, roles, out var expiresAtUtc);

                Response.Cookies.Append(AuthCookieName, token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = expiresAtUtc,
                    Path = "/"
                });

                loginResult.ExpiresAt = expiresAtUtc;

                await _auditLogService.WriteAsync(
    actorId: user.Id,
    action: "LOGIN",
    entity: "User",
    entityId: user.Id,
    note: $"ユーザー「{user.Name}」がログインしました。"
);

                return Ok(loginResult);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message == "USER_DISABLED")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponseDto
                {
                    Code = "AUTH_USER_DISABLED",
                    Message = "このアカウントは現在利用できません。"
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(MeResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me()
        {
            var result = await _authService.GetCurrentUserAsync(User);
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(MeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateCurrentUserRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "VALIDATION_ERROR",
                    Message = "入力内容を確認してください。"
                });
            }

            try
            {
                var result = await _authService.UpdateCurrentUserAsync(User, request);

                if (result == null)
                {
                    return Unauthorized(new ApiErrorResponseDto
                    {
                        Code = "AUTH_REQUIRED",
                        Message = "ログインが必要です。"
                    });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_REGISTERED")
            {
                return Conflict(new ApiErrorResponseDto
                {
                    Code = "EMAIL_ALREADY_REGISTERED",
                    Message = "すでに登録済みのメールアドレスです。"
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AuthCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SignupResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Signup([FromBody] SignupRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "VALIDATION_ERROR",
                    Message = "入力内容を確認してください。"
                });
            }

            try
            {
                var requestBaseUrl = $"{Request.Scheme}://{Request.Host}";
                await _authService.SignupAsync(request, requestBaseUrl);

                return Ok(new SignupResponseDto
                {
                    Success = true,
                    Message = "認証メールを送信しました。メール内のリンクから登録を完了してください。"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_REGISTERED")
            {
                return Conflict(new ApiErrorResponseDto
                {
                    Code = "EMAIL_ALREADY_REGISTERED",
                    Message = "すでに登録済みのメールアドレスです。"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "PASSWORD_MISMATCH")
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "PASSWORD_MISMATCH",
                    Message = "パスワードが一致しません。"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ROLE_NOT_FOUND")
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponseDto
                {
                    Code = "ROLE_NOT_FOUND",
                    Message = "初期ロールが設定されていません。"
                });
            }
        }

        [HttpGet("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify([FromQuery] string token)
        {
            const string frontendBaseUrl = "http://localhost:3000";

            if (string.IsNullOrWhiteSpace(token))
            {
                return Redirect($"{frontendBaseUrl}/signup/verify-error?reason=token-required");
            }

            var success = await _authService.VerifySignupAsync(token);

            if (!success)
            {
                return Redirect($"{frontendBaseUrl}/signup/verify-error?reason=invalid-token");
            }

            return Redirect($"{frontendBaseUrl}/signup/verified");
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "VALIDATION_ERROR",
                    Message = "入力内容を確認してください。"
                });
            }

            await _passwordResetService.RequestResetAsync(request.Email);

            return Ok(new MessageResponseDto
            {
                Success = true,
                Message = "登録されているメールアドレスの場合、再設定メールを送信しました。"
            });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "VALIDATION_ERROR",
                    Message = "入力内容を確認してください。"
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "PASSWORD_MISMATCH",
                    Message = "パスワードが一致しません。"
                });
            }

            var result = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);

            if (!result.IsSuccess && result.ErrorCode == "INVALID_TOKEN")
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "INVALID_TOKEN",
                    Message = "トークンが無効、または有効期限切れです。"
                });
            }

            if (!result.IsSuccess && result.ErrorCode == "USER_DISABLED")
            {
                return BadRequest(new ApiErrorResponseDto
                {
                    Code = "USER_DISABLED",
                    Message = "このアカウントは現在利用できません。"
                });
            }

            return Ok(new MessageResponseDto
            {
                Success = true,
                Message = "パスワードを再設定しました。"
            });
        }
    }
}