// 设置服务接口
// 管理应用配置的读取、写入和持久化
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 设置服务接口
/// 提供强类型的配置读取/写入和搜索引擎管理
/// </summary>
public interface ISettingsService
{
    /// <summary>加载应用设置（从 JSON 文件反序列化）</summary>
    AppSettings LoadSettings();

    /// <summary>保存应用设置（序列化为 JSON 文件）</summary>
    /// <param name="settings">设置对象</param>
    void SaveSettings(AppSettings settings);

    /// <summary>获取所有已配置的搜索引擎列表</summary>
    List<SearchEngine> GetSearchEngines();

    /// <summary>获取下载文件保存路径</summary>
    /// <returns>下载目录完整路径</returns>
    string GetDownloadPath();

    /// <summary>获取或设置默认搜索引擎</summary>
    string DefaultSearchEngine { get; set; }

    /// <summary>获取或设置主页 URL</summary>
    string HomePage { get; set; }

    /// <summary>获取或设置下载路径</summary>
    string DownloadPath { get; set; }
}
