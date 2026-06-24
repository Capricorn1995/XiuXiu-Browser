// 嗅嗅浏览器 - 应用程序入口
// 使用 .NET 通用主机（Generic Host）配置依赖注入和日志
// 使用 CommunityToolkit.Mvvm 源代码生成器模式

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Windows;

using XiuXiu.Services;
using XiuXiu.ViewModels;
using XiuXiu.Views;

namespace XiuXiu;

/// <summary>
/// 应用程序入口类，负责配置 DI 容器、日志和启动主窗口
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    /// <summary>
    /// 获取 DI 容器中的服务
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// 从 DI 容器中获取服务
    /// </summary>
    public T GetService<T>() where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    public App()
    {
        // 全局异常捕获 - 防止应用静默崩溃
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            string crashLog = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XiuXiu", "logs", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
            File.WriteAllText(crashLog,
                $"[{DateTime.Now}] 未处理异常: {args.ExceptionObject}");
            MessageBox.Show($"应用发生错误，请查看日志:\n{crashLog}",
                "嗅嗅浏览器 - 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // 构建 .NET 通用主机，注册所有服务和 ViewModel
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // 加载 appsettings.json 配置文件
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseSerilog((context, config) =>
            {
                // 配置 Serilog 日志：输出到文件和控制台
                config
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "XiuXiu", "logs", "xiuxiu-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .Enrich.FromLogContext();
            })
            .ConfigureServices((context, services) =>
            {
                // ===== 注册数据库 =====
                services.AddSingleton<Data.AppDbContext>();

                // ===== 注册服务（接口 → 实现） =====
                // 所有核心服务使用单例模式，在应用生命周期内共享
                services.AddSingleton<IBrowserService, BrowserService>();
                services.AddSingleton<IMediaExtractionService, MediaExtractionService>();
                services.AddSingleton<IDownloadService, DownloadService>();
                services.AddSingleton<IBookmarkService, BookmarkService>();
                services.AddSingleton<IHistoryService, HistoryService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();

                // ===== 注册 ViewModel =====
                // MainViewModel 为单例，主窗口生命周期内唯一
                services.AddSingleton<MainViewModel>();
                // BrowserTabViewModel 为瞬时，每个标签页独立实例
                services.AddTransient<BrowserTabViewModel>();
                services.AddSingleton<MediaPanelViewModel>();
                services.AddTransient<GalleryViewModel>();
                services.AddSingleton<DownloadManagerViewModel>();
                services.AddSingleton<BookmarkViewModel>();
                services.AddSingleton<HistoryViewModel>();
                services.AddSingleton<SettingsViewModel>();

                // ===== 注册窗口 =====
                services.AddTransient<MainWindow>();
            })
            .Build();
    }

    /// <summary>
    /// 应用程序启动时调用，初始化设置并显示主窗口
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 启动托管服务
        await _host.StartAsync();

        // 初始化数据库
        var dbContext = _host.Services.GetRequiredService<Data.AppDbContext>();
        Data.DatabaseInitializer.Initialize(dbContext);

        // 加载用户设置并应用主题
        var settingsService = _host.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();

        var themeService = _host.Services.GetRequiredService<IThemeService>();
        themeService.ApplyTheme(settings.Theme);

        // 创建并显示主窗口，设置 DataContext 为 MainViewModel
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    /// <summary>
    /// 应用程序退出时调用，清理资源并保存设置
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        // 加载当前设置
        var settingsService = _host.Services.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();

        // 如果设置了退出时清除历史记录
        if (settings.ClearHistoryOnExit)
        {
            var historyService = _host.Services.GetRequiredService<IHistoryService>();
            await historyService.ClearAllAsync();
        }

        // 保存当前设置
        settingsService.SaveSettings(settings);

        // 停止托管服务
        await _host.StopAsync(TimeSpan.FromSeconds(5));
        _host.Dispose();

        base.OnExit(e);
    }
}
