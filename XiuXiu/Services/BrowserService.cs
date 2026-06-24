// 浏览器服务实现
// 管理 WebView2 实例的多标签页生命周期和导航操作
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.Services;

/// <summary>
/// 浏览器服务
/// 使用 Dictionary 跟踪所有标签页的 WebView2 实例
/// 处理导航事件并通过 WeakReferenceMessenger 发送消息
/// </summary>
public class BrowserService : IBrowserService
{
    // 当前活动标签页的 WebView2 引用
    private WebView2? _activeWebView;

    // 标签页 ID → WebView2 实例映射表
    private readonly Dictionary<string, WebView2> _tabWebViews = new();

    // WebView2 用户数据文件夹路径
    private readonly string _userDataFolder;

    public BrowserService()
    {
        // WebView2 用户数据存储目录
        _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XiuXiu", "WebView2Data");
    }

    /// <summary>
    /// 初始化指定标签页的 WebView2 控件
    /// 配置 CoreWebView2 环境并注册导航事件处理
    /// </summary>
    public async Task InitializeAsync(WebView2 webView)
    {
        _activeWebView = webView;

        // 确保用户数据目录存在
        Directory.CreateDirectory(_userDataFolder);

        // 创建 CoreWebView2 环境
        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: _userDataFolder);

        await webView.EnsureCoreWebView2Async(env);

        // 配置 WebView2 设置
        var coreWebView = webView.CoreWebView2;
        coreWebView.Settings.IsScriptEnabled = true;
        coreWebView.Settings.IsWebMessageEnabled = true;
        coreWebView.Settings.AreDefaultScriptDialogsEnabled = true;
        coreWebView.Settings.IsStatusBarEnabled = false;

        // 注册导航开始事件
        webView.NavigationStarting += (s, e) =>
        {
            WeakReferenceMessenger.Default.Send(new NavigationStartingMessage(e.Uri));
        };

        // 注册导航完成事件
        webView.NavigationCompleted += (s, e) =>
        {
            var currentUrl = webView.Source?.ToString() ?? "";
            WeakReferenceMessenger.Default.Send(new NavigationCompletedMessage(
                currentUrl, e.IsSuccess, e.WebErrorStatus.ToString()));
        };

        // 注册 WebMessageReceived 事件（接收页面 JavaScript 消息）
        webView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            WeakReferenceMessenger.Default.Send(new WebMessageReceivedMessage(e.TryGetWebMessageAsString()));
        };

        // 注册文档标题变更事件
        webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
        {
            WeakReferenceMessenger.Default.Send(new TitleChangedMessage(webView.CoreWebView2.DocumentTitle));
        };
    }

    /// <summary>
    /// 导航到指定 URL
    /// </summary>
    public async Task NavigateAsync(string url)
    {
        if (_activeWebView?.CoreWebView2 == null)
            return;

        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _activeWebView.CoreWebView2.Navigate(url);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 后退到上一页
    /// </summary>
    public async Task GoBackAsync()
    {
        if (_activeWebView?.CoreWebView2?.CanGoBack == true)
        {
            _activeWebView.CoreWebView2.GoBack();
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 前进到下一页
    /// </summary>
    public async Task GoForwardAsync()
    {
        if (_activeWebView?.CoreWebView2?.CanGoForward == true)
        {
            _activeWebView.CoreWebView2.GoForward();
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 重新加载当前页面
    /// </summary>
    public async Task ReloadAsync()
    {
        _activeWebView?.CoreWebView2?.Reload();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 停止加载当前页面
    /// </summary>
    public async Task StopAsync()
    {
        _activeWebView?.CoreWebView2?.Stop();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 在活动 WebView2 中执行 JavaScript 脚本
    /// </summary>
    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (_activeWebView?.CoreWebView2 == null)
            return string.Empty;

        return await _activeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    /// <summary>
    /// 销毁指定标签页的 WebView2 实例
    /// 释放 WebView2 资源并从跟踪字典中移除
    /// </summary>
    public void DisposeTab(string tabId)
    {
        if (_tabWebViews.TryGetValue(tabId, out var webView))
        {
            webView.Dispose();
            _tabWebViews.Remove(tabId);
        }
    }
}

// ===== 消息定义 =====
// 使用 CommunityToolkit.Mvvm 的 WeakReferenceMessenger 进行组件间通信

/// <summary>导航开始消息</summary>
public record NavigationStartingMessage(string Url);

/// <summary>导航完成消息</summary>
public record NavigationCompletedMessage(string Url, bool IsSuccess, string ErrorStatus);

/// <summary>Web 消息接收消息</summary>
public record WebMessageReceivedMessage(string Message);

/// <summary>页面标题变更消息</summary>
public record TitleChangedMessage(string Title);
