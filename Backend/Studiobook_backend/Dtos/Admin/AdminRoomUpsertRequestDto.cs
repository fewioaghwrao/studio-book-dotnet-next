using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos.Admin
{
    public class AdminRoomUpsertRequestDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string ImageName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 1_000_000)]
        public int Price { get; set; }

        [Range(1, 10_000)]
        public int Capacity { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;
    }
}