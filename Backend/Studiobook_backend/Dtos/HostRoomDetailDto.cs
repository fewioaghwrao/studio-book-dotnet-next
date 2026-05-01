namespace Studiobook_backend.Dtos
{
    public class HostRoomDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}