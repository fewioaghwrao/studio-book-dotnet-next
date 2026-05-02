using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos;
using Studiobook_backend.Services;
using Studiobook_backend.Services.Interfaces;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController CreateController(
        IAuthService? authService = null,
        IJwtTokenService? jwtTokenService = null,
        AppDbContext? dbContext = null,
        PasswordResetService? passwordResetService = null,
        AuditLogService? auditLogService = null)
    {
        var authMock = authService ?? Mock.Of<IAuthService>();
        var jwtMock = jwtTokenService ?? Mock.Of<IJwtTokenService>();
        var db = dbContext ?? TestDbContextFactory.Create();

        // ここは実装状況によって調整が必要
        // PasswordResetService / AuditLogService が具象クラス依存なので、
        // 可能なら interface 化するとテストしやすい
        var passwordReset = passwordResetService ?? null!;
        var auditLog = auditLogService ?? null!;

        var controller = new AuthController(
            authMock,
            jwtMock,
            db,
            passwordReset,
            auditLog);

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
}