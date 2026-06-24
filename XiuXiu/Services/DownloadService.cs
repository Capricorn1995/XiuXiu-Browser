// 下载服务实现
// 使用 HttpClient 流式下载文件，支持暂停/恢复/取消和批量下载
// 通过 ConcurrentDictionary 线程安全地管理活跃下载任务
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using XiuXiu.Helpers;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 下载服务
/// 核心特性：
/// - 流式下载，支持大文件
/// - CancellationTokenSource 实现暂停/取消控制
/// - IProgress&lt;double&gt; 报告单个下载进度
/// - IProgress&lt;DownloadProgress&gt; 报告批量下载进度
/// - 安全文件名生成，避免路径冲突
/// - ConcurrentDictionary 线程安全管理
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    // 活跃下载任务字典
    private readonly ConcurrentDictionary<string, DownloadItem> _activeDownloads = new();

    // 取消令牌字典（key: 下载任务 ID）
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

    public DownloadService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // 配置 HttpClient 支持重定向和自定义 User-Agent
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        });
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromMinutes(30); // 大文件下载超时
    }

    /// <summary>
    /// 启动单个文件下载
    /// 流程：
    /// 1. 创建 DownloadItem 并生成安全文件名
    /// 2. 确定保存路径并确保目录存在
    /// 3. 创建 CancellationTokenSource
    /// 4. 流式下载文件，报告进度
    /// 5. 处理异常（取消/失败）
    /// </summary>
    public async Task<DownloadItem> StartDownloadAsync(string url, string? fileName = null, IProgress<double>? progress = null)
    {
        var downloadItem = new DownloadItem
        {
            Url = url,
            Status = DownloadStatus.Downloading
        };

        // 生成文件名
        downloadItem.FileName = fileName ?? FileHelper.GetSafeFileName(url);

        // 确定保存路径
        string directory = GetDownloadDirectory();
        downloadItem.FilePath = FileHelper.GetUniqueFilePath(directory, downloadItem.FileName);

        // 注册到活跃下载列表
        _activeDownloads[downloadItem.Id] = downloadItem;
        var cts = new CancellationTokenSource();
        _cancellationTokens[downloadItem.Id] = cts;

        try
        {
            await ExecuteDownloadAsync(downloadItem, cts.Token, progress);
        }
        catch (OperationCanceledException)
        {
            // 检查是暂停还是取消
            if (downloadItem.Status == DownloadStatus.Paused)
            {
                // 保持 Paused 状态，不移除
            }
            else
            {
                downloadItem.Status = DownloadStatus.Cancelled;
                CleanupFailedDownload(downloadItem);
                _activeDownloads.TryRemove(downloadItem.Id, out _);
            }
        }
        catch (Exception ex)
        {
            downloadItem.Status = DownloadStatus.Failed;
            downloadItem.ErrorMessage = ex.Message;
            CleanupFailedDownload(downloadItem);
            _activeDownloads.TryRemove(downloadItem.Id, out _);
        }
        finally
        {
            _cancellationTokens.TryRemove(downloadItem.Id, out _);
        }

        return downloadItem;
    }

    /// <summary>
    /// 批量下载多个文件
    /// 依次下载每个文件（非并发），通过 IProgress&lt;DownloadProgress&gt; 报告总体进度
    /// </summary>
    public async Task<List<DownloadItem>> StartBatchDownloadAsync(
        List<string> urls, IProgress<DownloadProgress>? progress = null)
    {
        var results = new List<DownloadItem>();
        int total = urls.Count;
        int completed = 0;

        foreach (var url in urls)
        {
            // 单个文件进度报告
            var itemProgress = new Progress<double>(p =>
            {
                // 可在 ViewModel 中绑定单个条目进度
            });

            var item = await StartDownloadAsync(url, progress: itemProgress);
            results.Add(item);
            completed++;

            // 报告批量进度
            progress?.Report(new DownloadProgress
            {
                CurrentItem = item,
                CompletedCount = completed,
                TotalCount = total
            });
        }

        return results;
    }

    /// <summary>
    /// 暂停下载（通过取消令牌停止当前下载，保留进度信息以便恢复）
    /// </summary>
    public void PauseDownload(string downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var item) &&
            item.Status == DownloadStatus.Downloading)
        {
            item.Status = DownloadStatus.Paused;

            if (_cancellationTokens.TryGetValue(downloadId, out var cts))
            {
                cts.Cancel();
            }
        }
    }

    /// <summary>
    /// 恢复已暂停的下载
    /// 注意：当前实现为重新下载（非断点续传）
    /// 断点续传需要服务器支持 Range 请求
    /// </summary>
    public void ResumeDownload(string downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var item) &&
            item.Status == DownloadStatus.Paused)
        {
            // 重新启动下载任务
            _ = StartDownloadAsync(item.Url, item.FileName);
        }
    }

    /// <summary>
    /// 取消下载并清理未完成的文件
    /// </summary>
    public void CancelDownload(string downloadId)
    {
        if (_cancellationTokens.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
        }

        if (_activeDownloads.TryRemove(downloadId, out var item))
        {
            item.Status = DownloadStatus.Cancelled;
            CleanupFailedDownload(item);
        }
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 执行流式下载
    /// </summary>
    private async Task ExecuteDownloadAsync(
        DownloadItem item, CancellationToken token, IProgress<double>? progress)
    {
        // 先发送 HEAD 请求获取文件大小（可选优化）
        try
        {
            var headRequest = new HttpRequestMessage(HttpMethod.Head, item.Url);
            var headResponse = await _httpClient.SendAsync(headRequest, token);
            if (headResponse.Content.Headers.ContentLength.HasValue)
            {
                item.TotalBytes = headResponse.Content.Headers.ContentLength.Value;
            }
        }
        catch
        {
            // HEAD 请求失败不影响下载，继续
        }

        // 确保保存目录存在
        string? directory = Path.GetDirectoryName(item.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // 发送 GET 请求并流式读取
        using var response = await _httpClient.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();

        // 如果 HEAD 请求未获取到大小，从 GET 响应中获取
        item.TotalBytes ??= response.Content.Headers.ContentLength;

        using var contentStream = await response.Content.ReadAsStreamAsync(token);
        using var fileStream = new FileStream(item.FilePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 8192, useAsync: true);

        var buffer = new byte[8192]; // 8KB 缓冲区
        int bytesRead;
        long totalDownloaded = 0;

        while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
            totalDownloaded += bytesRead;
            item.DownloadedBytes = totalDownloaded;

            // 计算并更新进度
            if (item.TotalBytes.HasValue && item.TotalBytes.Value > 0)
            {
                item.Progress = (double)totalDownloaded / item.TotalBytes.Value * 100;
            }

            // 报告进度
            progress?.Report(item.Progress);
        }

        // 下载成功
        item.Status = DownloadStatus.Completed;
        item.Progress = 100;
        _activeDownloads.TryRemove(item.Id, out _);
    }

    /// <summary>
    /// 获取下载保存目录
    /// 优先使用用户设置的自定义路径，否则使用默认路径
    /// </summary>
    private string GetDownloadDirectory()
    {
        string settingsPath = _settingsService.DownloadPath;
        string directory = FileHelper.GetDownloadPath(settingsPath);
        FileHelper.EnsureDirectoryExists(directory);
        return directory;
    }

    /// <summary>
    /// 清理下载失败的文件
    /// </summary>
    private static void CleanupFailedDownload(DownloadItem item)
    {
        if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
        {
            try
            {
                File.Delete(item.FilePath);
            }
            catch
            {
                // 清理失败不影响主流程
            }
        }
    }
}
