namespace Studiobook_backend.Dtos
{
    public class MeResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public CurrentUserDto? User { get; set; }
    }
}
