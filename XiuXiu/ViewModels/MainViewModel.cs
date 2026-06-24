// 主窗口 ViewModel
// 管理浏览器主窗口的整体状态，包括标签页管理、导航控制和侧边栏切换
// 使用 WeakReferenceMessenger 进行跨 ViewModel 通信
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Web.WebView2.Wpf;

using XiuXiu.Helpers;
using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 主窗口的 ViewModel，作为应用程序的核心控制器
/// 管理标签页集合、地址栏、导航操作、媒体嗅探和侧边栏面板
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ===== 注入的服务 =====
    private readonly IBrowserService _browserService;
    private readonly IMediaExtractionService _mediaExtractionService;
    private readonly IDownloadService _downloadService;
    private readonly IBookmarkService _bookmarkService;
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;

    // ===== 可观察属性 - 标签页 =====

    /// <summary>
    /// 浏览器标签页集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BrowserTabViewModel> _tabs = new();

    /// <summary>
    /// 当前活动的标签页
    /// </summary>
    [ObservableProperty]
    private BrowserTabViewModel? _activeTab;

    // ===== 可观察属性 - 地址栏 =====

    /// <summary>
    /// 地址栏中的 URL 文本
    /// </summary>
    [ObservableProperty]
    private string _addressBarText = string.Empty;

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

    // ===== 可观察属性 - 媒体面板 =====

    /// <summary>
    /// 媒体面板是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isMediaPanelOpen;

    /// <summary>
    /// 嗅探到的媒体资源集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MediaItem> _extractedMedia = new();

    /// <summary>
    /// 媒体筛选文本
    /// </summary>
    [ObservableProperty]
    private string _mediaFilterText = string.Empty;

    /// <summary>
    /// 当前媒体类型筛选
    /// </summary>
    [ObservableProperty]
    private MediaType _mediaTypeFilter = MediaType.Image;

    /// <summary>
    /// 提取到的媒体资源数量
    /// </summary>
    [ObservableProperty]
    private int _extractedCount;

    // ===== 可观察属性 - 状态栏 =====

    /// <summary>
    /// 状态栏文本
    /// </summary>
    [ObservableProperty]
    private string _statusText = "就绪";

    // ===== 可观察属性 - 其他面板 =====

    /// <summary>
    /// 下载管理器是否可见
    /// </summary>
    [ObservableProperty]
    private bool _isDownloadPanelVisible;

    /// <summary>
    /// 书签面板是否可见
    /// </summary>
    [ObservableProperty]
    private bool _isBookmarkPanelVisible;

    /// <summary>
    /// 历史记录面板是否可见
    /// </summary>
    [ObservableProperty]
    private bool _isHistoryPanelVisible;

    /// <summary>
    /// 设置面板是否可见
    /// </summary>
    [ObservableProperty]
    private bool _isSettingsPanelVisible;

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化主 ViewModel，注入所有必要的服务
    /// </summary>
    public MainViewModel(
        IBrowserService browserService,
        IMediaExtractionService mediaExtractionService,
        IDownloadService downloadService,
        IBookmarkService bookmarkService,
        IHistoryService historyService,
        ISettingsService settingsService)
    {
        _browserService = browserService;
        _mediaExtractionService = mediaExtractionService;
        _downloadService = downloadService;
        _bookmarkService = bookmarkService;
        _historyService = historyService;
        _settingsService = settingsService;

        // 注册消息处理
        RegisterMessages();
    }

    // ===== 命令 - 标签页管理 =====

    /// <summary>
    /// 添加新标签页
    /// </summary>
    [RelayCommand]
    private void AddNewTab()
    {
        var newTab = new BrowserTabViewModel(_settingsService.HomePage);
        Tabs.Add(newTab);
        SwitchTab(newTab);
    }

    /// <summary>
    /// 关闭指定标签页
    /// 至少保留一个标签页
    /// </summary>
    [RelayCommand]
    private async Task CloseTab(BrowserTabViewModel tab)
    {
        if (Tabs.Count <= 1)
            return;

        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        // 如果关闭的是活动标签页，切换到相邻标签页
        if (ActiveTab == tab)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;
            SwitchTab(Tabs[index]);
        }

        // 销毁 WebView2 资源
        _browserService.DisposeTab(tab.TabId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 切换到指定标签页
    /// 同步活动标签页的状态到主窗口
    /// </summary>
    [RelayCommand]
    private void SwitchTab(BrowserTabViewModel tab)
    {
        if (ActiveTab == tab)
            return;

        ActiveTab = tab;

        // 同步地址栏和导航状态
        AddressBarText = tab.Url;
        IsLoading = tab.IsLoading;
        LoadingProgress = tab.LoadingProgress;
        CanGoBack = tab.CanGoBack;
        CanGoForward = tab.CanGoForward;

        // 发送标签页切换消息
        WeakReferenceMessenger.Default.Send(new TabChangedMessage(tab.TabId));
    }

    // ===== 命令 - 导航操作 =====

    /// <summary>
    /// 导航到地址栏中的 URL
    /// 自动识别搜索关键词并跳转到搜索引擎
    /// </summary>
    [RelayCommand]
    private async Task NavigateToUrl()
    {
        if (string.IsNullOrWhiteSpace(AddressBarText))
            return;

        string normalizedUrl = UrlHelper.NormalizeUrl(
            AddressBarText, _settingsService.DefaultSearchEngine);
        await _browserService.NavigateAsync(normalizedUrl);
    }

    /// <summary>
    /// 浏览器后退
    /// </summary>
    [RelayCommand]
    private async Task GoBack()
    {
        await _browserService.GoBackAsync();
    }

    /// <summary>
    /// 浏览器前进
    /// </summary>
    [RelayCommand]
    private async Task GoForward()
    {
        await _browserService.GoForwardAsync();
    }

    /// <summary>
    /// 刷新当前页面
    /// </summary>
    [RelayCommand]
    private async Task Reload()
    {
        await _browserService.ReloadAsync();
    }

    /// <summary>
    /// 停止加载当前页面
    /// </summary>
    [RelayCommand]
    private async Task StopLoading()
    {
        await _browserService.StopAsync();
    }

    // ===== 命令 - 媒体嗅探（核心功能） =====

    /// <summary>
    /// 嗅探当前页面中的媒体资源（"嗅一嗅"按钮）
    /// 核心流程：
    /// 1. 获取活动标签页的 CoreWebView2
    /// 2. 通过 MediaExtractionService 提取媒体资源
    /// 3. 更新 ExtractedMedia 集合
    /// 4. 打开媒体面板
    /// 5. 更新状态文本
    /// </summary>
    [RelayCommand]
    private async Task SniffMedia()
    {
        if (ActiveTab?.WebView?.CoreWebView2 == null)
        {
            StatusText = "无法嗅探：页面尚未加载完成";
            return;
        }

        StatusText = "正在嗅探媒体资源...";
        IsLoading = true;

        try
        {
            // 调用媒体提取服务，从 WebView2 中提取资源
            var mediaItems = await _mediaExtractionService.ExtractMediaAsync(
                ActiveTab.WebView.CoreWebView2);

            // 更新媒体集合
            ExtractedMedia.Clear();
            foreach (var item in mediaItems)
            {
                ExtractedMedia.Add(item);
            }

            ExtractedCount = mediaItems.Count;

            // 打开媒体面板
            IsMediaPanelOpen = true;

            // 更新状态
            StatusText = $"嗅探完成，找到 {mediaItems.Count} 个资源";

            // 发送媒体提取完成消息
            WeakReferenceMessenger.Default.Send(
                new MediaExtractedMessage(ExtractedMedia));
        }
        catch (Exception ex)
        {
            StatusText = $"嗅探失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 切换媒体面板显示状态
    /// </summary>
    [RelayCommand]
    private void ToggleMediaPanel()
    {
        IsMediaPanelOpen = !IsMediaPanelOpen;
    }

    /// <summary>
    /// 一键下载所有已提取的媒体资源
    /// </summary>
    [RelayCommand]
    private async Task DownloadAllMedia()
    {
        if (ExtractedMedia.Count == 0)
            return;

        StatusText = $"正在下载 {ExtractedMedia.Count} 个资源...";

        var urls = ExtractedMedia.Select(m => m.Url).ToList();
        await _downloadService.StartBatchDownloadAsync(urls);

        StatusText = $"下载完成: {ExtractedMedia.Count} 个资源";
    }

    // ===== 命令 - 书签操作 =====

    /// <summary>
    /// 切换当前页面的书签状态（添加/移除）
    /// </summary>
    [RelayCommand]
    private async Task ToggleBookmark()
    {
        if (ActiveTab == null || string.IsNullOrWhiteSpace(ActiveTab.Url))
            return;

        string url = ActiveTab.Url;
        bool isBookmarked = await _bookmarkService.IsBookmarkedAsync(url);

        if (isBookmarked)
        {
            // 移除书签
            var existing = await _bookmarkService.GetByUrlAsync(url);
            if (existing != null)
            {
                await _bookmarkService.DeleteAsync(existing.Id);
            }
            StatusText = "已移除书签";
        }
        else
        {
            // 添加书签
            var bookmark = new BookmarkItem
            {
                Title = ActiveTab.Title,
                Url = url,
                FaviconUrl = ActiveTab.FaviconUrl,
                CreatedAt = DateTime.UtcNow
            };
            await _bookmarkService.AddAsync(bookmark);
            StatusText = "已添加书签";
        }
    }

    // ===== 命令 - 面板切换 =====

    /// <summary>
    /// 切换下载管理器显示状态
    /// </summary>
    [RelayCommand]
    private void ToggleDownloadPanel()
    {
        IsDownloadPanelVisible = !IsDownloadPanelVisible;
    }

    /// <summary>
    /// 切换书签面板显示状态
    /// </summary>
    [RelayCommand]
    private void ToggleBookmarkPanel()
    {
        IsBookmarkPanelVisible = !IsBookmarkPanelVisible;
    }

    /// <summary>
    /// 切换历史记录面板显示状态
    /// </summary>
    [RelayCommand]
    private void ToggleHistoryPanel()
    {
        IsHistoryPanelVisible = !IsHistoryPanelVisible;
    }

    /// <summary>
    /// 切换设置面板显示状态
    /// </summary>
    [RelayCommand]
    private void ToggleSettingsPanel()
    {
        IsSettingsPanelVisible = !IsSettingsPanelVisible;
    }

    // ===== 消息注册 =====

    /// <summary>
    /// 注册 WeakReferenceMessenger 消息处理
    /// 处理来自其他 ViewModel 的导航请求和状态更新
    /// </summary>
    private void RegisterMessages()
    {
        // 处理来自书签或历史的导航请求
        WeakReferenceMessenger.Default.Register<NavigateToUrlMessage>(this, (r, m) =>
        {
            AddressBarText = m.Url;
            NavigateToUrlCommand.Execute(null);
        });

        // 处理打开图库请求
        WeakReferenceMessenger.Default.Register<OpenGalleryMessage>(this, (r, m) =>
        {
            // 由 GalleryViewModel 处理，这里可做日志记录
        });

        // 处理状态消息
        WeakReferenceMessenger.Default.Register<StatusMessage>(this, (r, m) =>
        {
            StatusText = m.Message;
        });

        // 处理主题变更消息
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            // 主题变更后的额外处理
        });
    }

    // ===== 部分方法（由源生成器触发） =====

    /// <summary>
    /// 活动标签页变更时同步状态
    /// </summary>
    partial void OnActiveTabChanged(BrowserTabViewModel? value)
    {
        if (value != null)
        {
            AddressBarText = value.Url;
            IsLoading = value.IsLoading;
            CanGoBack = value.CanGoBack;
            CanGoForward = value.CanGoForward;
        }
    }
}
