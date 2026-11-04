namespace Studiobook_backend.Application.Settings
{
    /// <summary>
    /// フロントエンド（Next.js）の公開URL設定。
    /// メール認証後のリダイレクト先や、パスワード再設定メールのリンク生成に使用します。
    /// 設定キー：Frontend:BaseUrl（環境変数では Frontend__BaseUrl）
    /// </summary>
    public class FrontendSettings
    {
        public const string SectionName = "Frontend";

        /// <summary>開発環境で未設定の場合に使う既定値</summary>
        public const string DevelopmentDefaultBaseUrl = "http://localhost:3000";

        /// <summary>例：https://studio-book.example.com（末尾の / は有無どちらでも可）</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// BaseUrl が http/https の絶対URLかどうか。
        /// </summary>
        public bool HasValidBaseUrl()
        {
            return Uri.TryCreate(BaseUrl?.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// フロントエンドのパスから絶対URLを組み立てる。
        /// 例：BuildUrl("/signup/verified") → https://studio-book.example.com/signup/verified
        /// </summary>
        public string BuildUrl(string pathAndQuery)
        {
            if (!HasValidBaseUrl())
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl が設定されていないか、URLの形式が正しくありません。");
            }

            var baseUrl = BaseUrl.Trim().TrimEnd('/');
            var path = pathAndQuery.StartsWith('/') ? pathAndQuery : "/" + pathAndQuery;

            return baseUrl + path;
        }
    }
}
