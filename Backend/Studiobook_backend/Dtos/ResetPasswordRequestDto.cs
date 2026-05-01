using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "トークンは必須です。")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "新しいパスワードは必須です。")]
        [MinLength(8, ErrorMessage = "パスワードは8文字以上で入力してください。")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "確認用パスワードは必須です。")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}