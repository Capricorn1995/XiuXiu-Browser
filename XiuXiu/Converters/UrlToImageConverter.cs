// URL 到图片转换器
// 将 URL 字符串异步加载为 BitmapImage，用于媒体缩略图显示
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace XiuXiu.Converters;

/// <summary>
/// 将 URL 字符串转换为 BitmapImage
/// 支持 HTTP/HTTPS URL 的异步图片加载，加载失败时返回 null
/// </summary>
public class UrlToImageConverter : IValueConverter
{
    /// <summary>
    /// 将 URL 字符串转换为 BitmapImage
    /// </summary>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.DecodePixelWidth = 104; // 2x for HiDPI
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze(); // 允许跨线程访问
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 不支持反向转换
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
