// 主窗口 ViewModel
// 管理浏览器主窗口的整体状态
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Web.WebView2.Wpf;
using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMediaExtractionService _mediaExtractionService;
    private readonly IDownloadService _downloadService;
    private readonly IBookmarkService _bookmarkService;
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;

    // WebView2 引用（由 MainWindow 在加载后设置）
    public WebView2? BrowserWebView { get; set; }

    // ===== 地址栏 =====
    [ObservableProperty] private string _addressBarText = "https://www.baidu.com";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private double _loadingProgress;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;

    // ===== 标签页 =====
    [ObservableProperty] private ObservableCollection<BrowserTabViewModel> _tabs = new();
    [ObservableProperty] private BrowserTabViewModel? _activeTab;

    // ===== 媒体面板 =====
    [ObservableProperty] private bool _isMediaPanelOpen;
    [ObservableProperty] private ObservableCollection<MediaItem> _extractedMedia = new();
    [ObservableProperty] private int _extractedCount;

    // ===== 面板开关 =====
    [ObservableProperty] private bool _isDownloadPanelVisible;
    [ObservableProperty] private bool _isBookmarkPanelVisible;
    [ObservableProperty] private bool _isHistoryPanelVisible;
    [ObservableProperty] private bool _isSettingsPanelVisible;

    // ===== 状态栏 =====
    [ObservableProperty] private string _statusText = "就绪";

    public MainViewModel(
        IMediaExtractionService mediaExtractionService,
        IDownloadService downloadService,
        IBookmarkService bookmarkService,
        IHistoryService historyService,
        ISettingsService settingsService)
    {
        _mediaExtractionService = mediaExtractionService;
        _downloadService = downloadService;
        _bookmarkService = bookmarkService;
        _historyService = historyService;
        _settingsService = settingsService;
    }

    // ===== 导航命令 =====

    [RelayCommand]
    private void GoBack()
    {
        if (BrowserWebView?.CoreWebView2?.CanGoBack == true)
            BrowserWebView.CoreWebView2.GoBack();
    }

    [RelayCommand]
    private void GoForward()
    {
        if (BrowserWebView?.CoreWebView2?.CanGoForward == true)
            BrowserWebView.CoreWebView2.GoForward();
    }

    [RelayCommand]
    private void Reload() => BrowserWebView?.CoreWebView2?.Reload();

    [RelayCommand]
    private void StopLoading() => BrowserWebView?.CoreWebView2?.Stop();

    [RelayCommand]
    private void NavigateToUrl()
    {
        string url = AddressBarText?.Trim() ?? "";
        if (string.IsNullOrEmpty(url)) return;

        // 自动添加协议或转为搜索
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            if (url.Contains('.') && !url.Contains(' '))
                url = "https://" + url;
            else
                url = "https://www.baidu.com/s?wd=" + Uri.EscapeDataString(url);
        }

        AddressBarText = url;
        BrowserWebView?.CoreWebView2?.Navigate(url);
    }

    // ===== 标签页命令 =====

    [RelayCommand]
    private void AddNewTab()
    {
        var newTab = new BrowserTabViewModel(_settingsService.HomePage);
        Tabs.Add(newTab);
        SwitchTab(newTab);
    }

    [RelayCommand]
    private void CloseTab(BrowserTabViewModel tab)
    {
        if (Tabs.Count <= 1) return;
        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (ActiveTab == tab)
            SwitchTab(index >= Tabs.Count ? Tabs[^1] : Tabs[index]);
    }

    [RelayCommand]
    private void SwitchTab(BrowserTabViewModel tab)
    {
        if (ActiveTab == tab) return;
        ActiveTab = tab;
        AddressBarText = tab.Url;
    }

    // ===== "嗅一嗅" 命令 =====

    [RelayCommand]
    private async Task SniffMedia()
    {
        if (BrowserWebView?.CoreWebView2 == null)
        {
            StatusText = "无法嗅探：页面尚未加载完成";
            return;
        }

        StatusText = "正在嗅探媒体资源...";

        try
        {
            var mediaItems = await _mediaExtractionService.ExtractMediaAsync(
                BrowserWebView.CoreWebView2);

            ExtractedMedia.Clear();
            foreach (var item in mediaItems)
                ExtractedMedia.Add(item);

            ExtractedCount = mediaItems.Count;
            IsMediaPanelOpen = true;
            StatusText = $"嗅探完成，找到 {mediaItems.Count} 个资源";
        }
        catch (Exception ex)
        {
            StatusText = $"嗅探失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleMediaPanel() => IsMediaPanelOpen = !IsMediaPanelOpen;

    [RelayCommand]
    private async Task DownloadAllMedia()
    {
        if (ExtractedMedia.Count == 0) return;
        int total = ExtractedMedia.Count;
        int current = 0;
        string downloadDir = _downloadService.GetDownloadDirectory();

        foreach (var item in ExtractedMedia)
        {
            current++;
            StatusText = $"正在下载 {current}/{total}: {item.FileName}";

            try
            {
                if (item.Type == MediaType.Video && DownloadService.IsVideoUrl(item.Url))
                {
                    await _downloadService.StartVideoDownloadAsync(item.Url);
                }
                else
                {
                    await _downloadService.StartDownloadAsync(item.Url);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"下载 {item.FileName} 失败: {ex.Message}";
            }
        }

        StatusText = $"下载完成: {total} 个资源 → {downloadDir}";
    }

    [RelayCommand]
    private async Task DownloadSingleMedia(MediaItem? item)
    {
        if (item == null) return;
        StatusText = $"正在下载: {item.FileName}...";

        try
        {
            DownloadItem result;

            if (item.Type == MediaType.Video && DownloadService.IsVideoUrl(item.Url))
            {
                if (DownloadService.IsM3u8Url(item.Url))
                {
                    StatusText = "正在解析m3u8...";
                }

                var progress = new Progress<double>(p =>
                {
                    if (DownloadService.IsM3u8Url(item.Url))
                    {
                        if (p < 5)
                            StatusText = $"正在解析m3u8...";
                        else if (p < 90)
                            StatusText = $"下载分片中... {p:F0}%";
                        else if (p < 100)
                            StatusText = $"合并视频中...";
                    }
                    else
                    {
                        StatusText = $"正在下载 {item.FileName}: {p:F0}%";
                    }
                });

                result = await _downloadService.StartVideoDownloadAsync(item.Url, progress);
            }
            else
            {
                result = await _downloadService.StartDownloadAsync(item.Url);
            }

            if (result.Status == Models.DownloadStatus.Completed)
                StatusText = $"下载完成: {item.FileName}";
            else if (result.Status == Models.DownloadStatus.Failed)
                StatusText = $"下载失败: {result.ErrorMessage ?? "未知错误"}";
            else
                StatusText = $"下载状态: {result.Status}";
        }
        catch (Exception ex)
        {
            StatusText = $"下载失败: {ex.Message}";
        }
    }

    // ===== 书签命令 =====

    [RelayCommand]
    private async Task ToggleBookmark()
    {
        var url = BrowserWebView?.Source?.ToString();
        if (string.IsNullOrEmpty(url)) return;

        bool isBookmarked = await _bookmarkService.IsBookmarkedAsync(url);
        if (isBookmarked)
        {
            var existing = await _bookmarkService.GetByUrlAsync(url);
            if (existing != null) await _bookmarkService.DeleteAsync(existing.Id);
            StatusText = "已移除书签";
        }
        else
        {
            await _bookmarkService.AddAsync(new BookmarkItem
            {
                Title = BrowserWebView?.CoreWebView2?.DocumentTitle ?? url,
                Url = url,
                CreatedAt = DateTime.UtcNow
            });
            StatusText = "已添加书签";
        }
    }

    // ===== 面板切换 =====

    [RelayCommand]
    private void ToggleDownloadPanel()
    {
        IsDownloadPanelVisible = !IsDownloadPanelVisible;
        if (IsDownloadPanelVisible)
        {
            string dir = _downloadService.GetDownloadDirectory();
            StatusText = $"下载目录: {dir}";
            // 尝试用文件管理器打开下载目录
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch
            {
                StatusText = $"下载目录: {dir}（无法打开资源管理器）";
            }
        }
        else
        {
            StatusText = "下载面板已关闭";
        }
    }

    [RelayCommand]
    private void ToggleBookmarkPanel()
    {
        IsBookmarkPanelVisible = !IsBookmarkPanelVisible;
        StatusText = IsBookmarkPanelVisible ? "书签面板已打开" : "书签面板已关闭";
    }

    [RelayCommand]
    private void ToggleHistoryPanel()
    {
        IsHistoryPanelVisible = !IsHistoryPanelVisible;
        StatusText = IsHistoryPanelVisible ? "历史记录面板已打开" : "历史记录面板已关闭";
    }

    [RelayCommand]
    private void ToggleSettingsPanel()
    {
        IsSettingsPanelVisible = !IsSettingsPanelVisible;
        StatusText = IsSettingsPanelVisible ? "设置面板已打开" : "设置面板已关闭";
    }
}
