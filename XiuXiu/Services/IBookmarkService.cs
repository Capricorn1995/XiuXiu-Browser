// 书签服务接口
// 管理用户书签的增删改查及文件夹组织
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 书签服务接口
/// 提供书签的完整 CRUD 操作，支持按文件夹分组和 URL 查重
/// </summary>
public interface IBookmarkService
{
    /// <summary>获取所有书签</summary>
    Task<List<BookmarkItem>> GetAllAsync();

    /// <summary>获取指定文件夹下的书签（null 表示根目录）</summary>
    Task<List<BookmarkItem>> GetByFolderAsync(string? folder);

    /// <summary>根据 URL 查找书签</summary>
    Task<BookmarkItem?> GetByUrlAsync(string url);

    /// <summary>添加书签</summary>
    Task AddAsync(BookmarkItem bookmark);

    /// <summary>更新书签信息</summary>
    Task UpdateAsync(BookmarkItem bookmark);

    /// <summary>根据 ID 删除书签</summary>
    Task DeleteAsync(int id);

    /// <summary>检查指定 URL 是否已被收藏</summary>
    Task<bool> IsBookmarkedAsync(string url);

    /// <summary>获取所有文件夹名称列表</summary>
    Task<List<string>> GetFoldersAsync();
}
