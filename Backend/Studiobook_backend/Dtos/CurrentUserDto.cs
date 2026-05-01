namespace Studiobook_backend.Dtos
{
    public class CurrentUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kana { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UsageType { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
