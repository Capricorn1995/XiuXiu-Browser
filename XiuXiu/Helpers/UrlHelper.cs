// URL 辅助工具类
// 提供 URL 验证、规范化、搜索 URL 构建和域名提取等功能
namespace XiuXiu.Helpers;

/// <summary>
/// URL 处理静态辅助类
/// 提供 URL 验证、格式化、搜索 URL 构建和资源类型判断等功能
/// </summary>
public static class UrlHelper
{
    // 搜索引擎配置映射表
    // 存储各搜索引擎的名称和对应的搜索 URL 模板（{0} 为查询关键词占位符）
    private static readonly Dictionary<string, string> SearchEngines = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Baidu", "https://www.baidu.com/s?wd={0}" },
        { "Google", "https://www.google.com/search?q={0}" },
        { "Bing", "https://www.bing.com/search?q={0}" },
        { "Sogou", "https://www.sogou.com/web?query={0}" }
    };

    // 已知顶级域名后缀，用于判断 URL 是否有效
    private static readonly HashSet<string> KnownTlds = new(StringComparer.OrdinalIgnoreCase)
    {
        ".com", ".cn", ".org", ".net", ".edu", ".gov", ".io",
        ".co", ".uk", ".de", ".jp", ".kr", ".fr", ".ru", ".info",
        ".biz", ".tv", ".cc", ".me", ".top", ".xyz", ".app", ".dev"
    };

    /// <summary>
    /// 验证字符串是否为有效的 URL
    /// 检查格式是否合法，支持 http/https 协议
    /// </summary>
    /// <param name="url">待验证的 URL 字符串</param>
    /// <returns>如果格式有效返回 true，否则返回 false</returns>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // 尝试用 Uri 类解析，必须为绝对 URI
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// 规范化用户输入，自动补全协议或将搜索词转为搜索 URL
    /// 处理逻辑：
    /// 1. 已包含协议的有效 URL → 直接返回
    /// 2. 类似 "example.com" 的输入 → 添加 https:// 前缀
    /// 3. 纯搜索关键词 → 使用默认搜索引擎构建搜索 URL
    /// </summary>
    /// <param name="input">用户输入文本</param>
    /// <param name="defaultEngine">默认搜索引擎名称（默认 Baidu）</param>
    /// <returns>规范化后的完整 URL</returns>
    public static string NormalizeUrl(string input, string defaultEngine = "Baidu")
    {
        if (string.IsNullOrWhiteSpace(input))
            return "about:blank";

        input = input.Trim();

        // 情况1：已是有效 URL
        if (IsValidUrl(input))
            return input;

        // 情况2：以 "www." 开头或包含常见 TLD，补全协议
        if (input.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
            HasKnownTld(input))
        {
            return $"https://{input}";
        }

        // 情况3：可能是搜索关键词，使用搜索引擎
        return BuildSearchUrl(input, defaultEngine);
    }

    /// <summary>
    /// 使用指定搜索引擎构建搜索 URL
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="engineName">搜索引擎名称</param>
    /// <returns>完整的搜索 URL</returns>
    public static string BuildSearchUrl(string query, string engineName)
    {
        if (SearchEngines.TryGetValue(engineName, out string? template))
        {
            // URL 编码搜索关键词并填充到模板中
            return string.Format(template, Uri.EscapeDataString(query));
        }

        // 默认使用百度搜索
        return string.Format(SearchEngines["Baidu"], Uri.EscapeDataString(query));
    }

    /// <summary>
    /// 从 URL 中提取域名（主机名）
    /// </summary>
    /// <param name="url">完整 URL</param>
    /// <returns>域名部分，解析失败返回空字符串</returns>
    public static string ExtractDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return uri.Host;
        }

        return string.Empty;
    }

    /// <summary>
    /// 判断 URL 是否指向图片资源
    /// 通过文件扩展名判断
    /// </summary>
    /// <param name="url">待检查的 URL</param>
    /// <returns>如果是图片 URL 返回 true</returns>
    public static bool IsImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // 支持的图片文件扩展名
        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico", ".svg" };

        string extension = Path.GetExtension(url).ToLowerInvariant();

        // 去除查询参数后再检查扩展名
        int queryIndex = extension.IndexOf('?');
        if (queryIndex > 0)
            extension = extension[..queryIndex];

        return imageExtensions.Contains(extension);
    }

    /// <summary>
    /// 判断 URL 是否指向视频资源
    /// 通过文件扩展名或常见视频流格式判断
    /// </summary>
    /// <param name="url">待检查的 URL</param>
    /// <returns>如果是视频 URL 返回 true</returns>
    public static bool IsVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // 支持的视频文件扩展名和流格式
        string[] videoExtensions = { ".mp4", ".webm", ".m3u8", ".flv", ".mov", ".avi", ".mkv", ".wmv", ".ts" };

        string extension = Path.GetExtension(url).ToLowerInvariant();

        // 去除查询参数
        int queryIndex = extension.IndexOf('?');
        if (queryIndex > 0)
            extension = extension[..queryIndex];

        return videoExtensions.Contains(extension);
    }

    /// <summary>
    /// 检查输入字符串是否包含已知的顶级域名
    /// 用于判断用户输入是 URL 还是搜索关键词
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>包含已知 TLD 返回 true</returns>
    private static bool HasKnownTld(string input)
    {
        // 提取可能的主机名部分（去掉路径和协议）
        string hostPart = input;

        // 移除可能的协议前缀
        if (hostPart.Contains("://"))
        {
            hostPart = hostPart[(hostPart.IndexOf("://") + 3)..];
        }

        // 只取第一个斜杠之前的部分
        int slashIndex = hostPart.IndexOf('/');
        if (slashIndex > 0)
            hostPart = hostPart[..slashIndex];

        // 检查是否以已知 TLD 结尾
        return KnownTlds.Any(tld => hostPart.EndsWith(tld, StringComparison.OrdinalIgnoreCase))
               || hostPart.Contains('.');
    }
}
