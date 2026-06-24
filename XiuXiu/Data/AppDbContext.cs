// 数据库上下文
// 管理 SQLite 数据库连接，提供 IDbConnection 工厂方法
using System.Data;
using Microsoft.Data.Sqlite;

namespace XiuXiu.Data;

/// <summary>
/// 应用数据库上下文
/// 集中管理 SQLite 数据库连接字符串和连接创建
/// 数据库文件存储在 %LocalAppData%\XiuXiu\data.db
/// </summary>
public class AppDbContext
{
    // 数据库连接字符串
    private readonly string _connectionString;

    /// <summary>
    /// 初始化数据库上下文
    /// 自动确定数据库文件路径（%LocalAppData%\XiuXiu\data.db）
    /// </summary>
    public AppDbContext()
    {
        // 数据库文件存储路径
        string dbDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XiuXiu");

        // 确保目录存在
        if (!Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        string dbPath = Path.Combine(dbDirectory, "data.db");
        _connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared";
    }

    /// <summary>
    /// 创建并返回一个新的数据库连接
    /// 调用方负责释放连接（using 语句）
    /// </summary>
    /// <returns>已打开的 SQLite 连接</returns>
    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 获取连接字符串（用于需要直接操作连接字符串的场景）
    /// </summary>
    public string ConnectionString => _connectionString;
}
