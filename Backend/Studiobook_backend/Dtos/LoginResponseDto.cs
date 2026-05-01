namespace Studiobook_backend.Dtos
{
    public class LoginResponseDto
    {
        public CurrentUserDto User { get; set; } = new();
        public string RedirectTo { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
