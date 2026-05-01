using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class AdminSettingsService
    {
        private const string TaxRateKey = "tax_rate";
        private const string AdminFeeRateKey = "admin_fee_rate";

        private readonly AppDbContext _context;

        public AdminSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminSettingsDto> GetAsync()
        {
            var taxRate = await GetDecimalValueAsync(TaxRateKey, "0.10");
            var adminFeeRate = await GetDecimalValueAsync(AdminFeeRateKey, "0.15");

            return new AdminSettingsDto
            {
                TaxRatePercent = Math.Round(taxRate * 100m, 2, MidpointRounding.AwayFromZero),
                AdminFeeRatePercent = Math.Round(adminFeeRate * 100m, 2, MidpointRounding.AwayFromZero)
            };
        }

        public async Task<AdminSettingsDto> UpdateAsync(UpdateAdminSettingsRequestDto request)
        {
            var taxRateDecimal = request.TaxRatePercent / 100m;
            var adminFeeRateDecimal = request.AdminFeeRatePercent / 100m;

            await UpsertAsync(TaxRateKey, ToSettingValue(taxRateDecimal));
            await UpsertAsync(AdminFeeRateKey, ToSettingValue(adminFeeRateDecimal));

            await _context.SaveChangesAsync();

            return await GetAsync();
        }

        private async Task<decimal> GetDecimalValueAsync(string key, string defaultValue)
        {
            var setting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key);

            var value = setting?.Value ?? defaultValue;

            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                return result;
            }

            return decimal.Parse(defaultValue, CultureInfo.InvariantCulture);
        }

        private async Task UpsertAsync(string key, string value)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Key == key);

            if (setting == null)
            {
                _context.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAtUtc = DateTime.UtcNow
                });

                return;
            }

            setting.Value = value;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static string ToSettingValue(decimal value)
        {
            return value
                .ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}