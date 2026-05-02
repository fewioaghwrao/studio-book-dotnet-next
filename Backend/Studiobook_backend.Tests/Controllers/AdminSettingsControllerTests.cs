using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminSettingsControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Get_正常時_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AdminSettingsService(context);
        var controller = new AdminSettingsController(
            service,
            auditLogService: null!);

        // Act
        var result = await controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminSettingsDto>(okResult.Value);

        Assert.Equal(10.00m, body.TaxRatePercent);
        Assert.Equal(15.00m, body.AdminFeeRatePercent);
    }

    [Fact]
    public async Task Update_ModelStateが不正な場合_BadRequestを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AdminSettingsService(context);
        var controller = new AdminSettingsController(
            service,
            auditLogService: null!);

        controller.ModelState.AddModelError("TaxRatePercent", "税率は必須です。");

        var request = new UpdateAdminSettingsRequestDto
        {
            TaxRatePercent = 0,
            AdminFeeRatePercent = 15
        };

        // Act
        var result = await controller.Update(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
