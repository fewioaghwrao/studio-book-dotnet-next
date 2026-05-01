namespace Studiobook_backend.Dtos
{
    public class HostRoomListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}