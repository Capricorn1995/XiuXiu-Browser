// M3U8/HLS 视频下载服务
// 支持解析 m3u8 播放列表、多线程并发下载 TS 分片、合并为完整 MP4
// 支持 AES-128 加密的 m3u8 流

using System.Threading;

namespace XiuXiu.Services;

/// <summary>
/// M3U8 密钥信息
/// </summary>
public class M3u8KeyInfo
{
    public string Method { get; set; } = "";
    public string? Uri { get; set; }
    public string? Iv { get; set; }
}

/// <summary>
/// M3U8/HLS 视频下载服务
/// 支持：
/// - 解析 m3u8 播放列表
/// - 多线程并发下载 TS 分片
/// - 合并 TS 分片为完整 MP4（直接二进制合并）
/// - 支持加密的 m3u8（AES-128）
/// </summary>
public class M3u8DownloadService
{
    private readonly HttpClient _httpClient;

    // 默认并发下载数
    private const int DefaultConcurrency = 8;
    // 每个分片最大重试次数
    private const int MaxRetries = 3;

    public M3u8DownloadService()
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        });
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// 下载 m3u8 视频并合并为单个文件
    /// </summary>
    /// <param name="m3u8Url">m3u8 播放列表 URL</param>
    /// <param name="outputPath">输出文件路径（.ts 或 .mp4）</param>
    /// <param name="progress">进度报告器 (0-100)</param>
    /// <returns>合并后的文件路径</returns>
    public async Task<string> DownloadM3u8Async(string m3u8Url, string outputPath, IProgress<double>? progress = null)
    {
        // 1. 下载并解析 m3u8 内容
        progress?.Report(0);
        string m3u8Content = await _httpClient.GetStringAsync(m3u8Url);
        string baseUrl = GetBaseUrl(m3u8Url);

        // 2. 解析密钥信息
        var keyInfo = ParseKeyInfo(m3u8Content);

        // 3. 解析 TS 分片 URL 列表
        var segmentUrls = ParseM3u8Segments(m3u8Content, baseUrl);

        if (segmentUrls.Count == 0)
        {
            throw new InvalidOperationException("m3u8 播放列表中未找到任何 TS 分片");
        }

        // 4. 创建临时目录下载分片
        string tempDir = Path.Combine(Path.GetTempPath(), $"xiuxiu_m3u8_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 5. 下载密钥（如果需要）
            byte[]? key = null;
            if (keyInfo != null && !string.IsNullOrEmpty(keyInfo.Uri) &&
                keyInfo.Method.Equals("AES-128", StringComparison.OrdinalIgnoreCase))
            {
                string keyUrl = ResolveUrl(keyInfo.Uri, baseUrl);
                key = await _httpClient.GetByteArrayAsync(keyUrl);
            }

            // 6. 并发下载所有 TS 分片
            var segmentPaths = await DownloadSegmentsAsync(segmentUrls, tempDir, DefaultConcurrency, progress, key, keyInfo?.Iv);

            // 7. 合并所有 TS 分片
            progress?.Report(95);
            MergeSegments(segmentPaths, outputPath);
            progress?.Report(100);

            return outputPath;
        }
        finally
        {
            // 清理临时目录
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch
            {
                // 清理失败不影响主流程
            }
        }
    }

    /// <summary>
    /// 解析 m3u8 内容，提取 TS 分片 URL 列表
    /// 处理绝对 URL 和相对 URL（通过 baseUrl 解析）
    /// </summary>
    private List<string> ParseM3u8Segments(string m3u8Content, string baseUrl)
    {
        var segments = new List<string>();
        var lines = m3u8Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // 跳过注释行和标签行
            if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                continue;

            // 非注释行即为分片 URL
            string absoluteUrl = ResolveUrl(line, baseUrl);
            segments.Add(absoluteUrl);
        }

        return segments;
    }

    /// <summary>
    /// 解析 m3u8 中的 AES-128 密钥信息
    /// 格式：#EXT-X-KEY:METHOD=AES-128,URI="key_url",IV=0x...
    /// </summary>
    private static M3u8KeyInfo? ParseKeyInfo(string m3u8Content)
    {
        var lines = m3u8Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("#EXT-X-KEY:", StringComparison.OrdinalIgnoreCase))
                continue;

            var keyInfo = new M3u8KeyInfo();

            // 解析 METHOD
            var methodMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                @"METHOD=([^,]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (methodMatch.Success)
                keyInfo.Method = methodMatch.Groups[1].Value.Trim();

            // 解析 URI
            var uriMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                @"URI=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (uriMatch.Success)
                keyInfo.Uri = uriMatch.Groups[1].Value;

            // 解析 IV
            var ivMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                @"IV=0x([0-9a-fA-F]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (ivMatch.Success)
                keyInfo.Iv = ivMatch.Groups[1].Value;

            return keyInfo;
        }

        return null;
    }

    /// <summary>
    /// 并发下载 TS 分片到临时目录
    /// 使用 SemaphoreSlim 限制并发数，每个分片支持重试
    /// </summary>
    private async Task<List<string>> DownloadSegmentsAsync(
        List<string> segmentUrls, string tempDir, int concurrency,
        IProgress<double>? progress, byte[]? key, string? ivHex)
    {
        var segmentPaths = new List<string>();
        int totalSegments = segmentUrls.Count;
        int downloadedCount = 0;
        var lockObj = new object();

        using var semaphore = new SemaphoreSlim(concurrency);

        var tasks = new List<Task>();
        for (int i = 0; i < segmentUrls.Count; i++)
        {
            int index = i;
            string url = segmentUrls[i];
            string segmentPath = Path.Combine(tempDir, $"segment_{index:D6}.ts");

            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await DownloadSegmentWithRetryAsync(url, segmentPath, key, ivHex, index);
                    lock (lockObj)
                    {
                        segmentPaths.Add(segmentPath);
                        downloadedCount++;
                        double pct = (double)downloadedCount / totalSegments * 90; // 0-90% 用于下载阶段
                        progress?.Report(pct);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // 按索引排序，确保合并顺序正确
        segmentPaths.Sort();

        return segmentPaths;
    }

    /// <summary>
    /// 下载单个分片，支持重试
    /// </summary>
    private async Task DownloadSegmentWithRetryAsync(
        string url, string segmentPath, byte[]? key, string? ivHex, int index)
    {
        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                byte[] data = await _httpClient.GetByteArrayAsync(url);

                // 如果提供了密钥，进行 AES-128 解密
                if (key != null && key.Length > 0)
                {
                    data = DecryptAes128(data, key, ivHex, index);
                }

                await File.WriteAllBytesAsync(segmentPath, data);
                return;
            }
            catch (Exception)
            {
                if (retry == MaxRetries - 1)
                    throw;
                // 重试前短暂等待
                await Task.Delay(500 * (retry + 1));
            }
        }
    }

    /// <summary>
    /// AES-128-CBC 解密 TS 分片数据
    /// </summary>
    private static byte[] DecryptAes128(byte[] data, byte[] key, string? ivHex, int segmentIndex)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.Padding = System.Security.Cryptography.PaddingMode.None;

        // IV: 使用指定的 IV 或使用分片索引作为 IV（HLS 标准）
        byte[] iv;
        if (!string.IsNullOrEmpty(ivHex))
        {
            iv = new byte[16];
            for (int i = 0; i < 16 && i * 2 < ivHex.Length; i++)
            {
                iv[i] = Convert.ToByte(ivHex.Substring(i * 2, 2), 16);
            }
        }
        else
        {
            // 默认 IV：分片索引的大端序 16 字节表示
            iv = new byte[16];
            byte[] indexBytes = BitConverter.GetBytes(segmentIndex);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(indexBytes);
            Array.Copy(indexBytes, 0, iv, 12, 4);
        }

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var msInput = new MemoryStream(data);
        using var cs = new System.Security.Cryptography.CryptoStream(msInput, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var msOutput = new MemoryStream();

        cs.CopyTo(msOutput);
        return msOutput.ToArray();
    }

    /// <summary>
    /// 合并 TS 分片（简单二进制拼接）
    /// 大多数未加密的 m3u8 可以通过此方式直接合并
    /// </summary>
    private static void MergeSegments(List<string> segmentPaths, string outputPath)
    {
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        foreach (string segmentPath in segmentPaths)
        {
            byte[] segmentData = File.ReadAllBytes(segmentPath);
            outputStream.Write(segmentData, 0, segmentData.Length);
        }
    }

    /// <summary>
    /// 使用 ffmpeg 将 TS 文件转换为 MP4（可选，如果系统有 ffmpeg）
    /// </summary>
    public static async Task<bool> ConvertToMp4Async(string tsPath, string mp4Path)
    {
        if (!IsFfmpegAvailable())
            return false;

        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{tsPath}\" -c copy -bsf:a aac_adtstoasc \"{mp4Path}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查 ffmpeg 是否可用
    /// </summary>
    public static bool IsFfmpegAvailable()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测 URL 是否为 m3u8
    /// </summary>
    public static bool IsM3u8Url(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // 提取路径部分（去掉查询参数）
        string lower = url.ToLowerInvariant();
        int queryIndex = lower.IndexOf('?');
        string path = queryIndex >= 0 ? lower[..queryIndex] : lower;

        return path.EndsWith(".m3u8") || path.EndsWith(".m3u");
    }

    /// <summary>
    /// 从 m3u8 URL 获取基础 URL（用于解析相对路径）
    /// </summary>
    private static string GetBaseUrl(string m3u8Url)
    {
        try
        {
            var uri = new Uri(m3u8Url);
            int lastSlash = uri.AbsoluteUri.LastIndexOf('/');
            if (lastSlash > 8) // 保留 "https://" 之后的内容
            {
                return uri.AbsoluteUri[..(lastSlash + 1)];
            }
            return uri.AbsoluteUri;
        }
        catch
        {
            return m3u8Url;
        }
    }

    /// <summary>
    /// 解析相对 URL 为绝对 URL
    /// </summary>
    private static string ResolveUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // 已经是绝对 URL
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        // 协议相对 URL
        if (url.StartsWith("//"))
            return "https:" + url;

        // 相对路径
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri) &&
            Uri.TryCreate(baseUri, url, out Uri? resolved))
        {
            return resolved.AbsoluteUri;
        }

        // 简单拼接
        string baseWithoutFile = baseUrl;
        if (!baseWithoutFile.EndsWith("/"))
        {
            int lastSlash = baseWithoutFile.LastIndexOf('/');
            if (lastSlash > 8)
                baseWithoutFile = baseWithoutFile[..(lastSlash + 1)];
            else
                baseWithoutFile += "/";
        }

        return baseWithoutFile + url.TrimStart('/');
    }
}
