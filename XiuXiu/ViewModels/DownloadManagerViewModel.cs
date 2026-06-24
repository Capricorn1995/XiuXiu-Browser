// 下载管理器 ViewModel
// 管理文件下载任务的状态和用户交互
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 下载管理器的 ViewModel
/// 显示活跃和已完成的下载任务，支持暂停/恢复/取消操作
/// </summary>
public partial class DownloadManagerViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 下载任务集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DownloadItem> _downloads = new();

    /// <summary>
    /// 活跃下载任务数量
    /// </summary>
    [ObservableProperty]
    private int _activeCount;

    /// <summary>
    /// 已完成下载任务数量
    /// </summary>
    [ObservableProperty]
    private int _completedCount;

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化下载管理器 ViewModel
    /// </summary>
    public DownloadManagerViewModel(IDownloadService downloadService, ISettingsService settingsService)
    {
        _downloadService = downloadService;
        _settingsService = settingsService;
    }

    // ===== 命令 =====

    /// <summary>
    /// 暂停指定下载
    /// </summary>
    [RelayCommand]
    private void PauseDownload(DownloadItem item)
    {
        _downloadService.PauseDownload(item.Id);
    }

    /// <summary>
    /// 恢复暂停的下载
    /// </summary>
    [RelayCommand]
    private void ResumeDownload(DownloadItem item)
    {
        _downloadService.ResumeDownload(item.Id);
    }

    /// <summary>
    /// 取消下载
    /// </summary>
    [RelayCommand]
    private void CancelDownload(DownloadItem item)
    {
        _downloadService.CancelDownload(item.Id);
        Downloads.Remove(item);
        UpdateCounts();
    }

    /// <summary>
    /// 清除所有已完成的下载记录
    /// </summary>
    [RelayCommand]
    private void ClearCompleted()
    {
        var completed = Downloads.Where(d =>
            d.Status == DownloadStatus.Completed ||
            d.Status == DownloadStatus.Failed ||
            d.Status == DownloadStatus.Cancelled).ToList();

        foreach (var item in completed)
        {
            Downloads.Remove(item);
        }

        UpdateCounts();
    }

    /// <summary>
    /// 打开下载文件夹
    /// </summary>
    [RelayCommand]
    private void OpenDownloadFolder()
    {
        string downloadPath = _settingsService.GetDownloadPath();

        if (string.IsNullOrEmpty(downloadPath))
        {
            downloadPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "XiuXiu");
        }

        if (System.IO.Directory.Exists(downloadPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", downloadPath);
        }
    }

    // ===== 辅助方法 =====

    /// <summary>
    /// 更新活跃和已完成任务计数
    /// </summary>
    private void UpdateCounts()
    {
        ActiveCount = Downloads.Count(d =>
            d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Queued);
        CompletedCount = Downloads.Count(d => d.Status == DownloadStatus.Completed);
    }
}
