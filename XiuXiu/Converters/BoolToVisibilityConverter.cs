// 布尔值到可见性转换器
// 将 bool 值转换为 Visibility 枚举，true → Visible，false → Collapsed
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace XiuXiu.Converters;

/// <summary>
/// 将布尔值转换为 WPF Visibility 枚举
/// true 映射为 Visible，false 映射为 Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 将 bool 值转换为 Visibility
    /// </summary>
    /// <param name="value">源布尔值</param>
    /// <param name="targetType">目标类型（Visibility）</param>
    /// <param name="parameter">可选反转参数（传入 "Invert" 时反转逻辑）</param>
    /// <param name="culture">区域信息</param>
    /// <returns>Visible 或 Collapsed</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;

        // 如果传入 "Invert" 参数，反转布尔值逻辑
        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 将 Visibility 转回 bool 值
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;

            // 反转参数同样影响反向转换
            if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                result = !result;
            }

            return result;
        }

        return false;
    }
}
