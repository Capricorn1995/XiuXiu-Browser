// 书签服务实现
// 使用 SQLite + Dapper 存储和管理书签数据
// 通过 AppDbContext 获取数据库连接，DatabaseInitializer 负责建表
using Dapper;
using Microsoft.Data.Sqlite;
using XiuXiu.Data;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 书签服务
/// 基于 SQLite 数据库，使用 Dapper 轻量级 ORM
/// 表结构由 DatabaseInitializer 统一管理
/// </summary>
public class BookmarkService : IBookmarkService
{
    private readonly AppDbContext _dbContext;

    public BookmarkService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取所有书签，按创建时间降序排列
    /// </summary>
    public async Task<List<BookmarkItem>> GetAllAsync()
    {
        using var connection = _dbContext.CreateConnection();
        var bookmarks = await connection.QueryAsync<BookmarkItem>(
            "SELECT * FROM Bookmarks ORDER BY CreatedAt DESC");
        return bookmarks.ToList();
    }

    /// <summary>
    /// 获取指定文件夹下的书签
    /// folder 为 null 时返回根目录书签
    /// </summary>
    public async Task<List<BookmarkItem>> GetByFolderAsync(string? folder)
    {
        using var connection = _dbContext.CreateConnection();

        string sql;
        object param;

        if (folder == null)
        {
            sql = "SELECT * FROM Bookmarks WHERE Folder IS NULL ORDER BY CreatedAt DESC";
            param = new { };
        }
        else
        {
            sql = "SELECT * FROM Bookmarks WHERE Folder = @Folder ORDER BY CreatedAt DESC";
            param = new { Folder = folder };
        }

        var bookmarks = await connection.QueryAsync<BookmarkItem>(sql, param);
        return bookmarks.ToList();
    }

    /// <summary>
    /// 根据 URL 查找书签（用于查重）
    /// </summary>
    public async Task<BookmarkItem?> GetByUrlAsync(string url)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BookmarkItem>(
            "SELECT * FROM Bookmarks WHERE Url = @Url", new { Url = url });
    }

    /// <summary>
    /// 添加书签
    /// 自动设置创建时间为当前 UTC 时间
    /// </summary>
    public async Task AddAsync(BookmarkItem bookmark)
    {
        using var connection = _dbContext.CreateConnection();

        // 如果未设置创建时间，使用当前时间
        if (bookmark.CreatedAt == default)
        {
            bookmark.CreatedAt = DateTime.UtcNow;
        }

        string sql = @"INSERT INTO Bookmarks (Title, Url, FaviconUrl, Folder, CreatedAt)
                       VALUES (@Title, @Url, @FaviconUrl, @Folder, @CreatedAt)";

        await connection.ExecuteAsync(sql, bookmark);
    }

    /// <summary>
    /// 更新书签信息（根据 Id 匹配）
    /// </summary>
    public async Task UpdateAsync(BookmarkItem bookmark)
    {
        using var connection = _dbContext.CreateConnection();

        string sql = @"UPDATE Bookmarks SET
                       Title = @Title,
                       Url = @Url,
                       FaviconUrl = @FaviconUrl,
                       Folder = @Folder
                       WHERE Id = @Id";

        await connection.ExecuteAsync(sql, bookmark);
    }

    /// <summary>
    /// 根据 ID 删除书签
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM Bookmarks WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 检查指定 URL 是否已被收藏
    /// </summary>
    public async Task<bool> IsBookmarkedAsync(string url)
    {
        using var connection = _dbContext.CreateConnection();
        int count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Bookmarks WHERE Url = @Url", new { Url = url });
        return count > 0;
    }

    /// <summary>
    /// 获取所有文件夹名称列表（去重，排除 NULL）
    /// </summary>
    public async Task<List<string>> GetFoldersAsync()
    {
        using var connection = _dbContext.CreateConnection();
        var folders = await connection.QueryAsync<string>(
            "SELECT DISTINCT Folder FROM Bookmarks WHERE Folder IS NOT NULL ORDER BY Folder");
        return folders.ToList();
    }
}
