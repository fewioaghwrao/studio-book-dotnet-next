namespace Studiobook_backend.Application.Dtos
{
    /// <summary>
    /// ログイン成功時に発行したアクセストークンと、監査ログ用のユーザー情報。
    /// </summary>
    public class AuthTokenResultDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;
    }
}
