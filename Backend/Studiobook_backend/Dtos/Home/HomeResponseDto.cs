namespace Studiobook_backend.Dtos.Home
{
    public class HomeResponseDto
    {
        public List<HomeRoomDto> PopularRooms { get; set; } = new();

        public List<HomeRoomDto> NewRooms { get; set; } = new();
    }
}