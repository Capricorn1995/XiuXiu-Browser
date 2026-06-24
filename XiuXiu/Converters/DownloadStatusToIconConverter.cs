// 下载状态到图标转换器
// 将 DownloadStatus 枚举转换为对应的 Unicode 图标字符
using System.Globalization;
using System.Windows.Data;

using XiuXiu.Models;

namespace XiuXiu.Converters;

/// <summary>
/// 将 DownloadStatus 枚举值转换为对应的图标字符（Unicode 符号）
/// 用于在下载管理界面中直观显示下载状态
/// </summary>
public class DownloadStatusToIconConverter : IValueConverter
{
    /// <summary>
    /// 将下载状态转换为图标字符
    /// </summary>
    /// <param name="value">DownloadStatus 枚举值</param>
    /// <returns>对应状态的 Unicode 图标字符</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Pending => "⏳",     // 沙漏 - 等待中
                DownloadStatus.Downloading => "⬇",   // 向下箭头 - 下载中
                DownloadStatus.Paused => "⏸",       // 暂停符号 - 已暂停
                DownloadStatus.Completed => "✓",     // 对勾 - 已完成
                DownloadStatus.Failed => "✗",       // 叉号 - 失败
                DownloadStatus.Cancelled => "⊘",     // 禁止符号 - 已取消
                _ => "•"                            // 默认圆点
            };
        }

        return "•";
    }

    /// <summary>
    /// 反向转换不支持
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("DownloadStatusToIconConverter 不支持反向转换");
    }
}
