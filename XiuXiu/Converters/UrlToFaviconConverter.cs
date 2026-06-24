// URL 到 Favicon 转换器
// 从完整 URL 中提取域名，生成 favicon.ico 地址
using System.Globalization;
using System.Windows.Data;

namespace XiuXiu.Converters;

/// <summary>
/// 将 URL 字符串转换为其对应的 favicon 图标地址
/// 提取域名后拼接 /favicon.ico 路径
/// </summary>
public class UrlToFaviconConverter : IValueConverter
{
    /// <summary>
    /// 从 URL 提取域名并构建 favicon 地址
    /// </summary>
    /// <param name="value">完整 URL 字符串</param>
    /// <returns>favicon 图标 URL，格式为 https://{domain}/favicon.ico</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return string.Empty;

        try
        {
            // 尝试解析 URL 获取域名
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                string domain = uri.Host;
                // 构建 favicon 地址，使用 https 协议
                return $"https://{domain}/favicon.ico";
            }
        }
        catch
        {
            // 解析失败时返回空字符串
        }

        return string.Empty;
    }

    /// <summary>
    /// 反向转换不支持，返回空字符串
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Empty;
    }
}
