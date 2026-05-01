namespace Studiobook_backend.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Kana { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UsageType { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;
        public DateTime? EmailVerifiedAtUtc { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        public ICollection<Room> Rooms { get; set; } = new List<Room>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}