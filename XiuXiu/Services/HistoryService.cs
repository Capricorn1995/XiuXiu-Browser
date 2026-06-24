// 历史记录服务实现
// 使用 SQLite + Dapper 存储和管理浏览历史
// 通过 AppDbContext 获取数据库连接，DatabaseInitializer 负责建表和索引
using Dapper;
using Microsoft.Data.Sqlite;
using XiuXiu.Data;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 历史记录服务
/// 基于 SQLite 数据库，使用 Dapper 轻量级 ORM
/// 支持模糊搜索、按时间范围清理和分页查询
/// 表结构由 DatabaseInitializer 统一管理
/// </summary>
public class HistoryService : IHistoryService
{
    private readonly AppDbContext _dbContext;

    public HistoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取最近的浏览历史
    /// 按访问时间降序排列，支持限制返回数量
    /// </summary>
    public async Task<List<HistoryItem>> GetRecentAsync(int limit = 100)
    {
        using var connection = _dbContext.CreateConnection();
        var items = await connection.QueryAsync<HistoryItem>(
            "SELECT * FROM History ORDER BY VisitedAt DESC LIMIT @Limit",
            new { Limit = limit });
        return items.ToList();
    }

    /// <summary>
    /// 搜索浏览历史
    /// 按标题或 URL 进行模糊匹配（LIKE）
    /// 结果按访问时间降序排列
    /// </summary>
    public async Task<List<HistoryItem>> SearchAsync(string query)
    {
        using var connection = _dbContext.CreateConnection();

        string sql = @"SELECT * FROM History
                       WHERE Title LIKE @Query OR Url LIKE @Query
                       ORDER BY VisitedAt DESC
                       LIMIT 50";

        var items = await connection.QueryAsync<HistoryItem>(sql,
            new { Query = $"%{query}%" });
        return items.ToList();
    }

    /// <summary>
    /// 添加或更新浏览历史记录
    /// 如果同一 URL 已存在，则更新访问时间和标题
    /// 使用 INSERT OR REPLACE 策略
    /// </summary>
    public async Task AddAsync(HistoryItem entry)
    {
        using var connection = _dbContext.CreateConnection();

        // 如果未设置访问时间，使用当前 UTC 时间
        if (entry.VisitedAt == default)
        {
            entry.VisitedAt = DateTime.UtcNow;
        }

        // 检查 URL 是否已存在
        var existing = await connection.QueryFirstOrDefaultAsync<HistoryItem>(
            "SELECT Id FROM History WHERE Url = @Url LIMIT 1", new { entry.Url });

        if (existing != null)
        {
            // 更新已有记录：更新标题、访问时间和图标 URL
            string updateSql = @"UPDATE History SET
                                 Title = @Title,
                                 VisitedAt = @VisitedAt,
                                 FaviconUrl = @FaviconUrl
                                 WHERE Url = @Url";
            await connection.ExecuteAsync(updateSql, entry);
        }
        else
        {
            // 插入新记录
            string insertSql = @"INSERT INTO History (Title, Url, FaviconUrl, VisitedAt)
                                 VALUES (@Title, @Url, @FaviconUrl, @VisitedAt)";
            await connection.ExecuteAsync(insertSql, entry);
        }
    }

    /// <summary>
    /// 根据 ID 删除单条历史记录
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM History WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 清除所有浏览历史
    /// </summary>
    public async Task ClearAllAsync()
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM History");
    }

    /// <summary>
    /// 清除指定日期之前的历史记录
    /// 例如清除 30 天前的记录
    /// </summary>
    public async Task ClearOlderThanAsync(DateTime date)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM History WHERE VisitedAt < @Date",
            new { Date = date.ToString("yyyy-MM-dd HH:mm:ss") });
    }
}
