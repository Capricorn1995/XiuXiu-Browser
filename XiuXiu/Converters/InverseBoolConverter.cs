// 布尔值反转转换器
// 将布尔值取反：true → false，false → true
using System.Globalization;
using System.Windows.Data;

namespace XiuXiu.Converters;

/// <summary>
/// 将布尔值取反的简单转换器
/// 常用于将 IsReadOnly 转换为 IsEditable 等场景
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>
    /// 反转布尔值
    /// </summary>
    /// <param name="value">源布尔值</param>
    /// <returns>取反后的布尔值</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        // 非布尔值默认返回 true
        return true;
    }

    /// <summary>
    /// 反向转换：再次取反（对称操作）
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return true;
    }
}
