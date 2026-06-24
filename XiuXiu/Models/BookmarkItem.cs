// 书签模型
// 表示用户保存的单个书签条目
namespace XiuXiu.Models;

/// <summary>
/// 书签条目
/// 持久化到 SQLite 数据库中
/// </summary>
public class BookmarkItem
{
    /// <summary>自增主键</summary>
    public int Id { get; set; }

    /// <summary>书签标题</summary>
    public string Title { get; set; } = "";

    /// <summary>书签 URL</summary>
    public string Url { get; set; } = "";

    /// <summary>网站图标 URL</summary>
    public string FaviconUrl { get; set; } = "";

    /// <summary>所属文件夹名称（null 表示根目录）</summary>
    public string? Folder { get; set; }

    /// <summary>创建时间 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
