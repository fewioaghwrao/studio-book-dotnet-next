using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Tests.Settings;

public class FrontendSettingsTests
{
    [Theory]
    [InlineData("https://front.example.com", "/signup/verified", "https://front.example.com/signup/verified")]
    [InlineData("https://front.example.com/", "/signup/verified", "https://front.example.com/signup/verified")]
    [InlineData(" https://front.example.com// ", "signup/verified", "https://front.example.com/signup/verified")]
    [InlineData("http://localhost:3000", "/reset-password?token=abc", "http://localhost:3000/reset-password?token=abc")]
    public void BuildUrl_ベースURLとパスを結合する(string baseUrl, string path, string expected)
    {
        var settings = new FrontendSettings { BaseUrl = baseUrl };

        Assert.Equal(expected, settings.BuildUrl(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("front.example.com")]
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://front.example.com")]
    public void 不正なベースURLは無効と判定し_BuildUrlは例外を投げる(string baseUrl)
    {
        var settings = new FrontendSettings { BaseUrl = baseUrl };

        Assert.False(settings.HasValidBaseUrl());
        Assert.Throws<InvalidOperationException>(() => settings.BuildUrl("/signup/verified"));
    }

    [Fact]
    public void http_https_の絶対URLは有効と判定する()
    {
        Assert.True(new FrontendSettings { BaseUrl = "https://front.example.com" }.HasValidBaseUrl());
        Assert.True(new FrontendSettings { BaseUrl = "http://localhost:3000" }.HasValidBaseUrl());
    }
}
