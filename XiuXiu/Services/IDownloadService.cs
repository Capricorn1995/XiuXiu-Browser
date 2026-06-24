// 下载服务接口
// 管理文件下载任务的生命周期：启动、暂停、恢复、取消和批量下载
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 下载进度报告数据
/// 用于批量下载时的进度通知
/// </summary>
public class DownloadProgress
{
    /// <summary>当前正在下载的条目</summary>
    public DownloadItem CurrentItem { get; set; } = new();

    /// <summary>已完成数量</summary>
    public int CompletedCount { get; set; }

    /// <summary>总数量</summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// 下载服务接口
/// 支持单个下载和批量下载，提供暂停/恢复/取消控制
/// </summary>
public interface IDownloadService
{
    /// <summary>启动单个文件下载</summary>
    /// <param name="url">下载 URL</param>
    /// <param name="fileName">指定文件名（可选，null 则从 URL 提取）</param>
    /// <param name="progress">进度报告器（可选）</param>
    /// <returns>下载任务对象</returns>
    Task<DownloadItem> StartDownloadAsync(string url, string? fileName = null, IProgress<double>? progress = null);

    /// <summary>启动批量文件下载</summary>
    /// <param name="urls">下载 URL 列表</param>
    /// <param name="progress">批量进度报告器（可选）</param>
    /// <returns>所有下载任务对象列表</returns>
    Task<List<DownloadItem>> StartBatchDownloadAsync(List<string> urls, IProgress<DownloadProgress>? progress = null);

    /// <summary>暂停指定下载任务</summary>
    /// <param name="downloadId">下载任务 ID</param>
    void PauseDownload(string downloadId);

    /// <summary>恢复已暂停的下载任务</summary>
    /// <param name="downloadId">下载任务 ID</param>
    void ResumeDownload(string downloadId);

    /// <summary>取消指定下载任务</summary>
    /// <param name="downloadId">下载任务 ID</param>
    void CancelDownload(string downloadId);

    /// <summary>
    /// 启动视频下载（自动检测 m3u8/mp4 等格式）
    /// </summary>
    /// <param name="url">视频 URL</param>
    /// <param name="progress">进度报告器（可选）</param>
    /// <returns>下载任务对象</returns>
    Task<DownloadItem> StartVideoDownloadAsync(string url, IProgress<double>? progress = null);

    /// <summary>获取下载保存目录路径</summary>
    /// <returns>下载目录的完整路径</returns>
    string GetDownloadDirectory();
}
