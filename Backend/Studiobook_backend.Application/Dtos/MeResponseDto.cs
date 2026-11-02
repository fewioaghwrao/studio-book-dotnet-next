namespace Studiobook_backend.Application.Dtos
{
    public class MeResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public CurrentUserDto? User { get; set; }
    }
}
