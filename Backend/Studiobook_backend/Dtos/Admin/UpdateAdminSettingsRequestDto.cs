using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos.Admin
{
    public class UpdateAdminSettingsRequestDto
    {
        [Range(0, 100)]
        public decimal TaxRatePercent { get; set; }

        [Range(0, 100)]
        public decimal AdminFeeRatePercent { get; set; }
    }
}