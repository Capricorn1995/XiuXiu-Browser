// 嗅嗅浏览器 - 主窗口
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using XiuXiu.ViewModels;

namespace XiuXiu.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as MainViewModel;
        if (_vm == null) return;

        // 初始化 WebView2
        var userDataFolder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XiuXiu", "WebView2Data");

        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await BrowserWebView.EnsureCoreWebView2Async(env);

        var core = BrowserWebView.CoreWebView2;
        core.Settings.IsScriptEnabled = true;
        core.Settings.IsWebMessageEnabled = true;
        core.Settings.AreDefaultScriptDialogsEnabled = true;
        core.Settings.IsStatusBarEnabled = false;

        // 把 WebView2 传给 ViewModel
        _vm.BrowserWebView = BrowserWebView;

        // 事件 → ViewModel
        BrowserWebView.NavigationStarting += (s, args) =>
        {
            _vm.AddressBarText = args.Uri;
            _vm.IsLoading = true;
            _vm.StatusText = "加载中...";
        };

        BrowserWebView.NavigationCompleted += (s, args) =>
        {
            _vm.IsLoading = false;
            _vm.LoadingProgress = 0;
            _vm.CanGoBack = BrowserWebView.CanGoBack;
            _vm.CanGoForward = BrowserWebView.CanGoForward;
            _vm.AddressBarText = BrowserWebView.Source?.ToString() ?? "";
            _vm.StatusText = args.IsSuccess ? "已完成" : $"加载失败: {args.WebErrorStatus}";
        };

        BrowserWebView.CoreWebView2.HistoryChanged += (s, args) =>
        {
            _vm.CanGoBack = BrowserWebView.CanGoBack;
            _vm.CanGoForward = BrowserWebView.CanGoForward;
        };

        // 加载首页
        core.Navigate("https://www.baidu.com");
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm != null)
        {
            string url = _vm.AddressBarText?.Trim() ?? "";
            if (string.IsNullOrEmpty(url)) return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                if (url.Contains('.') && !url.Contains(' '))
                    url = "https://" + url;
                else
                    url = "https://www.baidu.com/s?wd=" + Uri.EscapeDataString(url);
            }

            _vm.AddressBarText = url;
            BrowserWebView.CoreWebView2?.Navigate(url);
        }
    }
}
