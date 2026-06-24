// 历史记录 ViewModel
// 管理浏览历史的展示、搜索和清理
// 支持按日期分组显示和关键词搜索
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 历史记录面板的 ViewModel
/// 管理浏览历史的显示、搜索和清理功能
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 历史记录列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HistoryItem> _historyItems = new();

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// 是否按日期分组显示
    /// </summary>
    [ObservableProperty]
    private bool _isGroupedByDate = true;

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化历史记录 ViewModel
    /// </summary>
    public HistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    // ===== 命令 =====

    /// <summary>
    /// 加载历史记录
    /// </summary>
    [RelayCommand]
    private async Task LoadHistory()
    {
        List<HistoryItem> items;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            items = await _historyService.SearchAsync(SearchText);
        }
        else
        {
            items = await _historyService.GetRecentAsync(500);
        }

        HistoryItems.Clear();
        foreach (var item in items.OrderByDescending(i => i.VisitedAt))
        {
            HistoryItems.Add(item);
        }
    }

    /// <summary>
    /// 搜索历史记录
    /// </summary>
    [RelayCommand]
    private async Task SearchHistory()
    {
        await LoadHistory();
    }

    /// <summary>
    /// 清除所有历史记录
    /// </summary>
    [RelayCommand]
    private async Task ClearAll()
    {
        await _historyService.ClearAllAsync();
        HistoryItems.Clear();
    }

    /// <summary>
    /// 删除单条历史记录
    /// </summary>
    [RelayCommand]
    private async Task DeleteItem(HistoryItem item)
    {
        await _historyService.DeleteAsync(item.Id);
        HistoryItems.Remove(item);
    }

    /// <summary>
    /// 导航到历史记录中的 URL
    /// </summary>
    [RelayCommand]
    private async Task NavigateToHistory(HistoryItem item)
    {
        // 发送导航消息到 MainViewModel
        WeakReferenceMessenger.Default.Send(new NavigateToUrlMessage(item.Url));
        await Task.CompletedTask;
    }
}
