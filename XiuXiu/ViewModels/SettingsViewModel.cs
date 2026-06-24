// 设置 ViewModel
// 管理应用程序各项设置的用户界面交互
// 包括搜索引擎、主题、下载路径、主页和隐私设置
using System.Windows.Forms;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 设置面板的 ViewModel
/// 提供对浏览器各项配置的读写操作
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 当前应用设置对象
    /// </summary>
    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>
    /// 可用的搜索引擎列表
    /// </summary>
    [ObservableProperty]
    private List<SearchEngine> _searchEngines = new();

    /// <summary>
    /// 当前选中的搜索引擎
    /// </summary>
    [ObservableProperty]
    private SearchEngine? _selectedSearchEngine;

    /// <summary>
    /// 当前主题（Light/Dark/System）
    /// </summary>
    [ObservableProperty]
    private string _selectedTheme = "System";

    /// <summary>
    /// 下载路径
    /// </summary>
    [ObservableProperty]
    private string _downloadPath = string.Empty;

    /// <summary>
    /// 主页 URL
    /// </summary>
    [ObservableProperty]
    private string _homePage = "https://www.baidu.com";

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化设置 ViewModel
    /// </summary>
    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;

        // 加载当前设置
        LoadCurrentSettings();
    }

    // ===== 命令 =====

    /// <summary>
    /// 保存所有设置
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        // 同步到 Settings 对象
        Settings.DefaultSearchEngine = SelectedSearchEngine?.Name ?? "Baidu";
        Settings.DownloadPath = DownloadPath;
        Settings.Theme = SelectedTheme;
        Settings.HomePage = HomePage;

        // 保存到文件
        _settingsService.SaveSettings(Settings);

        // 应用主题变更
        _themeService.SetTheme(SelectedTheme);
    }

    /// <summary>
    /// 浏览下载路径
    /// </summary>
    [RelayCommand]
    private void BrowseDownloadPath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择下载保存目录",
            UseDescriptionForTitle = true,
            SelectedPath = string.IsNullOrWhiteSpace(DownloadPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : DownloadPath
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DownloadPath = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// 清除所有数据（历史记录和缓存）
    /// </summary>
    [RelayCommand]
    private void ClearAllData()
    {
        // 由 MainViewModel 协调清除操作
        // 这里通过设置来标记
    }

    /// <summary>
    /// 重置所有设置为默认值
    /// </summary>
    [RelayCommand]
    private void ResetDefaults()
    {
        SelectedSearchEngine = SearchEngines.FirstOrDefault(s => s.Name == "Baidu");
        DownloadPath = string.Empty;
        SelectedTheme = "System";
        HomePage = "https://www.baidu.com";

        Settings = new AppSettings();
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 加载当前设置到 ViewModel
    /// </summary>
    private void LoadCurrentSettings()
    {
        Settings = _settingsService.LoadSettings();
        SearchEngines = _settingsService.GetSearchEngines();

        // 同步到 UI 属性
        SelectedSearchEngine = SearchEngines.FirstOrDefault(
            s => s.Name == Settings.DefaultSearchEngine) ?? SearchEngines.FirstOrDefault();
        DownloadPath = Settings.DownloadPath;
        SelectedTheme = Settings.Theme;
        HomePage = Settings.HomePage;
    }
}
