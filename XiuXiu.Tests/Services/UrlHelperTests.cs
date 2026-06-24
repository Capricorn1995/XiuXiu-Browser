// UrlHelper 单元测试
// 测试 URL 验证、规范化、搜索 URL 构建、域名提取和资源类型判断
using FluentAssertions;
using Xunit;
using XiuXiu.Helpers;

namespace XiuXiu.Tests.Services;

/// <summary>
/// UrlHelper 静态辅助类单元测试
/// 覆盖 IsValidUrl、NormalizeUrl、BuildSearchUrl、ExtractDomain、IsImageUrl、IsVideoUrl
/// </summary>
public class UrlHelperTests
{
    // ===== IsValidUrl 测试 =====

    [Theory(DisplayName = "IsValidUrl - 有效 URL 返回 true")]
    [InlineData("https://www.example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path/to/page?query=1#hash")]
    [InlineData("https://sub.domain.example.com")]
    public void IsValidUrl_WithValidUrls_ReturnsTrue(string url)
    {
        var result = UrlHelper.IsValidUrl(url);
        result.Should().BeTrue();
    }

    [Theory(DisplayName = "IsValidUrl - 无效 URL 返回 false")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/path/to/file")]
    public void IsValidUrl_WithInvalidUrls_ReturnsFalse(string url)
    {
        var result = UrlHelper.IsValidUrl(url);
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsValidUrl - null 输入返回 false")]
    public void IsValidUrl_WithNull_ReturnsFalse()
    {
        var result = UrlHelper.IsValidUrl(null!);
        result.Should().BeFalse();
    }

    // ===== NormalizeUrl 测试 =====

    [Theory(DisplayName = "NormalizeUrl - 已有协议的 URL 直接返回")]
    [InlineData("https://www.example.com", "https://www.example.com")]
    [InlineData("http://example.com/page", "http://example.com/page")]
    public void NormalizeUrl_WithValidUrl_ReturnsAsIs(string input, string expected)
    {
        var result = UrlHelper.NormalizeUrl(input);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "NormalizeUrl - www 开头的输入补全 https://")]
    [InlineData("www.example.com", "https://www.example.com")]
    [InlineData("www.google.com/search", "https://www.google.com/search")]
    public void NormalizeUrl_WithWwwPrefix_AddsHttps(string input, string expected)
    {
        var result = UrlHelper.NormalizeUrl(input);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "NormalizeUrl - 包含已知 TLD 的输入补全协议")]
    [InlineData("example.com", "https://example.com")]
    [InlineData("mysite.cn/page", "https://mysite.cn/page")]
    public void NormalizeUrl_WithKnownTld_AddsHttps(string input, string expected)
    {
        var result = UrlHelper.NormalizeUrl(input);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "NormalizeUrl - 搜索关键词转为搜索 URL")]
    [InlineData("你好世界")]
    [InlineData("C# WPF 教程")]
    [InlineData("test search query")]
    public void NormalizeUrl_WithSearchQuery_ReturnsSearchUrl(string query)
    {
        var result = UrlHelper.NormalizeUrl(query);
        result.Should().StartWith("https://www.baidu.com/s?wd=");
        result.Should().Contain(Uri.EscapeDataString(query));
    }

    [Fact(DisplayName = "NormalizeUrl - 空输入返回 about:blank")]
    public void NormalizeUrl_WithEmptyInput_ReturnsAboutBlank()
    {
        var result = UrlHelper.NormalizeUrl("");
        result.Should().Be("about:blank");
    }

    [Fact(DisplayName = "NormalizeUrl - 空白输入返回 about:blank")]
    public void NormalizeUrl_WithWhitespace_ReturnsAboutBlank()
    {
        var result = UrlHelper.NormalizeUrl("   ");
        result.Should().Be("about:blank");
    }

    // ===== BuildSearchUrl 测试 =====

    [Fact(DisplayName = "BuildSearchUrl - 构建百度搜索 URL")]
    public void BuildSearchUrl_WithBaidu_ReturnsBaiduSearchUrl()
    {
        var result = UrlHelper.BuildSearchUrl("测试", "Baidu");
        result.Should().Be("https://www.baidu.com/s?wd=" + Uri.EscapeDataString("测试"));
    }

    [Fact(DisplayName = "BuildSearchUrl - 构建 Google 搜索 URL")]
    public void BuildSearchUrl_WithGoogle_ReturnsGoogleSearchUrl()
    {
        var result = UrlHelper.BuildSearchUrl("hello world", "Google");
        result.Should().Be("https://www.google.com/search?q=" + Uri.EscapeDataString("hello world"));
    }

    [Fact(DisplayName = "BuildSearchUrl - 构建 Bing 搜索 URL")]
    public void BuildSearchUrl_WithBing_ReturnsBingSearchUrl()
    {
        var result = UrlHelper.BuildSearchUrl("search", "Bing");
        result.Should().Be("https://www.bing.com/search?q=" + Uri.EscapeDataString("search"));
    }

    [Fact(DisplayName = "BuildSearchUrl - 未知搜索引擎回退到百度")]
    public void BuildSearchUrl_WithUnknownEngine_FallsBackToBaidu()
    {
        var result = UrlHelper.BuildSearchUrl("测试", "UnknownEngine");
        result.Should().Be("https://www.baidu.com/s?wd=" + Uri.EscapeDataString("测试"));
    }

    [Fact(DisplayName = "BuildSearchUrl - 特殊字符被正确编码")]
    public void BuildSearchUrl_WithSpecialCharacters_EncodesCorrectly()
    {
        var result = UrlHelper.BuildSearchUrl("C# & .NET", "Google");
        result.Should().Contain(Uri.EscapeDataString("C# & .NET"));
        result.Should().NotContain(" "); // 空格应被编码
    }

    // ===== ExtractDomain 测试 =====

    [Theory(DisplayName = "ExtractDomain - 从各种 URL 提取域名")]
    [InlineData("https://www.example.com/page", "www.example.com")]
    [InlineData("http://example.com", "example.com")]
    [InlineData("https://sub.domain.example.com/path?query=1", "sub.domain.example.com")]
    [InlineData("https://example.com:8080/page", "example.com")]
    public void ExtractDomain_WithValidUrl_ReturnsHost(string url, string expectedDomain)
    {
        var result = UrlHelper.ExtractDomain(url);
        result.Should().Be(expectedDomain);
    }

    [Theory(DisplayName = "ExtractDomain - 无效 URL 返回空字符串")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    public void ExtractDomain_WithInvalidUrl_ReturnsEmptyString(string url)
    {
        var result = UrlHelper.ExtractDomain(url);
        result.Should().BeEmpty();
    }

    // ===== IsImageUrl 测试 =====

    [Theory(DisplayName = "IsImageUrl - 图片扩展名返回 true")]
    [InlineData("https://example.com/image.jpg", true)]
    [InlineData("https://example.com/photo.png", true)]
    [InlineData("https://example.com/anim.gif", true)]
    [InlineData("https://example.com/img.webp", true)]
    [InlineData("https://example.com/pic.bmp", true)]
    [InlineData("https://example.com/icon.ico", true)]
    public void IsImageUrl_WithImageExtension_ReturnsTrue(string url, bool expected)
    {
        var result = UrlHelper.IsImageUrl(url);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "IsImageUrl - 非图片扩展名返回 false")]
    [InlineData("https://example.com/video.mp4", false)]
    [InlineData("https://example.com/page.html", false)]
    [InlineData("https://example.com/file.pdf", false)]
    public void IsImageUrl_WithNonImageExtension_ReturnsFalse(string url, bool expected)
    {
        var result = UrlHelper.IsImageUrl(url);
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "IsImageUrl - 带查询参数的图片 URL 正确判断")]
    public void IsImageUrl_WithQueryParameters_ReturnsTrue()
    {
        var result = UrlHelper.IsImageUrl("https://example.com/image.jpg?width=800&height=600");
        result.Should().BeTrue();
    }

    // ===== IsVideoUrl 测试 =====

    [Theory(DisplayName = "IsVideoUrl - 视频扩展名返回 true")]
    [InlineData("https://example.com/video.mp4", true)]
    [InlineData("https://example.com/clip.webm", true)]
    [InlineData("https://example.com/stream.m3u8", true)]
    [InlineData("https://example.com/movie.flv", true)]
    [InlineData("https://example.com/recording.mov", true)]
    [InlineData("https://example.com/film.avi", true)]
    public void IsVideoUrl_WithVideoExtension_ReturnsTrue(string url, bool expected)
    {
        var result = UrlHelper.IsVideoUrl(url);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "IsVideoUrl - 非视频扩展名返回 false")]
    [InlineData("https://example.com/image.jpg", false)]
    [InlineData("https://example.com/page.html", false)]
    [InlineData("https://example.com/audio.mp3", false)]
    public void IsVideoUrl_WithNonVideoExtension_ReturnsFalse(string url, bool expected)
    {
        var result = UrlHelper.IsVideoUrl(url);
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "IsVideoUrl - 带查询参数的视频 URL 正确判断")]
    public void IsVideoUrl_WithQueryParameters_ReturnsTrue()
    {
        var result = UrlHelper.IsVideoUrl("https://example.com/video.mp4?token=abc123");
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsImageUrl - 空字符串返回 false")]
    public void IsImageUrl_WithEmptyString_ReturnsFalse()
    {
        var result = UrlHelper.IsImageUrl("");
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsVideoUrl - 空字符串返回 false")]
    public void IsVideoUrl_WithEmptyString_ReturnsFalse()
    {
        var result = UrlHelper.IsVideoUrl("");
        result.Should().BeFalse();
    }
}
