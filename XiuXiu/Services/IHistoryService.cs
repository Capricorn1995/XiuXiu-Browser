// 历史记录服务接口
// 管理浏览器历史记录的存储、搜索、清理和删除操作
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 历史记录服务接口
/// 提供浏览历史的完整生命周期管理
/// </summary>
public interface IHistoryService
{
    /// <summary>获取最近的浏览记录</summary>
    /// <param name="limit">返回记录数上限（默认 100）</param>
    Task<List<HistoryItem>> GetRecentAsync(int limit = 100);

    /// <summary>搜索浏览历史（按标题或 URL 模糊匹配）</summary>
    /// <param name="query">搜索关键词</param>
    Task<List<HistoryItem>> SearchAsync(string query);

    /// <summary>添加或更新浏览历史记录</summary>
    /// <param name="entry">历史记录条目</param>
    Task AddAsync(HistoryItem entry);

    /// <summary>根据 ID 删除单条记录</summary>
    /// <param name="id">历史记录 ID</param>
    Task DeleteAsync(int id);

    /// <summary>清除所有浏览历史</summary>
    Task ClearAllAsync();

    /// <summary>清除指定日期之前的历史记录</summary>
    /// <param name="date">截止日期（清除此日期之前的记录）</param>
    Task ClearOlderThanAsync(DateTime date);
}
