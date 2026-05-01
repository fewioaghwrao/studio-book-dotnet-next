using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos
{
    public class SignupRequestDto
    {
        [Required(ErrorMessage = "氏名は必須です。")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "フリガナは必須です。")]
        [MaxLength(100)]
        public string Kana { get; set; } = string.Empty;

        [Required(ErrorMessage = "メールアドレスは必須です。")]
        [EmailAddress(ErrorMessage = "メールアドレス形式が不正です。")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "郵便番号は必須です。")]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "住所は必須です。")]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "電話番号は必須です。")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "利用区分は必須です。")]
        [MaxLength(50)]
        public string UsageType { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードは必須です。")]
        [MinLength(8, ErrorMessage = "パスワードは8文字以上で入力してください。")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "確認用パスワードは必須です。")]
        public string PasswordConfirmation { get; set; } = string.Empty;
    }
}