// 浏览器核心服务接口
// 管理 WebView2 实例的生命周期、导航、脚本执行和标签页销毁
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.Services;

/// <summary>
/// 浏览器服务接口
/// 提供多标签页 WebView2 管理、页面导航和脚本注入功能
/// </summary>
public interface IBrowserService
{
    /// <summary>初始化指定标签页的 WebView2 控件</summary>
    /// <param name="webView">WebView2 控件实例</param>
    Task InitializeAsync(WebView2 webView);

    /// <summary>导航到指定 URL</summary>
    /// <param name="url">目标 URL</param>
    Task NavigateAsync(string url);

    /// <summary>后退到上一页</summary>
    Task GoBackAsync();

    /// <summary>前进到下一页</summary>
    Task GoForwardAsync();

    /// <summary>重新加载当前页面</summary>
    Task ReloadAsync();

    /// <summary>停止加载当前页面</summary>
    Task StopAsync();

    /// <summary>在 WebView2 中执行 JavaScript 脚本并返回结果</summary>
    /// <param name="script">要执行的 JavaScript 代码</param>
    /// <returns>脚本执行结果字符串</returns>
    Task<string> ExecuteScriptAsync(string script);

    /// <summary>销毁指定标签页的 WebView2 实例并释放资源</summary>
    /// <param name="tabId">标签页 ID</param>
    void DisposeTab(string tabId);
}
