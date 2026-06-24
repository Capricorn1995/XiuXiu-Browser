// 图片库 ViewModel
// 管理全屏图片库的浏览和查看功能，支持缩放、平移和键盘导航
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 全屏图片库的 ViewModel
/// 用于浏览和管理从网页中嗅探到的图片资源
/// 支持缩放、平移和键盘导航
/// </summary>
public partial class GalleryViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 图库中的所有媒体项
    /// </summary>
    [ObservableProperty]
    private List<MediaItem> _items = new();

    /// <summary>
    /// 当前显示项的索引
    /// </summary>
    [ObservableProperty]
    private int _currentIndex;

    /// <summary>
    /// 当前显示的媒体项
    /// </summary>
    [ObservableProperty]
    private MediaItem? _currentItem;

    /// <summary>
    /// 缩放级别（1.0 = 原始大小）
    /// </summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// X 轴平移偏移
    /// </summary>
    [ObservableProperty]
    private double _panX;

    /// <summary>
    /// Y 轴平移偏移
    /// </summary>
    [ObservableProperty]
    private double _panY;

    /// <summary>
    /// 位置指示文本（如 "3/15"）
    /// </summary>
    [ObservableProperty]
    private string _positionText = string.Empty;

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化图片库 ViewModel
    /// </summary>
    public GalleryViewModel(IDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    /// <summary>
    /// 设置图库内容并跳转到指定位置
    /// </summary>
    /// <param name="items">媒体项列表</param>
    /// <param name="startIndex">起始索引</param>
    public void Initialize(List<MediaItem> items, int startIndex = 0)
    {
        Items = items;
        CurrentIndex = Math.Clamp(startIndex, 0, items.Count - 1);
        UpdateCurrentItem();
    }

    // ===== 命令 - 导航 =====

    /// <summary>
    /// 切换到下一张
    /// </summary>
    [RelayCommand]
    private void Next()
    {
        if (Items.Count == 0) return;

        CurrentIndex = (CurrentIndex + 1) % Items.Count;
        UpdateCurrentItem();
    }

    /// <summary>
    /// 切换到上一张
    /// </summary>
    [RelayCommand]
    private void Previous()
    {
        if (Items.Count == 0) return;

        CurrentIndex = (CurrentIndex - 1 + Items.Count) % Items.Count;
        UpdateCurrentItem();
    }

    /// <summary>
    /// 关闭图片库
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // 由 View 层处理窗口关闭
    }

    // ===== 命令 - 操作 =====

    /// <summary>
    /// 下载当前显示的图片
    /// </summary>
    [RelayCommand]
    private async Task DownloadCurrent()
    {
        if (CurrentItem == null) return;

        await _downloadService.StartDownloadAsync(CurrentItem.Url);
    }

    /// <summary>
    /// 放大
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel * 1.25, 10.0);
    }

    /// <summary>
    /// 缩小
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1);
    }

    /// <summary>
    /// 重置缩放
    /// </summary>
    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 更新当前显示项和位置文本
    /// </summary>
    private void UpdateCurrentItem()
    {
        if (CurrentIndex >= 0 && CurrentIndex < Items.Count)
        {
            CurrentItem = Items[CurrentIndex];
            PositionText = $"{CurrentIndex + 1}/{Items.Count}";
            ResetZoom();
        }
    }

    /// <summary>
    /// 当前项变更时更新位置文本
    /// </summary>
    partial void OnCurrentItemChanged(MediaItem? value)
    {
        if (value != null && Items.Count > 0)
        {
            PositionText = $"{CurrentIndex + 1}/{Items.Count}";
        }
    }
}
