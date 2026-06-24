// 应用设置模型
// 封装所有可持久化的用户配置项
namespace XiuXiu.Models;

/// <summary>
/// 应用程序设置
/// 通过 ISettingsService 持久化为 JSON 文件
/// </summary>
public class AppSettings
{
    /// <summary>默认搜索引擎名称</summary>
    public string DefaultSearchEngine { get; set; } = "Baidu";

    /// <summary>下载文件保存路径（空字符串表示使用默认路径）</summary>
    public string DownloadPath { get; set; } = "";

    /// <summary>主题模式："Light"（浅色）, "Dark"（深色）, "System"（跟随系统）</summary>
    public string Theme { get; set; } = "System";

    /// <summary>主题强调色，十六进制颜色值</summary>
    public string AccentColor { get; set; } = "#319FDE";

    /// <summary>退出时是否自动清除浏览历史</summary>
    public bool ClearHistoryOnExit { get; set; } = false;

    /// <summary>页面加载完成后是否自动嗅探媒体资源</summary>
    public bool AutoExtractOnPageLoad { get; set; } = true;

    /// <summary>浏览器主页 URL</summary>
    public string HomePage { get; set; } = "https://www.baidu.com";
}
