// SettingsViewModel 单元测试
// 测试设置保存、重置默认值和主题变更功能
using FluentAssertions;
using Moq;
using Xunit;
using XiuXiu.Models;
using XiuXiu.Services;
using XiuXiu.ViewModels;

namespace XiuXiu.Tests.ViewModels;

/// <summary>
/// SettingsViewModel 单元测试
/// 覆盖 SaveSettings、ResetDefaults 和主题变更传播
/// </summary>
public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IThemeService> _themeServiceMock;

    public SettingsViewModelTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _themeServiceMock = new Mock<IThemeService>();
    }

    /// <summary>
    /// 创建 SettingsViewModel 实例并配置默认模拟行为
    /// </summary>
    private SettingsViewModel CreateViewModel()
    {
        var defaultSettings = new AppSettings
        {
            DefaultSearchEngine = "Baidu",
            DownloadPath = "",
            Theme = "System",
            HomePage = "https://www.baidu.com"
        };

        var searchEngines = new List<SearchEngine>
        {
            new() { Name = "Baidu", BaseUrl = "https://www.baidu.com/s?wd={0}", IsBuiltIn = true },
            new() { Name = "Google", BaseUrl = "https://www.google.com/search?q={0}", IsBuiltIn = true },
            new() { Name = "Bing", BaseUrl = "https://www.bing.com/search?q={0}", IsBuiltIn = true }
        };

        _settingsServiceMock.Setup(s => s.LoadSettings()).Returns(defaultSettings);
        _settingsServiceMock.Setup(s => s.GetSearchEngines()).Returns(searchEngines);

        return new SettingsViewModel(_settingsServiceMock.Object, _themeServiceMock.Object);
    }

    // ===== SaveSettings 测试 =====

    [Fact(DisplayName = "SaveSettings - 保存设置到服务")]
    public void SaveSettings_PersistsSettings()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 修改设置
        viewModel.DownloadPath = @"C:\Downloads";
        viewModel.SelectedTheme = "Dark";
        viewModel.HomePage = "https://www.google.com";

        // 执行
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证：SaveSettings 被调用，且设置已同步
        _settingsServiceMock.Verify(
            s => s.SaveSettings(It.Is<AppSettings>(settings =>
                settings.DownloadPath == @"C:\Downloads" &&
                settings.Theme == "Dark" &&
                settings.HomePage == "https://www.google.com")),
            Times.Once);

        // 验证：主题服务收到主题变更通知
        _themeServiceMock.Verify(t => t.SetTheme("Dark"), Times.Once);
    }

    [Fact(DisplayName = "SaveSettings - 保存默认设置值")]
    public void SaveSettings_WithDefaultValues_PersistsDefaults()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 执行：直接保存（不修改任何值）
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证：SaveSettings 被调用，包含默认值
        _settingsServiceMock.Verify(
            s => s.SaveSettings(It.IsAny<AppSettings>()),
            Times.Once);
    }

    [Fact(DisplayName = "SaveSettings - 同步搜索引擎到设置对象")]
    public void SaveSettings_SyncsSearchEngineToSettings()
    {
        // 准备
        var viewModel = CreateViewModel();
        viewModel.SelectedSearchEngine = new SearchEngine
        {
            Name = "Google",
            BaseUrl = "https://www.google.com/search?q={0}"
        };

        // 执行
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证：搜索引擎名称被正确同步
        _settingsServiceMock.Verify(
            s => s.SaveSettings(It.Is<AppSettings>(settings =>
                settings.DefaultSearchEngine == "Google")),
            Times.Once);
    }

    // ===== ResetDefaults 测试 =====

    [Fact(DisplayName = "ResetDefaults - 恢复默认设置值")]
    public void ResetDefaults_RestoresDefaultValues()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 先修改设置
        viewModel.DownloadPath = @"C:\Custom\Path";
        viewModel.SelectedTheme = "Dark";
        viewModel.HomePage = "https://www.google.com";

        // 执行：重置为默认值
        viewModel.ResetDefaultsCommand.Execute(null);

        // 验证：下载路径恢复为空
        viewModel.DownloadPath.Should().Be(string.Empty);
        // 验证：主题恢复为 System
        viewModel.SelectedTheme.Should().Be("System");
        // 验证：主页恢复为百度
        viewModel.HomePage.Should().Be("https://www.baidu.com");
        // 验证：搜索引擎恢复为百度
        viewModel.SelectedSearchEngine.Should().NotBeNull();
        viewModel.SelectedSearchEngine!.Name.Should().Be("Baidu");
    }

    [Fact(DisplayName = "ResetDefaults - 重置后设置对象为全新实例")]
    public void ResetDefaults_CreatesNewSettingsObject()
    {
        // 准备
        var viewModel = CreateViewModel();
        var originalSettings = viewModel.Settings;

        // 执行
        viewModel.ResetDefaultsCommand.Execute(null);

        // 验证：Settings 被替换为新对象
        viewModel.Settings.Should().NotBeNull();
        viewModel.Settings.DefaultSearchEngine.Should().Be("Baidu");
    }

    // ===== 主题变更测试 =====

    [Fact(DisplayName = "主题变更 - 保存设置时主题变更被传播到 ThemeService")]
    public void ThemeChange_OnSave_IsPropagatedToThemeService()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 修改主题为 Dark
        viewModel.SelectedTheme = "Dark";

        // 执行
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证：ThemeService.SetTheme 被调用，传入 Dark
        _themeServiceMock.Verify(t => t.SetTheme("Dark"), Times.Once);
    }

    [Fact(DisplayName = "主题变更 - Light 主题被正确传播")]
    public void ThemeChange_LightTheme_IsPropagated()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 修改主题为 Light
        viewModel.SelectedTheme = "Light";

        // 执行
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证
        _themeServiceMock.Verify(t => t.SetTheme("Light"), Times.Once);
    }

    [Fact(DisplayName = "主题变更 - System 主题被正确传播")]
    public void ThemeChange_SystemTheme_IsPropagated()
    {
        // 准备
        var viewModel = CreateViewModel();

        // 默认已是 System，先切换到 Dark 再切回 System
        viewModel.SelectedTheme = "Dark";
        viewModel.SelectedTheme = "System";

        // 执行
        viewModel.SaveSettingsCommand.Execute(null);

        // 验证
        _themeServiceMock.Verify(t => t.SetTheme("System"), Times.Once);
    }

    [Fact(DisplayName = "SettingsViewModel - 初始化时加载当前设置")]
    public void SettingsViewModel_OnConstruction_LoadsCurrentSettings()
    {
        // 准备：配置模拟服务返回特定值
        var customSettings = new AppSettings
        {
            DefaultSearchEngine = "Google",
            DownloadPath = @"D:\Downloads",
            Theme = "Light",
            HomePage = "https://www.bing.com"
        };

        var searchEngines = new List<SearchEngine>
        {
            new() { Name = "Baidu", BaseUrl = "https://www.baidu.com/s?wd={0}" },
            new() { Name = "Google", BaseUrl = "https://www.google.com/search?q={0}" },
            new() { Name = "Bing", BaseUrl = "https://www.bing.com/search?q={0}" }
        };

        _settingsServiceMock.Setup(s => s.LoadSettings()).Returns(customSettings);
        _settingsServiceMock.Setup(s => s.GetSearchEngines()).Returns(searchEngines);

        // 执行：创建 ViewModel（构造函数中自动加载）
        var viewModel = new SettingsViewModel(_settingsServiceMock.Object, _themeServiceMock.Object);

        // 验证：属性值已从服务加载
        viewModel.SelectedSearchEngine!.Name.Should().Be("Google");
        viewModel.DownloadPath.Should().Be(@"D:\Downloads");
        viewModel.SelectedTheme.Should().Be("Light");
        viewModel.HomePage.Should().Be("https://www.bing.com");
        viewModel.SearchEngines.Should().HaveCount(3);
    }
}
