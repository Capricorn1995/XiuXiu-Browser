// 浏览器标签页模型
// 表示一个浏览器标签页，包含 WebView2 引用和导航状态
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.Models;

/// <summary>
/// 浏览器标签页（可观察对象）
/// 每个标签页对应一个 WebView2 实例，管理独立的浏览会话
/// 使用 CommunityToolkit.Mvvm 源生成器实现属性变更通知
/// </summary>
public partial class BrowserTab : ObservableObject
{
    /// <summary>标签页唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>页面标题</summary>
    [ObservableProperty]
    private string _title = "新标签页";

    /// <summary>当前页面 URL</summary>
    [ObservableProperty]
    private string _url = "";

    /// <summary>网站图标 URL</summary>
    [ObservableProperty]
    private string _faviconUrl = "";

    /// <summary>是否正在加载页面</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>页面加载进度 (0-100)</summary>
    [ObservableProperty]
    private double _loadingProgress;

    /// <summary>是否可以后退</summary>
    [ObservableProperty]
    private bool _canGoBack;

    /// <summary>是否可以前进</summary>
    [ObservableProperty]
    private bool _canGoForward;

    /// <summary>WebView2 控件引用（由 BrowserService 管理生命周期）</summary>
    public WebView2? WebView { get; set; }
}
