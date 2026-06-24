// 文件大小转换器
// 将字节数（long）转换为人类可读的文件大小字符串
using System.Globalization;
using System.Windows.Data;

namespace XiuXiu.Converters;

/// <summary>
/// 将 long 类型的字节数转换为易读的文件大小格式
/// 例如：1024 → "1 KB"，1048576 → "1 MB"
/// </summary>
public class FileSizeConverter : IValueConverter
{
    // 文件大小单位数组，从字节到 PB
    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB", "PB" };

    /// <summary>
    /// 将字节数转换为人类可读的大小字符串
    /// </summary>
    /// <param name="value">long 类型的字节数</param>
    /// <returns>格式化后的文件大小字符串</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = 0;

        if (value is long l)
            bytes = l;
        else if (value is int i)
            bytes = i;
        else if (value is double d)
            bytes = (long)d;
        else
            return "0 B";

        // 0 字节直接返回
        if (bytes == 0)
            return "0 B";

        // 计算合适的单位级别
        // 使用 1024 进制进行换算
        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024 && unitIndex < SizeUnits.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        // 格式化输出：保留一位小数，如果小数部分为 0 则不显示
        if (size >= 100 || unitIndex == 0)
            return $"{size:F0} {SizeUnits[unitIndex]}";
        else if (size >= 10)
            return $"{size:F1} {SizeUnits[unitIndex]}";
        else
            return $"{size:F2} {SizeUnits[unitIndex]}";
    }

    /// <summary>
    /// 反向转换不支持
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FileSizeConverter 不支持反向转换");
    }
}
