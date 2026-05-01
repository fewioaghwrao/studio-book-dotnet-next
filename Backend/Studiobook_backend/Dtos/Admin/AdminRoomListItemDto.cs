namespace Studiobook_backend.Dtos.Admin
{
    public class AdminRoomListItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int HostUserId { get; set; }

        public string HostName { get; set; } = string.Empty;
    }
}