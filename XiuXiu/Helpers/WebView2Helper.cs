// WebView2 辅助工具类
// 提供 WebView2 运行时初始化、用户数据目录管理和创建属性配置
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.Helpers;

/// <summary>
/// WebView2 静态辅助类
/// 负责 WebView2 环境的初始化配置和用户数据管理
/// </summary>
public static class WebView2Helper
{
    // WebView2 用户数据文件夹路径（在 LocalAppData 下）
    // 每个应用应有独立的数据目录，避免冲突
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XiuXiu",
        "WebView2");

    /// <summary>
    /// 异步初始化 WebView2 环境
    /// 确保运行时可用，创建用户数据目录
    /// </summary>
    /// <returns>初始化任务</returns>
    public static async Task InitializeAsync()
    {
        // 确保用户数据目录存在
        if (!Directory.Exists(UserDataFolder))
        {
            Directory.CreateDirectory(UserDataFolder);
        }

        // 确保 WebView2 运行时已安装
        // CreateAsync 会自动下载或使用已安装的运行时
        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: UserDataFolder);
        }
        catch (Exception)
        {
            // WebView2 运行时未安装时，应用仍可启动
            // 首次导航时会触发运行时下载提示
        }
    }

    /// <summary>
    /// 获取默认的 WebView2 创建属性
    /// 配置用户数据目录和其他核心设置
    /// </summary>
    /// <returns>CoreWebView2CreationProperties 实例</returns>
    public static CoreWebView2CreationProperties GetDefaultCreationProperties()
    {
        return new CoreWebView2CreationProperties
        {
            // 设置用户数据文件夹，隔离不同应用的数据
            UserDataFolder = UserDataFolder,

            // 浏览器启动参数配置
            AdditionalBrowserArguments = string.Join(" ",
                "--disable-features=msWebView2BrowserHitTransparentWhenOverlapped",
                "--disable-gpu-vsync"
            ),

            // 语言设置为中文
            Language = "zh-CN",
        };
    }
}
