// 下载项目模型
// 表示单个文件下载任务的状态和进度
using CommunityToolkit.Mvvm.ComponentModel;

namespace XiuXiu.Models;

/// <summary>
/// 下载状态枚举
/// </summary>
public enum DownloadStatus { Queued, Downloading, Paused, Completed, Failed, Cancelled }

/// <summary>
/// 下载任务项（可观察对象）
/// 使用 CommunityToolkit.Mvvm 源生成器实现属性变更通知
/// </summary>
public partial class DownloadItem : ObservableObject
{
    /// <summary>下载任务唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>下载资源的 URL</summary>
    public string Url { get; set; } = "";

    /// <summary>保存的文件名</summary>
    public string FileName { get; set; } = "";

    /// <summary>保存的完整路径</summary>
    public string FilePath { get; set; } = "";

    /// <summary>下载状态</summary>
    [ObservableProperty]
    private DownloadStatus _status = DownloadStatus.Queued;

    /// <summary>下载进度百分比 (0-100)</summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>文件总大小（字节）</summary>
    public long? TotalBytes { get; set; }

    /// <summary>已下载字节数</summary>
    public long DownloadedBytes { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>错误信息（下载失败时填充）</summary>
    [ObservableProperty]
    private string? _errorMessage;
}
