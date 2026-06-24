// 主题到颜色转换器
// 将主题名称字符串（"Light"/"Dark"）转换为对应的 Color 或 SolidColorBrush
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace XiuXiu.Converters;

/// <summary>
/// 将主题名称转换为对应的颜色或画刷资源
/// 支持转换为 Color 或 SolidColorBrush 类型
/// </summary>
public class ThemeToColorConverter : IValueConverter
{
    // 主题颜色映射表
    // 根据主题名称返回对应的背景颜色
    private static readonly Dictionary<string, Color> ThemeColors = new()
    {
        { "Light", Color.FromRgb(0xF3, 0xF3, 0xF3) },
        { "Dark", Color.FromRgb(0x1E, 0x1E, 0x1E) },
        { "System", Color.FromRgb(0xF3, 0xF3, 0xF3) } // 默认使用浅色
    };

    /// <summary>
    /// 将主题名称转换为颜色或画刷
    /// </summary>
    /// <param name="value">主题名称（"Light"、"Dark"、"System"）</param>
    /// <param name="targetType">目标类型：Color 或 SolidColorBrush</param>
    /// <returns>对应的 Color 或 SolidColorBrush 对象</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? themeName = value as string ?? "Light";

        // 获取主题对应的颜色
        if (!ThemeColors.TryGetValue(themeName, out Color color))
        {
            color = ThemeColors["Light"];
        }

        // 根据目标类型返回 Color 或 SolidColorBrush
        if (targetType == typeof(Color))
        {
            return color;
        }
        else if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
        {
            return new SolidColorBrush(color);
        }

        // 默认返回 SolidColorBrush
        return new SolidColorBrush(color);
    }

    /// <summary>
    /// 反向转换：将颜色转换回主题名称
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            Color color = brush.Color;

            // 根据颜色匹配主题
            foreach (var kvp in ThemeColors)
            {
                if (kvp.Value == color)
                    return kvp.Key;
            }
        }
        else if (value is Color color)
        {
            foreach (var kvp in ThemeColors)
            {
                if (kvp.Value == color)
                    return kvp.Key;
            }
        }

        return "Light";
    }
}
