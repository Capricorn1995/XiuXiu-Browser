// 媒体面板 ViewModel
// 管理从网页中提取的媒体资源展示，支持筛选、选择和批量下载
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 媒体面板的 ViewModel
/// 管理嗅探到的图片和视频列表，支持类型筛选、关键词搜索和下载操作
/// </summary>
public partial class MediaPanelViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 媒体资源集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    /// <summary>
    /// 当前选中的媒体项
    /// </summary>
    [ObservableProperty]
    private MediaItem? _selectedMediaItem;

    /// <summary>
    /// 搜索筛选文本
    /// </summary>
    [ObservableProperty]
    private string _filterText = string.Empty;

    /// <summary>
    /// 当前选中的媒体类型筛选
    /// </summary>
    [ObservableProperty]
    private MediaType _selectedMediaType = MediaType.Image;

    /// <summary>
    /// 提取的媒体资源总数
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化媒体面板 ViewModel
    /// </summary>
    public MediaPanelViewModel(IDownloadService downloadService)
    {
        _downloadService = downloadService;

        // 注册媒体提取完成消息
        WeakReferenceMessenger.Default.Register<MediaExtractedMessage>(this, (r, m) =>
        {
            // 更新媒体列表
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MediaItems.Clear();
                foreach (var item in m.MediaItems)
                {
                    MediaItems.Add(item);
                }
                TotalCount = MediaItems.Count;
            });
        });
    }

    // ===== 命令 =====

    /// <summary>
    /// 在图片库中打开选中的媒体项
    /// </summary>
    [RelayCommand]
    private void OpenGallery(MediaItem? item)
    {
        if (item == null && SelectedMediaItem == null)
            return;

        var targetItem = item ?? SelectedMediaItem;
        if (targetItem == null)
            return;

        // 筛选当前类型的媒体项用于图库导航
        var items = MediaItems
            .Where(m => m.Type == targetItem.Type)
            .ToList();

        int index = items.IndexOf(targetItem);
        if (index < 0) index = 0;

        // 发送打开图库消息
        WeakReferenceMessenger.Default.Send(new OpenGalleryMessage(items, index));
    }

    /// <summary>
    /// 下载选中的媒体项
    /// </summary>
    [RelayCommand]
    private async Task DownloadSelected()
    {
        if (SelectedMediaItem == null)
            return;

        await _downloadService.StartDownloadAsync(SelectedMediaItem.Url);
    }

    /// <summary>
    /// 下载所有媒体项
    /// </summary>
    [RelayCommand]
    private async Task DownloadAll()
    {
        if (MediaItems.Count == 0)
            return;

        var urls = MediaItems.Select(m => m.Url).ToList();
        await _downloadService.StartBatchDownloadAsync(urls);
    }

    /// <summary>
    /// 选中指定媒体项
    /// </summary>
    [RelayCommand]
    private void SelectItem(MediaItem item)
    {
        // 切换选中状态
        item.IsSelected = !item.IsSelected;
        SelectedMediaItem = item;
    }

    /// <summary>
    /// 清除所有媒体项
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        MediaItems.Clear();
        TotalCount = 0;
        SelectedMediaItem = null;
    }
}
