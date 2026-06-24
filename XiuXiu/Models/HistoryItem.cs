// 历史记录模型
// 表示用户浏览历史中的一条访问记录
namespace XiuXiu.Models;

/// <summary>
/// 浏览历史条目
/// 持久化到 SQLite 数据库中
/// </summary>
public class HistoryItem
{
    /// <summary>自增主键</summary>
    public int Id { get; set; }

    /// <summary>页面标题</summary>
    public string Title { get; set; } = "";

    /// <summary>页面 URL</summary>
    public string Url { get; set; } = "";

    /// <summary>网站图标 URL</summary>
    public string FaviconUrl { get; set; } = "";

    /// <summary>访问时间 (UTC)</summary>
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}
