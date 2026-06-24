// MediaExtractionService 单元测试
// 测试从 HTML 中提取媒体资源、URL 规范化和去重功能
using FluentAssertions;
using Xunit;
using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.Tests.Services;

/// <summary>
/// MediaExtractionService 单元测试
/// 覆盖 ExtractFromHtml 的各种 HTML 元素提取场景
/// 以及 NormalizeUrl、IsValidMediaUrl、DeduplicateAndNormalize 方法
/// </summary>
public class MediaExtractionServiceTests
{
    private readonly MediaExtractionService _service;

    public MediaExtractionServiceTests()
    {
        _service = new MediaExtractionService();
    }

    // ===== ExtractFromHtml 测试 =====

    [Fact(DisplayName = "ExtractFromHtml - 提取基本 img 标签")]
    public void ExtractFromHtml_WithBasicImgTags_ReturnsMediaItems()
    {
        // 准备：包含基本 img 标签的 HTML
        var html = @"<html><body>
            <img src=""https://example.com/image1.jpg"" />
            <img src=""https://example.com/image2.png"" />
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(item =>
        {
            item.Type.Should().Be(MediaType.Image);
            item.SourcePageUrl.Should().Be("https://example.com");
        });
        result.Select(r => r.Url).Should().Contain("https://example.com/image1.jpg");
        result.Select(r => r.Url).Should().Contain("https://example.com/image2.png");
    }

    [Fact(DisplayName = "ExtractFromHtml - 提取 data-src 懒加载属性")]
    public void ExtractFromHtml_WithDataSrcLazyLoading_ReturnsMediaItems()
    {
        // 准备：包含 data-src 懒加载属性的 HTML
        var html = @"<html><body>
            <img data-src=""https://example.com/lazy1.jpg"" />
            <img data-src=""https://example.com/lazy2.jpg"" src=""placeholder.png"" />
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：data-src 优先于空 src，应提取到两个资源
        result.Should().HaveCount(2);
        result.Select(r => r.Url).Should().Contain("https://example.com/lazy1.jpg");
        result.Select(r => r.Url).Should().Contain("https://example.com/lazy2.jpg");
    }

    [Fact(DisplayName = "ExtractFromHtml - 提取 video source 标签")]
    public void ExtractFromHtml_WithVideoSourceTags_ReturnsVideoMediaItems()
    {
        // 准备：包含 video 和 source 标签的 HTML
        var html = @"<html><body>
            <video src=""https://example.com/video1.mp4""></video>
            <video>
                <source src=""https://example.com/video2.webm"" />
            </video>
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(item => item.Type.Should().Be(MediaType.Video));
        result.Select(r => r.Url).Should().Contain("https://example.com/video1.mp4");
        result.Select(r => r.Url).Should().Contain("https://example.com/video2.webm");
    }

    [Fact(DisplayName = "ExtractFromHtml - 提取 CSS background-image 内联样式")]
    public void ExtractFromHtml_WithCssBackgroundImage_ReturnsMediaItems()
    {
        // 准备：包含 background-image 内联样式的 HTML
        var html = @"<html><body>
            <div style=""background-image: url('https://example.com/bg1.jpg')""></div>
            <div style=""background-image: url(https://example.com/bg2.png)""></div>
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证
        result.Should().HaveCount(2);
        result.Select(r => r.Url).Should().Contain("https://example.com/bg1.jpg");
        result.Select(r => r.Url).Should().Contain("https://example.com/bg2.png");
        result.Should().AllSatisfy(item =>
        {
            item.SourceElement.Should().Be("style[background-image]");
            item.Type.Should().Be(MediaType.Image);
        });
    }

    [Fact(DisplayName = "ExtractFromHtml - 提取 og:image 元标签")]
    public void ExtractFromHtml_WithOgImageMetaTag_ReturnsMediaItem()
    {
        // 准备：包含 og:image meta 标签的 HTML
        var html = @"<html><head>
            <meta property=""og:image"" content=""https://example.com/og-image.jpg"" />
            <meta name=""twitter:image"" content=""https://example.com/twitter-image.jpg"" />
        </head><body></body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：og:image + twitter:image = 2 个资源
        result.Should().HaveCount(2);
        result.Select(r => r.Url).Should().Contain("https://example.com/og-image.jpg");
        result.Select(r => r.Url).Should().Contain("https://example.com/twitter-image.jpg");
    }

    [Fact(DisplayName = "ExtractFromHtml - 空 HTML 返回空列表")]
    public void ExtractFromHtml_WithEmptyHtml_ReturnsEmptyList()
    {
        // 执行
        var result = _service.ExtractFromHtml("", "https://example.com");

        // 验证
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "ExtractFromHtml - 无效 HTML 返回空列表（不抛出异常）")]
    public void ExtractFromHtml_WithMalformedHtml_ReturnsEmptyList()
    {
        // 准备：严重畸形的 HTML
        var html = "<><<<<>>>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：不抛出异常，返回空列表
        result.Should().BeEmpty();
    }

    // ===== NormalizeUrl 测试 =====

    [Fact(DisplayName = "NormalizeUrl - 处理协议相对 URL (//cdn)")]
    public void NormalizeUrl_WithProtocolRelativeUrl_AddsHttps()
    {
        // 准备：从基础 URL 提取，通过 ExtractFromHtml 间接测试
        var html = @"<img src=""//cdn.example.com/image.jpg"" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://mysite.com");

        // 验证：协议相对 URL 应补全为 https:
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://cdn.example.com/image.jpg");
    }

    [Fact(DisplayName = "NormalizeUrl - 处理相对路径 URL")]
    public void NormalizeUrl_WithRelativeUrl_ResolvesAgainstBaseUrl()
    {
        // 准备：相对路径
        var html = @"<img src=""/images/photo.jpg"" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://mysite.com/page");

        // 验证：相对路径应解析为绝对 URL
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://mysite.com/images/photo.jpg");
    }

    [Fact(DisplayName = "NormalizeUrl - 处理绝对 URL（保持不变）")]
    public void NormalizeUrl_WithAbsoluteUrl_ReturnsAsIs()
    {
        // 准备：已经是绝对 URL
        var html = @"<img src=""https://othersite.com/image.jpg"" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://mysite.com");

        // 验证：绝对 URL 保持不变
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://othersite.com/image.jpg");
    }

    // ===== IsValidMediaUrl 测试 =====

    [Fact(DisplayName = "IsValidMediaUrl - 过滤 data: URI")]
    public void IsValidMediaUrl_WithDataUri_ExcludesFromResults()
    {
        // 准备：包含 data: URI 的 img
        var html = @"<img src=""data:image/png;base64,iVBORw0KGgo=="" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：data: URI 应被过滤
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "IsValidMediaUrl - 过滤 SVG 文件")]
    public void IsValidMediaUrl_WithSvgFile_ExcludesFromResults()
    {
        // 准备：包含 .svg 的 img
        var html = @"<img src=""https://example.com/icon.svg"" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：SVG 文件应被过滤
        result.Should().BeEmpty();
    }

    // ===== DeduplicateAndNormalize 测试 =====

    [Fact(DisplayName = "DeduplicateAndNormalize - 去除重复 URL")]
    public void DeduplicateAndNormalize_WithDuplicateUrls_RemovesDuplicates()
    {
        // 准备：包含重复 URL 的 HTML
        var html = @"<html><body>
            <img src=""https://example.com/duplicate.jpg"" />
            <img src=""https://example.com/duplicate.jpg"" />
            <img src=""https://example.com/unique.jpg"" />
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：重复的 URL 只保留一个
        result.Should().HaveCount(2);
        result.Select(r => r.Url).Should().Contain("https://example.com/duplicate.jpg");
        result.Select(r => r.Url).Should().Contain("https://example.com/unique.jpg");
    }

    [Fact(DisplayName = "DeduplicateAndNormalize - 大小写不同的重复 URL 视为重复")]
    public void DeduplicateAndNormalize_WithCaseDifferentDuplicates_RemovesDuplicates()
    {
        // 准备：大小写不同的相同 URL
        var html = @"<html><body>
            <img src=""https://example.com/Image.JPG"" />
            <img src=""https://example.com/image.jpg"" />
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：大小写不同的相同 URL 应去重
        result.Should().HaveCount(1);
    }

    [Fact(DisplayName = "ExtractFromHtml - 支持 data-original 属性（懒加载变体）")]
    public void ExtractFromHtml_WithDataOriginalAttribute_ReturnsMediaItem()
    {
        // 准备：使用 data-original 的图片
        var html = @"<img data-original=""https://example.com/lazy-load.jpg"" />";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证
        result.Should().HaveCount(1);
        result[0].Url.Should().Be("https://example.com/lazy-load.jpg");
    }

    [Fact(DisplayName = "ExtractFromHtml - 同时使用多种提取方式不会丢失资源")]
    public void ExtractFromHtml_WithMultipleExtractionMethods_ReturnsAllUniqueItems()
    {
        // 准备：同时包含 img、background-image 和 meta 的 HTML
        var html = @"<html><head>
            <meta property=""og:image"" content=""https://example.com/og.jpg"" />
        </head><body>
            <img src=""https://example.com/img.jpg"" />
            <div style=""background-image: url('https://example.com/bg.jpg')""></div>
        </body></html>";

        // 执行
        var result = _service.ExtractFromHtml(html, "https://example.com");

        // 验证：三种方式都能提取到
        result.Should().HaveCount(3);
        result.Select(r => r.Url).Should().Contain(
            new[] { "https://example.com/og.jpg", "https://example.com/img.jpg", "https://example.com/bg.jpg" });
    }
}
