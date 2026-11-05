using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Studiobook_backend.Controllers;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Application.Dtos;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Infrastructure.Services;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController CreateController(
        IAuthService? authService = null,
        PasswordResetService? passwordResetService = null,
        AuditLogService? auditLogService = null,
        string frontendBaseUrl = "https://front.example.com")
    {
        var authMock = authService ?? Mock.Of<IAuthService>();

        // ここは実装状況によって調整が必要
        // PasswordResetService / AuditLogService が具象クラス依存なので、
        // 可能なら interface 化するとテストしやすい
        var passwordReset = passwordResetService ?? null!;
        var auditLog = auditLogService ?? null!;

        var controller = new AuthController(
            authMock,
            passwordReset,
            auditLog,
            Options.Create(new FrontendSettings { BaseUrl = frontendBaseUrl }));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    [Fact]
    public async Task Login_認証失敗の場合_Unauthorizedを返す()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthService>();

        authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((LoginResponseDto?)null);

        var controller = CreateController(authService: authServiceMock.Object);

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrong-password"
        };

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponseDto>(unauthorized.Value);

        Assert.Equal("AUTH_INVALID_CREDENTIALS", body.Code);
        Assert.Equal("メールアドレスまたはパスワードが正しくありません。", body.Message);
    }

    [Fact]
    public async Task UpdateMe_ModelStateが不正な場合_BadRequestを返す()
    {
        // Arrange
        var controller = CreateController();

        controller.ModelState.AddModelError("Email", "メールアドレスは必須です。");

        var request = new UpdateCurrentUserRequestDto
        {
            Name = "",
            Kana = "",
            Email = "",
            PostalCode = "",
            Address = "",
            PhoneNumber = ""
        };

        // Act
        var result = await controller.UpdateMe(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponseDto>(badRequest.Value);

        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("入力内容を確認してください。", body.Message);
    }
    [Fact]
    public async Task ResetPassword_パスワード不一致の場合_BadRequestを返す()
    {
        // Arrange
        var controller = CreateController();

        var request = new ResetPasswordRequestDto
        {
            Token = "dummy-token",
            NewPassword = "Password123!",
            ConfirmPassword = "Different123!"
        };

        // Act
        var result = await controller.ResetPassword(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponseDto>(badRequest.Value);

        Assert.Equal("PASSWORD_MISMATCH", body.Code);
        Assert.Equal("パスワードが一致しません。", body.Message);
    }
    [Fact]
    public async Task Signup_ModelStateが不正な場合_BadRequestを返す()
    {
        // Arrange
        var controller = CreateController();

        controller.ModelState.AddModelError("Email", "メールアドレスは必須です。");

        var request = new SignupRequestDto
        {
            Name = "",
            Kana = "",
            Email = "",
            Password = "",
            PasswordConfirmation = "",
            PostalCode = "",
            Address = "",
            PhoneNumber = "",
            UsageType = ""
        };

        // Act
        var result = await controller.Signup(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponseDto>(badRequest.Value);

        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("入力内容を確認してください。", body.Message);
    }
    [Fact]
    public async Task ForgotPassword_ModelStateが不正な場合_BadRequestを返す()
    {
        // Arrange
        var controller = CreateController();

        controller.ModelState.AddModelError("Email", "メールアドレスは必須です。");

        var request = new ForgotPasswordRequestDto
        {
            Email = ""
        };

        // Act
        var result = await controller.ForgotPassword(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponseDto>(badRequest.Value);

        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("入力内容を確認してください。", body.Message);
    }

    [Fact]
    public async Task Verify_トークン未指定の場合_設定したフロントエンドのエラー画面へリダイレクトする()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthService>();
        var controller = CreateController(authService: authServiceMock.Object);

        // Act
        var result = await controller.Verify("");

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(
            "https://front.example.com/signup/verify-error?reason=token-required",
            redirect.Url);

        authServiceMock.Verify(x => x.VerifySignupAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Verify_無効なトークンの場合_設定したフロントエンドのエラー画面へリダイレクトする()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthService>();

        authServiceMock
            .Setup(x => x.VerifySignupAsync("invalid-token"))
            .ReturnsAsync(false);

        var controller = CreateController(authService: authServiceMock.Object);

        // Act
        var result = await controller.Verify("invalid-token");

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(
            "https://front.example.com/signup/verify-error?reason=invalid-token",
            redirect.Url);
    }

    [Fact]
    public async Task Verify_認証成功の場合_設定したフロントエンドの完了画面へリダイレクトする()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthService>();

        authServiceMock
            .Setup(x => x.VerifySignupAsync("valid-token"))
            .ReturnsAsync(true);

        // 末尾の / があっても二重スラッシュにならないこと
        var controller = CreateController(
            authService: authServiceMock.Object,
            frontendBaseUrl: "https://front.example.com/");

        // Act
        var result = await controller.Verify("valid-token");

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://front.example.com/signup/verified", redirect.Url);
        Assert.DoesNotContain("localhost", redirect.Url);
    }

    [Fact]
    public void Logout_成功を返し_Cookieを操作しない()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Logout();

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Bearer 方式に統一したため、Set-Cookie（Cookie の削除を含む）を返さない
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
    }
}
