// 数据库初始化器
// 在应用首次启动时创建 SQLite 数据库表结构和索引
// 使用 IF NOT EXISTS 确保幂等性（多次执行安全）
using Dapper;
using Microsoft.Data.Sqlite;

namespace XiuXiu.Data;

/// <summary>
/// 数据库初始化器
/// 负责创建 Bookmarks 和 History 表，以及相关索引
/// 使用 IF NOT EXISTS 确保重复执行安全
/// </summary>
public static class DatabaseInitializer
{
    // 数据库初始化锁（防止并发初始化）
    private static readonly object _initLock = new();
    private static bool _initialized;

    /// <summary>
    /// 初始化数据库
    /// 创建所有必需的表和索引（使用 IF NOT EXISTS 确保幂等性）
    /// 线程安全，多次调用只执行一次
    /// </summary>
    /// <param name="connectionString">SQLite 连接字符串</param>
    public static void Initialize(string connectionString)
    {
        if (_initialized)
            return;

        lock (_initLock)
        {
            if (_initialized)
                return;

            // 确保数据库目录存在
            string? dataSource = ExtractDataSource(connectionString);
            if (!string.IsNullOrEmpty(dataSource))
            {
                string? directory = Path.GetDirectoryName(dataSource);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // 创建书签表
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Bookmarks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    FaviconUrl TEXT,
                    Folder TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                )");

            // 创建历史记录表
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS History (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    FaviconUrl TEXT,
                    VisitedAt TEXT NOT NULL DEFAULT (datetime('now'))
                )");

            // 创建索引 - 提升查询性能
            connection.Execute(@"
                CREATE INDEX IF NOT EXISTS idx_bookmarks_url ON Bookmarks(Url)");

            connection.Execute(@"
                CREATE INDEX IF NOT EXISTS idx_bookmarks_folder ON Bookmarks(Folder)");

            connection.Execute(@"
                CREATE INDEX IF NOT EXISTS idx_history_url ON History(Url)");

            connection.Execute(@"
                CREATE INDEX IF NOT EXISTS idx_history_visitedat ON History(VisitedAt DESC)");

            _initialized = true;
        }
    }

    /// <summary>
    /// 使用 AppDbContext 初始化数据库
    /// 便捷方法，从 AppDbContext 获取连接字符串
    /// </summary>
    /// <param name="dbContext">数据库上下文实例</param>
    public static void Initialize(AppDbContext dbContext)
    {
        Initialize(dbContext.ConnectionString);
    }

    /// <summary>
    /// 从连接字符串中提取 Data Source 路径
    /// </summary>
    private static string? ExtractDataSource(string connectionString)
    {
        const string prefix = "Data Source=";
        int index = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        int start = index + prefix.Length;
        string remaining = connectionString[start..];

        // 取到分号或结尾
        int end = remaining.IndexOf(';');
        return end < 0 ? remaining.Trim() : remaining[..end].Trim();
    }
}
