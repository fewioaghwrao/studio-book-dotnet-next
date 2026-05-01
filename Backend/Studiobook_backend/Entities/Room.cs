namespace Studiobook_backend.Entities
{
    public class Room
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int Price { get; set; }
        public int Capacity { get; set; }

        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public ICollection<Closure> Closures { get; set; } = new List<Closure>();

        public ICollection<BusinessHour> BusinessHours { get; set; } = new List<BusinessHour>();

        public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}