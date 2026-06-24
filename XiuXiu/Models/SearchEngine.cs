// 搜索引擎模型
// 表示一个可用的搜索引擎配置
namespace XiuXiu.Models;

/// <summary>
/// 搜索引擎配置
/// 包含搜索引擎名称、搜索 URL 模板和图标路径
/// </summary>
public class SearchEngine
{
    /// <summary>搜索引擎名称，如 "Baidu", "Google"</summary>
    public string Name { get; set; } = "";

    /// <summary>搜索基础 URL，使用 {0} 作为查询占位符
    /// 例如 "https://www.baidu.com/s?wd={0}"</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>搜索引擎图标路径</summary>
    public string IconPath { get; set; } = "";

    /// <summary>是否为内置搜索引擎（内置的不可删除）</summary>
    public bool IsBuiltIn { get; set; } = true;
}
