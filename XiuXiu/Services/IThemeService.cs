// 主题服务接口
// 管理应用程序主题的切换（浅色/深色/跟随系统）
namespace XiuXiu.Services;

/// <summary>
/// 主题服务接口
/// 支持三种主题模式：Light（浅色）、Dark（深色）、System（跟随系统）
/// 通过事件通知主题变更
/// </summary>
public interface IThemeService
{
    /// <summary>应用指定主题</summary>
    /// <param name="themeName">主题名称："Light"、"Dark" 或 "System"</param>
    void ApplyTheme(string themeName);

    /// <summary>设置主题（ApplyTheme 的别名，用于兼容旧代码）</summary>
    /// <param name="themeName">主题名称</param>
    void SetTheme(string themeName);

    /// <summary>获取当前生效的主题名称</summary>
    string GetCurrentTheme();

    /// <summary>应用当前保存的主题（从设置中读取）</summary>
    void ApplyTheme();

    /// <summary>主题变更事件（参数为新的主题名称）</summary>
    event EventHandler<string> ThemeChanged;
}
