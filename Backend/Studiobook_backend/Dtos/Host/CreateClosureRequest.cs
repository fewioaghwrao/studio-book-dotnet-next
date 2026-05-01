using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos.Host
{

    public class CreateClosureRequest
    {
        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }
    }
}
