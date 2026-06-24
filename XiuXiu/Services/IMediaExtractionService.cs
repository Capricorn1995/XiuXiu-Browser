// 媒体提取服务接口
// 负责从网页中嗅探图片和视频资源
using Microsoft.Web.WebView2.Core;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 媒体提取服务接口
/// 支持通过 JavaScript 注入和 HTML 解析两种方式提取媒体资源
/// </summary>
public interface IMediaExtractionService
{
    /// <summary>从 WebView2 核心中提取媒体资源（JavaScript 注入方式）</summary>
    /// <param name="webView">CoreWebView2 实例</param>
    /// <returns>提取到的媒体资源列表</returns>
    Task<List<MediaItem>> ExtractMediaAsync(CoreWebView2 webView);

    /// <summary>从 HTML 源代码中提取媒体资源（AngleSharp 解析方式）</summary>
    /// <param name="html">HTML 源代码</param>
    /// <param name="baseUrl">页面基础 URL（用于解析相对路径）</param>
    /// <returns>提取到的媒体资源列表</returns>
    List<MediaItem> ExtractFromHtml(string html, string baseUrl);
}
