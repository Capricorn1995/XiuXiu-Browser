// 文件操作辅助工具类
// 提供下载路径管理、目录创建、安全文件名生成和文件大小格式化功能
namespace XiuXiu.Helpers;

/// <summary>
/// 文件操作静态辅助类
/// 负责文件路径管理、安全文件名生成和目录操作
/// </summary>
public static class FileHelper
{
    // 默认下载目录名称
    private const string DefaultDownloadFolderName = "XiuXiu";

    // 文件名中不允许的字符
    // Windows 文件系统禁止在文件名中使用这些字符
    private static readonly HashSet<char> InvalidFileNameChars = new(
        Path.GetInvalidFileNameChars());

    /// <summary>
    /// 获取下载文件保存路径
    /// 优先使用用户设置的下载目录，未设置时使用系统默认下载目录下的子文件夹
    /// </summary>
    /// <param name="customPath">自定义下载路径（可选）</param>
    /// <returns>下载目录的完整路径</returns>
    public static string GetDownloadPath(string? customPath = null)
    {
        // 如果提供了自定义路径且有效，直接使用
        if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
            return customPath;

        // 使用系统默认下载目录
        string defaultDownloadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            DefaultDownloadFolderName);

        return defaultDownloadPath;
    }

    /// <summary>
    /// 确保指定目录存在，如果不存在则创建
    /// </summary>
    /// <param name="path">目录路径</param>
    public static void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// 从 URL 生成安全的文件名
    /// 提取 URL 中最后的文件名部分，移除非法字符
    /// </summary>
    /// <param name="url">下载 URL</param>
    /// <returns>安全的文件名</returns>
    public static string GetSafeFileName(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "download";

        try
        {
            // 尝试从 URL 中提取文件名
            string fileName = string.Empty;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                // 从路径的最后一段获取文件名
                fileName = Path.GetFileName(uri.AbsolutePath);
            }

            // 如果 URL 解析失败或文件名为空，使用默认名称
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "download";
            }

            // URL 解码文件名（处理 %20 等编码字符）
            fileName = Uri.UnescapeDataString(fileName);

            // 移除文件名中的非法字符
            fileName = RemoveInvalidFileNameChars(fileName);

            // 确保文件名不为空
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "download";
            }

            // 如果文件名过长，截断并保留扩展名
            if (fileName.Length > 200)
            {
                string extension = Path.GetExtension(fileName);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                int maxNameLength = 200 - extension.Length;
                if (maxNameLength > 0)
                {
                    fileName = nameWithoutExtension[..maxNameLength] + extension;
                }
                else
                {
                    fileName = fileName[..200];
                }
            }

            return fileName;
        }
        catch
        {
            return "download";
        }
    }

    /// <summary>
    /// 获取唯一文件路径
    /// 如果目标路径已存在同名文件，自动添加序号后缀
    /// 例如：file.txt → file (1).txt → file (2).txt
    /// </summary>
    /// <param name="directory">目标目录</param>
    /// <param name="fileName">期望的文件名</param>
    /// <returns>唯一的完整文件路径</returns>
    public static string GetUniqueFilePath(string directory, string fileName)
    {
        EnsureDirectoryExists(directory);

        string basePath = Path.Combine(directory, fileName);

        // 如果文件不存在，直接返回
        if (!File.Exists(basePath))
            return basePath;

        // 文件存在，添加序号后缀
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        int counter = 1;
        string newPath;

        do
        {
            string newFileName = $"{nameWithoutExtension} ({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        }
        while (File.Exists(newPath) && counter < 1000); // 防止无限循环

        return newPath;
    }

    /// <summary>
    /// 格式化文件大小为人类可读的字符串
    /// 例如：1024 → "1.00 KB"，1048576 → "1.00 MB"
    /// </summary>
    /// <param name="bytes">文件字节数</param>
    /// <returns>格式化后的文件大小字符串</returns>
    public static string FormatFileSize(long bytes)
    {
        // 文件大小单位数组
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };

        if (bytes == 0)
            return "0 B";

        // 计算单位级别（1024 进制）
        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        // 根据数值大小决定小数位数
        return $"{size:F2} {units[unitIndex]}";
    }

    /// <summary>
    /// 移除文件名中的非法字符
    /// Windows 文件系统不允许在文件名中使用某些特殊字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>移除非法字符后的文件名</returns>
    private static string RemoveInvalidFileNameChars(string fileName)
    {
        var validChars = fileName.Where(c => !InvalidFileNameChars.Contains(c)).ToArray();
        return new string(validChars);
    }
}
