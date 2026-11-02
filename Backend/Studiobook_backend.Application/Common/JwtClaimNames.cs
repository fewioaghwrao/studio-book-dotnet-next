namespace Studiobook_backend.Application.Common;

/// <summary>
/// JWT のクレーム名定数。
/// Application 層を System.IdentityModel.Tokens.Jwt に依存させないために定義しています。
/// （値は JwtRegisteredClaimNames と同一です）
/// </summary>
public static class JwtClaimNames
{
    public const string Sub = "sub";
}
