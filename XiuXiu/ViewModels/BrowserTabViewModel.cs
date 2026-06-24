// 浏览器标签页 ViewModel
// 管理单个标签页的状态，包括 URL、标题、加载状态和 WebView2 引用
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.ViewModels;

/// <summary>
/// 单个浏览器标签页的 ViewModel
/// 每个标签页拥有独立的 WebView2 实例和导航状态
/// 使用 CommunityToolkit.Mvvm 源生成器实现属性变更通知
/// </summary>
public partial class BrowserTabViewModel : ObservableObject
{
    /// <summary>
    /// 标签页唯一标识符
    /// </summary>
    [ObservableProperty]
    private string _tabId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 标签页标题（网页标题）
    /// </summary>
    [ObservableProperty]
    private string _title = "新标签页";

    /// <summary>
    /// 当前加载的 URL
    /// </summary>
    [ObservableProperty]
    private string _url = string.Empty;

    /// <summary>
    /// 网站 Favicon URL
    /// </summary>
    [ObservableProperty]
    private string _faviconUrl = string.Empty;

    /// <summary>
    /// 页面是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 页面加载进度 (0-100)
    /// </summary>
    [ObservableProperty]
    private double _loadingProgress;

    /// <summary>
    /// 是否可以后退
    /// </summary>
    [ObservableProperty]
    private bool _canGoBack;

    /// <summary>
    /// 是否可以前进
    /// </summary>
    [ObservableProperty]
    private bool _canGoForward;

    /// <summary>
    /// WebView2 控件引用（由 View 层设置）
    /// </summary>
    public WebView2? WebView { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="initialUrl">初始加载的 URL，null 则使用空白页</param>
    public BrowserTabViewModel(string? initialUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(initialUrl))
        {
            _url = initialUrl;
        }
    }
}
