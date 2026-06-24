// 主题服务实现
// 管理应用程序主题切换（浅色/深色/跟随系统）
// 通过替换 ResourceDictionary 实现运行时主题切换
// 通过 Windows 注册表检测系统主题
using System.Windows;

namespace XiuXiu.Services;

/// <summary>
/// 主题服务
/// 核心功能：
/// - 运行时替换 ResourceDictionary 切换主题
/// - 通过注册表检测 Windows 系统主题
/// - 支持 Light/Dark/System 三种模式
/// - 通过 ThemeChanged 事件通知 UI 更新
/// </summary>
public class ThemeService : IThemeService
{
    // 当前生效的主题名称
    private string _currentTheme = "Light";

    public event EventHandler<string>? ThemeChanged;

    /// <summary>
    /// 应用指定主题
    /// 处理流程：
    /// 1. 如果设置为 System，先检测系统主题
    /// 2. 查找并替换 Application.Resources 中的主题 ResourceDictionary
    /// 3. 触发 ThemeChanged 事件
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        string effectiveTheme = themeName;

        // 跟随系统时检测 Windows 主题设置
        if (themeName == "System")
        {
            effectiveTheme = DetectSystemTheme();
        }

        // 如果主题未变化，跳过
        if (_currentTheme == effectiveTheme)
            return;

        _currentTheme = effectiveTheme;

        // 获取应用程序资源字典
        var appResources = Application.Current.Resources;
        var mergedDicts = appResources.MergedDictionaries;

        // 查找现有主题字典
        ResourceDictionary? lightThemeDict = null;
        ResourceDictionary? darkThemeDict = null;

        foreach (var dict in mergedDicts)
        {
            if (dict.Source?.OriginalString.Contains("LightTheme.xaml") == true)
                lightThemeDict = dict;
            else if (dict.Source?.OriginalString.Contains("DarkTheme.xaml") == true)
                darkThemeDict = dict;
        }

        // 应用对应主题
        if (effectiveTheme == "Dark")
        {
            ApplyDarkTheme(mergedDicts, lightThemeDict, darkThemeDict);
        }
        else
        {
            ApplyLightTheme(mergedDicts, lightThemeDict, darkThemeDict);
        }

        // 通知主题变更
        ThemeChanged?.Invoke(this, effectiveTheme);
    }

    /// <summary>
    /// 获取当前生效的主题名称
    /// </summary>
    public string GetCurrentTheme()
    {
        return _currentTheme;
    }

    /// <summary>
    /// 设置主题（ApplyTheme 的别名）
    /// </summary>
    public void SetTheme(string themeName)
    {
        ApplyTheme(themeName);
    }

    /// <summary>
    /// 应用当前保存的主题（无参数版本，从设置中读取）
    /// </summary>
    public void ApplyTheme()
    {
        // 由 App.xaml.cs 在启动时调用，主题已通过 ApplyTheme(themeName) 设置
        // 如果需要从 ISettingsService 读取，可以通过依赖注入获取
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 应用浅色主题
    /// </summary>
    private static void ApplyLightTheme(
        System.Collections.ObjectModel.Collection<ResourceDictionary> mergedDicts,
        ResourceDictionary? lightThemeDict,
        ResourceDictionary? darkThemeDict)
    {
        // 移除深色主题
        if (darkThemeDict != null)
        {
            mergedDicts.Remove(darkThemeDict);
        }

        // 添加或替换浅色主题
        if (lightThemeDict != null)
        {
            int index = mergedDicts.IndexOf(lightThemeDict);
            mergedDicts.Remove(lightThemeDict);
            mergedDicts.Insert(index, new ResourceDictionary
            {
                Source = new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative)
            });
        }
        else
        {
            mergedDicts.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative)
            });
        }
    }

    /// <summary>
    /// 应用深色主题
    /// </summary>
    private static void ApplyDarkTheme(
        System.Collections.ObjectModel.Collection<ResourceDictionary> mergedDicts,
        ResourceDictionary? lightThemeDict,
        ResourceDictionary? darkThemeDict)
    {
        // 移除浅色主题
        if (lightThemeDict != null)
        {
            mergedDicts.Remove(lightThemeDict);
        }

        // 添加或替换深色主题
        if (darkThemeDict != null)
        {
            int index = mergedDicts.IndexOf(darkThemeDict);
            mergedDicts.Remove(darkThemeDict);
            mergedDicts.Insert(index, new ResourceDictionary
            {
                Source = new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
            });
        }
        else
        {
            mergedDicts.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
            });
        }
    }

    /// <summary>
    /// 检测 Windows 系统主题
    /// 通过读取注册表项判断系统使用浅色还是深色模式
    /// Windows 10 1809+ / Windows 11 支持
    /// </summary>
    private static string DetectSystemTheme()
    {
        try
        {
            // Windows 主题注册表路径
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath);
            if (key?.GetValue(valueName) is int value)
            {
                // 0 = 深色主题, 1 = 浅色主题
                return value == 0 ? "Dark" : "Light";
            }
        }
        catch
        {
            // 读取注册表失败时默认使用浅色主题
        }

        return "Light";
    }
}
