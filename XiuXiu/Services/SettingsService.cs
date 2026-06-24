// 设置服务实现
// 使用 JSON 文件持久化存储应用设置
// 默认下载路径：%USERPROFILE%\Downloads\XiuXiu
// 设置文件路径：%LocalAppData%\XiuXiu\settings.json
using System.Text.Json;
using XiuXiu.Models;

namespace XiuXiu.Services;

/// <summary>
/// 设置服务
/// 核心功能：
/// - JSON 文件持久化（%LocalAppData%\XiuXiu\settings.json）
/// - 应用启动时从 appsettings.json 读取默认值
/// - 提供内置搜索引擎列表
/// - 默认下载路径管理
/// </summary>
public class SettingsService : ISettingsService
{
    // 当前加载的设置（内存缓存）
    private AppSettings? _currentSettings;

    // 设置文件路径
    private readonly string _settingsFilePath;

    // 应用配置文件路径（appsettings.json）
    private readonly string _appSettingsPath;

    public SettingsService()
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XiuXiu");

        _settingsFilePath = Path.Combine(appDataDir, "settings.json");

        // appsettings.json 位于应用程序目录
        _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// 加载应用设置
    /// 优先级：用户设置文件 > appsettings.json > 默认值
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            // 优先加载用户设置文件
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _currentSettings = settings;
                    return settings;
                }
            }

            // 回退到 appsettings.json
            if (File.Exists(_appSettingsPath))
            {
                string json = File.ReadAllText(_appSettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _currentSettings = settings;
                    return settings;
                }
            }
        }
        catch (Exception)
        {
            // 加载失败时使用默认设置
        }

        // 使用硬编码默认值
        _currentSettings = CreateDefaultSettings();
        return _currentSettings;
    }

    /// <summary>
    /// 保存应用设置为 JSON 文件
    /// 自动创建目录结构
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            File.WriteAllText(_settingsFilePath, json);
            _currentSettings = settings;
        }
        catch (Exception)
        {
            // 保存失败时静默处理
        }
    }

    /// <summary>
    /// 获取所有可用的搜索引擎列表
    /// 包含内置搜索引擎（Baidu, Google, Bing, Sogou）
    /// 可扩展为从配置文件加载自定义搜索引擎
    /// </summary>
    public List<SearchEngine> GetSearchEngines()
    {
        // 内置搜索引擎列表
        return new List<SearchEngine>
        {
            new()
            {
                Name = "Baidu",
                BaseUrl = "https://www.baidu.com/s?wd={0}",
                IconPath = "Resources/Icons/baidu.png",
                IsBuiltIn = true
            },
            new()
            {
                Name = "Google",
                BaseUrl = "https://www.google.com/search?q={0}",
                IconPath = "Resources/Icons/google.png",
                IsBuiltIn = true
            },
            new()
            {
                Name = "Bing",
                BaseUrl = "https://www.bing.com/search?q={0}",
                IconPath = "Resources/Icons/bing.png",
                IsBuiltIn = true
            },
            new()
            {
                Name = "Sogou",
                BaseUrl = "https://www.sogou.com/web?query={0}",
                IconPath = "Resources/Icons/sogou.png",
                IsBuiltIn = true
            }
        };
    }

    /// <summary>
    /// 获取下载文件保存路径
    /// 优先使用用户设置的自定义路径，否则使用默认路径
    /// </summary>
    public string GetDownloadPath()
    {
        var settings = _currentSettings ?? LoadSettings();

        if (!string.IsNullOrWhiteSpace(settings.DownloadPath) &&
            Directory.Exists(settings.DownloadPath))
        {
            return settings.DownloadPath;
        }

        // 默认下载路径：%USERPROFILE%\Downloads\XiuXiu
        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "XiuXiu");

        // 确保目录存在
        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }

        return defaultPath;
    }

    // ===== 便捷属性 =====
    // 这些属性用于与 ViewModel 兼容，提供便捷的读写访问

    /// <summary>获取或设置默认搜索引擎</summary>
    public string DefaultSearchEngine
    {
        get => (_currentSettings ?? LoadSettings()).DefaultSearchEngine;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.DefaultSearchEngine = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置下载路径</summary>
    public string DownloadPath
    {
        get => (_currentSettings ?? LoadSettings()).DownloadPath;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.DownloadPath = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置主题</summary>
    public string Theme
    {
        get => (_currentSettings ?? LoadSettings()).Theme;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.Theme = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置强调色</summary>
    public string AccentColor
    {
        get => (_currentSettings ?? LoadSettings()).AccentColor;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.AccentColor = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置是否在退出时清除历史记录</summary>
    public bool ClearHistoryOnExit
    {
        get => (_currentSettings ?? LoadSettings()).ClearHistoryOnExit;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.ClearHistoryOnExit = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置是否自动提取媒体</summary>
    public bool AutoExtractOnPageLoad
    {
        get => (_currentSettings ?? LoadSettings()).AutoExtractOnPageLoad;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.AutoExtractOnPageLoad = value;
            SaveSettings(settings);
        }
    }

    /// <summary>获取或设置主页 URL</summary>
    public string HomePage
    {
        get => (_currentSettings ?? LoadSettings()).HomePage;
        set
        {
            var settings = _currentSettings ?? LoadSettings();
            settings.HomePage = value;
            SaveSettings(settings);
        }
    }

    /// <summary>
    /// 异步保存设置（兼容异步调用）
    /// </summary>
    public async Task SaveAsync()
    {
        if (_currentSettings != null)
        {
            SaveSettings(_currentSettings);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 异步加载设置（兼容异步调用）
    /// </summary>
    public async Task LoadAsync()
    {
        LoadSettings();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 创建默认应用设置
    /// </summary>
    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            DefaultSearchEngine = "Baidu",
            DownloadPath = "",
            Theme = "System",
            AccentColor = "#319FDE",
            ClearHistoryOnExit = false,
            AutoExtractOnPageLoad = true,
            HomePage = "https://www.baidu.com"
        };
    }
}
