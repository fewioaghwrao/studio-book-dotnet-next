using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "メールアドレスは必須です。")]
        [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません。")]
        public string Email { get; set; } = string.Empty;
    }
}