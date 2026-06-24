// 媒体提取服务实现
// 同时使用 JavaScript 注入和 AngleSharp HTML 解析两种方式提取媒体资源
// 两种方式结果合并后去重，确保最大程度地覆盖各类资源
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Web.WebView2.Core;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 媒体提取服务
/// 优先使用 JavaScript 注入方式（可获得动态加载的资源和准确尺寸）
/// 同时使用 AngleSharp 解析 HTML 作为补充（覆盖 JS 无法访问的场景）
/// 两种方式结果合并去重后返回
/// </summary>
public class MediaExtractionService : IMediaExtractionService
{
    // JavaScript 提取器脚本（从嵌入资源加载）
    private static string? _cachedScript;

    /// <summary>
    /// 从 WebView2 核心中提取媒体资源
    /// 步骤：
    /// 1. 注入 JavaScript 嗅探脚本获取动态资源
    /// 2. 获取页面 HTML 源码
    /// 3. 用 AngleSharp 解析 HTML 获取静态资源
    /// 4. 合并两个结果并去重
    /// </summary>
    public async Task<List<MediaItem>> ExtractMediaAsync(CoreWebView2 webView)
    {
        var allItems = new List<MediaItem>();

        try
        {
            // 方式一：JavaScript 注入提取
            string script = await GetMediaExtractorScriptAsync();
            string? jsonResult = await webView.ExecuteScriptAsync(script);

            if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null")
            {
                // JSON 结果可能被引号包裹，需要去除
                string cleanJson = JsonSerializer.Deserialize<string>(jsonResult) ?? jsonResult;

                try
                {
                    var jsItems = JsonSerializer.Deserialize<List<JsMediaResult>>(cleanJson);
                    if (jsItems != null)
                    {
                        string pageUrl = webView.Source;
                        foreach (var item in jsItems)
                        {
                            var mediaType = item.type == "video" ? MediaType.Video : MediaType.Image;
                            allItems.Add(new MediaItem
                            {
                                Url = item.url ?? "",
                                ThumbnailUrl = mediaType == MediaType.Image ? (item.url ?? "") : "",
                                Type = mediaType,
                                SourceElement = item.sourceElement ?? "",
                                SourcePageUrl = pageUrl,
                                Width = item.width,
                                Height = item.height
                            });
                        }
                    }
                }
                catch (JsonException)
                {
                    // JS 返回的不是标准 JSON 数组，跳过此方式
                }
            }

            // 方式二：AngleSharp HTML 解析（作为补充）
            string html = await webView.ExecuteScriptAsync("document.documentElement.outerHTML");
            if (!string.IsNullOrEmpty(html))
            {
                // 移除 JSON 引号包裹
                string cleanHtml = JsonSerializer.Deserialize<string>(html) ?? html;
                var htmlItems = ExtractFromHtml(cleanHtml, webView.Source);
                allItems.AddRange(htmlItems);
            }

            // 合并去重
            return DeduplicateAndNormalize(allItems);
        }
        catch (Exception)
        {
            // 提取失败时返回已获取的结果
            return DeduplicateAndNormalize(allItems);
        }
    }

    /// <summary>
    /// 从 HTML 源代码中提取媒体资源
    /// 使用 AngleSharp 解析以下元素：
    /// - img 标签（含 data-src, data-original, srcset 等懒加载属性）
    /// - video 和 source 标签
    /// - CSS background-image（内联样式）
    /// - OpenGraph meta 标签（og:image）
    /// - Twitter Card meta 标签（twitter:image）
    /// - link[rel="image_src"]
    /// </summary>
    public List<MediaItem> ExtractFromHtml(string html, string baseUrl)
    {
        var items = new List<MediaItem>();

        if (string.IsNullOrWhiteSpace(html))
            return items;

        try
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

            // 提取 img 标签（含各种懒加载属性）
            ExtractImgElements(document, items, baseUrl);

            // 提取 video/source 标签
            ExtractVideoElements(document, items, baseUrl);

            // 提取 CSS 背景图片（内联样式）
            ExtractBackgroundImages(document, items, baseUrl);

            // 提取 OpenGraph 图片
            ExtractMetaImages(document, items, baseUrl, "meta[property='og:image']", "content", "og:image");

            // 提取 Twitter Card 图片
            ExtractMetaImages(document, items, baseUrl, "meta[name='twitter:image']", "content", "twitter:image");
            ExtractMetaImages(document, items, baseUrl, "meta[name='twitter:image:src']", "content", "twitter:image:src");

            // 提取 link[rel="image_src"]
            ExtractLinkImages(document, items, baseUrl);
        }
        catch (Exception)
        {
            // 解析失败，返回空列表
        }

        return items;
    }

    // ===== 私有提取方法 =====

    /// <summary>
    /// 提取 img 标签及其变体的图片 URL
    /// 支持 src, data-src, data-original, data-lazy-src, srcset 属性
    /// </summary>
    private static void ExtractImgElements(IDocument document, List<MediaItem> items, string baseUrl)
    {
        var imgs = document.QuerySelectorAll("img");
        foreach (var img in imgs)
        {
            // 尝试多个可能的图片源属性
            string? src = img.GetAttribute("src")
                ?? img.GetAttribute("data-src")
                ?? img.GetAttribute("data-original")
                ?? img.GetAttribute("data-lazy-src");

            if (!string.IsNullOrWhiteSpace(src))
            {
                string absoluteUrl = NormalizeUrl(src, baseUrl);
                if (IsValidMediaUrl(absoluteUrl))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Image, "img[src]", baseUrl));
                }
            }

            // 解析 srcset 属性中的最大尺寸图片
            string? srcset = img.GetAttribute("srcset");
            if (!string.IsNullOrWhiteSpace(srcset))
            {
                string? largestSrc = ParseLargestSrcset(srcset);
                if (!string.IsNullOrWhiteSpace(largestSrc))
                {
                    string absoluteUrl = NormalizeUrl(largestSrc, baseUrl);
                    if (IsValidMediaUrl(absoluteUrl))
                    {
                        items.Add(CreateMediaItem(absoluteUrl, MediaType.Image, "img[srcset]", baseUrl));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 提取 video 标签和 source 子标签的视频 URL
    /// </summary>
    private static void ExtractVideoElements(IDocument document, List<MediaItem> items, string baseUrl)
    {
        // 提取 video 标签直接设置的 src
        var videos = document.QuerySelectorAll("video[src]");
        foreach (var video in videos)
        {
            string? src = video.GetAttribute("src");
            if (!string.IsNullOrWhiteSpace(src))
            {
                string absoluteUrl = NormalizeUrl(src, baseUrl);
                if (!absoluteUrl.StartsWith("data:") && !absoluteUrl.StartsWith("blob:"))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Video, "video[src]", baseUrl));
                }
            }
        }

        // 提取 video > source 子标签
        var sources = document.QuerySelectorAll("video source[src]");
        foreach (var source in sources)
        {
            string? src = source.GetAttribute("src");
            if (!string.IsNullOrWhiteSpace(src))
            {
                string absoluteUrl = NormalizeUrl(src, baseUrl);
                if (!absoluteUrl.StartsWith("data:") && !absoluteUrl.StartsWith("blob:"))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Video, "video>source", baseUrl));
                }
            }
        }
    }

    /// <summary>
    /// 提取内联样式中的 CSS background-image
    /// </summary>
    private static void ExtractBackgroundImages(IDocument document, List<MediaItem> items, string baseUrl)
    {
        var elementsWithStyle = document.QuerySelectorAll("[style]");
        foreach (var el in elementsWithStyle)
        {
            string? style = el.GetAttribute("style");
            if (string.IsNullOrWhiteSpace(style))
                continue;

            var matches = System.Text.RegularExpressions.Regex.Matches(
                style, @"url\(['""]?([^'"")]+)['""]?\)");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string bgUrl = match.Groups[1].Value;
                string absoluteUrl = NormalizeUrl(bgUrl, baseUrl);
                if (IsValidMediaUrl(absoluteUrl))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Image, "style[background-image]", baseUrl));
                }
            }
        }
    }

    /// <summary>
    /// 提取 meta 标签中的图片 URL（OpenGraph, Twitter Card 等）
    /// </summary>
    private static void ExtractMetaImages(IDocument document, List<MediaItem> items, string baseUrl,
        string selector, string attributeName, string sourceElement)
    {
        var metas = document.QuerySelectorAll(selector);
        foreach (var meta in metas)
        {
            string? content = meta.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(content))
            {
                string absoluteUrl = NormalizeUrl(content, baseUrl);
                if (IsValidMediaUrl(absoluteUrl))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Image, sourceElement, baseUrl));
                }
            }
        }
    }

    /// <summary>
    /// 提取 link[rel="image_src"] 标签
    /// </summary>
    private static void ExtractLinkImages(IDocument document, List<MediaItem> items, string baseUrl)
    {
        var links = document.QuerySelectorAll("link[rel='image_src']");
        foreach (var link in links)
        {
            string? href = link.GetAttribute("href");
            if (!string.IsNullOrWhiteSpace(href))
            {
                string absoluteUrl = NormalizeUrl(href, baseUrl);
                if (IsValidMediaUrl(absoluteUrl))
                {
                    items.Add(CreateMediaItem(absoluteUrl, MediaType.Image, "link[image_src]", baseUrl));
                }
            }
        }
    }

    // ===== URL 处理工具方法 =====

    /// <summary>
    /// 规范化 URL：处理协议相对路径（//）、相对路径和绝对路径
    /// </summary>
    private static string NormalizeUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // 处理协议相对 URL（例如 //example.com/image.jpg）
        if (url.StartsWith("//"))
        {
            return "https:" + url;
        }

        // 已经是绝对 URL
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
            return url;

        // 相对路径，以 baseUrl 为基础解析
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri) &&
            Uri.TryCreate(baseUri, url, out Uri? resolved))
        {
            return resolved.AbsoluteUri;
        }

        return url;
    }

    /// <summary>
    /// 判断 URL 是否为有效的媒体资源
    /// 过滤 data: URI、SVG 文件，确保是已知的图片或视频格式
    /// </summary>
    private static bool IsValidMediaUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // 过滤 data: URI
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        // 过滤 SVG 文件（通常是图标，不是用户想要的资源）
        if (url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return false;

        // 已知的图片扩展名
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico", ".tiff", ".tif" };
        // 已知的视频扩展名
        var videoExtensions = new[] { ".mp4", ".webm", ".m3u8", ".flv", ".mov", ".avi", ".mkv", ".wmv", ".ogg", ".m3u", ".ts" };

        // 提取 URL 路径（去掉查询参数和片段）
        string path = url;
        try
        {
            var uri = new Uri(url);
            path = uri.AbsolutePath;
        }
        catch { }

        // 检查是否有已知的媒体扩展名
        foreach (var ext in imageExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        foreach (var ext in videoExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 对于来自已知 CDN 的 URL（无扩展名），也认为是有效的
        var knownCdnDomains = new[]
        {
            "douyinvod.com", "douyincdn.com", "douyin.com",
            "pstatp.com", "ixigua.com", "bytedance.com",
            "alicdn.com", "cloudfront.net", "fastly.net",
            "qpic.cn", "sinaimg.cn", "imgur.com", "picsum.photos",
            "wp.com", "gravatar.com", "twimg.com"
        };

        foreach (var domain in knownCdnDomains)
        {
            if (url.Contains(domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 过滤 blob: URL（WebView2 本地 blob 无法下载）
        if (url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            return false;

        // 默认拒绝没有已知扩展名且非 CDN 的 URL
        return false;
    }

    /// <summary>
    /// 从 srcset 属性中提取最大尺寸的图片 URL
    /// srcset 格式："image-480.jpg 480w, image-800.jpg 800w"
    /// </summary>
    private static string? ParseLargestSrcset(string srcset)
    {
        if (string.IsNullOrWhiteSpace(srcset))
            return null;

        var parts = srcset.Split(',');
        string? bestUrl = null;
        int bestSize = 0;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var segments = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                string urlPart = segments[0];
                string sizeStr = segments[^1].TrimEnd('w', 'W');

                if (int.TryParse(sizeStr, out int size) && size > bestSize)
                {
                    bestSize = size;
                    bestUrl = urlPart;
                }
            }
            else if (segments.Length == 1 && bestUrl == null)
            {
                // 没有描述符时使用第一个
                bestUrl = segments[0];
            }
        }

        return bestUrl;
    }

    /// <summary>
    /// 去重并规范化媒体资源列表
    /// 按 URL 去重，保留最先出现的条目
    /// </summary>
    private static List<MediaItem> DeduplicateAndNormalize(List<MediaItem> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<MediaItem>();

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Url) && seen.Add(item.Url))
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// 创建媒体资源项（快捷方法）
    /// </summary>
    private static MediaItem CreateMediaItem(string url, MediaType type, string sourceElement, string baseUrl)
    {
        return new MediaItem
        {
            Url = url,
            ThumbnailUrl = type == MediaType.Image ? url : "",
            Type = type,
            SourceElement = sourceElement,
            SourcePageUrl = baseUrl
        };
    }

    // ===== JavaScript 脚本加载 =====

    /// <summary>
    /// 获取媒体提取 JavaScript 脚本内容
    /// 首次加载后缓存，避免重复读取嵌入资源
    /// </summary>
    private static async Task<string> GetMediaExtractorScriptAsync()
    {
        if (_cachedScript != null)
            return _cachedScript;

        // 从嵌入资源读取 JavaScript 脚本
        var assembly = typeof(MediaExtractionService).Assembly;
        string resourceName = "XiuXiu.Scripts.MediaExtractor.js";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // 如果找不到嵌入资源，返回内联的简化版脚本
            _cachedScript = GenerateFallbackScript();
            return _cachedScript;
        }

        using var reader = new StreamReader(stream);
        _cachedScript = await reader.ReadToEndAsync();
        return _cachedScript;
    }

    /// <summary>
    /// 生成备用提取脚本（当嵌入资源加载失败时使用）
    /// </summary>
    private static string GenerateFallbackScript()
    {
        return @"
(function() {
    var results = [];
    var baseUrl = window.location.href;

    function resolveUrl(url) {
        try {
            var a = document.createElement('a');
            a.href = url;
            return a.href;
        } catch(e) { return url; }
    }

    function addItem(url, type, source, w, h) {
        results.push({url: resolveUrl(url), type: type, sourceElement: source, width: w || 0, height: h || 0});
    }

    // 收集所有 img 标签
    document.querySelectorAll('img').forEach(function(img) {
        var src = img.src || img.getAttribute('data-src') || img.getAttribute('data-original') || img.getAttribute('data-lazy-src');
        if (src && !src.startsWith('data:')) {
            addItem(src, 'image', 'img[src]', img.naturalWidth, img.naturalHeight);
        }
    });

    // 收集 video 标签（包括动态创建的，使用 currentSrc）
    document.querySelectorAll('video').forEach(function(video) {
        var src = video.currentSrc || video.src || video.getAttribute('src');
        if (src && !src.startsWith('blob:') && !src.startsWith('data:')) {
            addItem(src, 'video', 'video[currentSrc]', video.videoWidth || 0, video.videoHeight || 0);
        }
        var poster = video.getAttribute('poster');
        if (poster && !poster.startsWith('data:')) {
            addItem(poster, 'image', 'video[poster]');
        }
        video.querySelectorAll('source').forEach(function(source) {
            var sourceSrc = source.getAttribute('src') || source.src;
            if (sourceSrc && !sourceSrc.startsWith('blob:') && !sourceSrc.startsWith('data:')) {
                addItem(sourceSrc, 'video', 'video>source');
            }
        });
    });

    // 收集 iframe 源
    document.querySelectorAll('iframe').forEach(function(iframe) {
        var src = iframe.getAttribute('src');
        if (src && src.indexOf('http') === 0) {
            addItem(src, 'video', 'iframe[src]');
        }
        try {
            if (iframe.contentDocument) {
                iframe.contentDocument.querySelectorAll('video').forEach(function(v) {
                    var vs = v.currentSrc || v.src || v.getAttribute('src');
                    if (vs && !vs.startsWith('blob:') && !vs.startsWith('data:')) {
                        addItem(vs, 'video', 'iframe>video');
                    }
                });
            }
        } catch(e) {}
    });

    // 检查 window.__INITIAL_STATE__ 等全局数据
    ['__INITIAL_STATE__', '__NUXT__', '__NEXT_DATA__'].forEach(function(key) {
        var data = window[key];
        if (data && typeof data === 'object') {
            try {
                var str = JSON.stringify(data);
                var urlMatches = str.match(/https?:\/\/[^\s""'<>(){}[\]]+\.(mp4|webm|m3u8|flv|mov|avi|mkv|jpg|jpeg|png|gif|webp)/gi);
                if (urlMatches) {
                    urlMatches.forEach(function(u) {
                        var t = /\.(mp4|webm|m3u8|flv|mov|avi|mkv)$/i.test(u) ? 'video' : 'image';
                        addItem(u, t, 'global[' + key + ']');
                    });
                }
            } catch(e) {}
        }
    });

    // 第十三步：扫描页面源码中的视频URL
    var videoUrlPattern = /(https?:\/\/[^\s""'<>]+\.(?:mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)[^\s""'<>]*)/gi;
    var m3u8Pattern = /(https?:\/\/[^\s""'<>]+\.m3u8[^\s""'<>]*)/gi;

    // 扫描 script 标签
    document.querySelectorAll('script').forEach(function(script) {
        var content = script.textContent || script.innerHTML || '';
        if (!content) return;
        var matches = content.match(videoUrlPattern);
        if (matches) {
            matches.forEach(function(url) {
                if (/\.(mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)/i.test(url)) {
                    addItem(url, 'video', 'script[content]');
                }
            });
        }
        var m3u8Matches = content.match(m3u8Pattern);
        if (m3u8Matches) {
            m3u8Matches.forEach(function(url) { addItem(url, 'video', 'script[m3u8]'); });
        }
    });

    // 扫描页面 HTML 中的 m3u8
    var htmlM3u8 = document.documentElement.outerHTML.match(m3u8Pattern);
    if (htmlM3u8) {
        htmlM3u8.forEach(function(url) { addItem(url, 'video', 'html[m3u8]'); });
    }

    // 检查 video data 属性
    ['data-video-url', 'data-mp4', 'data-src', 'data-url', 'data-video', 'data-source'].forEach(function(attr) {
        document.querySelectorAll('[' + attr + ']').forEach(function(el) {
            var val = el.getAttribute(attr);
            if (val && !val.startsWith('data:') && !val.startsWith('blob:') && !val.startsWith('#') && /\.(mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)/i.test(val)) {
                addItem(val, 'video', '[' + attr + ']');
            }
        });
    });

    return JSON.stringify(results);
})();
";
    }

    // ===== JS 结果反序列化辅助类 =====

    /// <summary>
    /// JavaScript 提取结果的结构
    /// </summary>
    private class JsMediaResult
    {
        public string? url { get; set; }
        public string? type { get; set; }
        public string? sourceElement { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
    }
}
