using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminSettingsServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAsync_設定が存在しない場合_デフォルト値を返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AdminSettingsService(context);

        // Act
        var result = await service.GetAsync();

        // Assert
        Assert.Equal(10.00m, result.TaxRatePercent);
        Assert.Equal(15.00m, result.AdminFeeRatePercent);
    }

    [Fact]
    public async Task GetAsync_設定が存在する場合_パーセントに変換して返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AppSettings.AddRange(
            new AppSetting
            {
                Id = 1,
                Key = "tax_rate",
                Value = "0.08",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new AppSetting
            {
                Id = 2,
                Key = "admin_fee_rate",
                Value = "0.20",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminSettingsService(context);

        // Act
        var result = await service.GetAsync();

        // Assert
        Assert.Equal(8.00m, result.TaxRatePercent);
        Assert.Equal(20.00m, result.AdminFeeRatePercent);
    }

    [Fact]
    public async Task GetAsync_設定値が不正な文字列の場合_デフォルト値を返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AppSettings.AddRange(
            new AppSetting
            {
                Id = 1,
                Key = "tax_rate",
                Value = "invalid-tax",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new AppSetting
            {
                Id = 2,
                Key = "admin_fee_rate",
                Value = "invalid-fee",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminSettingsService(context);

        // Act
        var result = await service.GetAsync();

        // Assert
        Assert.Equal(10.00m, result.TaxRatePercent);
        Assert.Equal(15.00m, result.AdminFeeRatePercent);
    }

    [Fact]
    public async Task UpdateAsync_設定が存在しない場合_AppSettingsを追加する()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AdminSettingsService(context);

        var request = new UpdateAdminSettingsRequestDto
        {
            TaxRatePercent = 10m,
            AdminFeeRatePercent = 15m
        };

        // Act
        var result = await service.UpdateAsync(request);

        // Assert
        Assert.Equal(10.00m, result.TaxRatePercent);
        Assert.Equal(15.00m, result.AdminFeeRatePercent);

        var taxRate = await context.AppSettings
            .FirstOrDefaultAsync(x => x.Key == "tax_rate");

        var adminFeeRate = await context.AppSettings
            .FirstOrDefaultAsync(x => x.Key == "admin_fee_rate");

        Assert.NotNull(taxRate);
        Assert.NotNull(adminFeeRate);

        Assert.Equal("0.1", taxRate!.Value);
        Assert.Equal("0.15", adminFeeRate!.Value);
    }

    [Fact]
    public async Task UpdateAsync_設定が存在する場合_AppSettingsを更新する()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AppSettings.AddRange(
            new AppSetting
            {
                Id = 1,
                Key = "tax_rate",
                Value = "0.08",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new AppSetting
            {
                Id = 2,
                Key = "admin_fee_rate",
                Value = "0.12",
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminSettingsService(context);

        var request = new UpdateAdminSettingsRequestDto
        {
            TaxRatePercent = 12m,
            AdminFeeRatePercent = 18m
        };

        // Act
        var result = await service.UpdateAsync(request);

        // Assert
        Assert.Equal(12.00m, result.TaxRatePercent);
        Assert.Equal(18.00m, result.AdminFeeRatePercent);

        var taxRate = await context.AppSettings
            .FirstAsync(x => x.Key == "tax_rate");

        var adminFeeRate = await context.AppSettings
            .FirstAsync(x => x.Key == "admin_fee_rate");

        Assert.Equal("0.12", taxRate.Value);
        Assert.Equal("0.18", adminFeeRate.Value);
    }

    [Fact]
    public async Task UpdateAsync_小数を含むパーセントの場合_設定値として小数に変換して保存する()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AdminSettingsService(context);

        var request = new UpdateAdminSettingsRequestDto
        {
            TaxRatePercent = 8.5m,
            AdminFeeRatePercent = 12.75m
        };

        // Act
        var result = await service.UpdateAsync(request);

        // Assert
        Assert.Equal(8.50m, result.TaxRatePercent);
        Assert.Equal(12.75m, result.AdminFeeRatePercent);

        var taxRate = await context.AppSettings
            .FirstAsync(x => x.Key == "tax_rate");

        var adminFeeRate = await context.AppSettings
            .FirstAsync(x => x.Key == "admin_fee_rate");

        Assert.Equal("0.085", taxRate.Value);
        Assert.Equal("0.1275", adminFeeRate.Value);
    }
}